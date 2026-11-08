using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MySqlConnector;
using MiBandDesktop.Core.Configuration;

namespace MiBandDesktop.Infrastructure.Migrations;

public class FlywayMigrationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> AppliedScripts { get; set; } = [];
}

public class FlywayMigrationRunner
{
    private static readonly Regex ScriptPattern = new(@"^V(?<version>\d+)__(?<desc>.+)\.sql$", RegexOptions.IgnoreCase);

    public static async Task<FlywayMigrationResult> MigrateAsync(DatabaseConfig config, Action<string>? logCallback = null)
    {
        var result = new FlywayMigrationResult();

        try
        {
            await using var conn = new MySqlConnection(config.GetDatabaseConnectionString());
            await conn.OpenAsync();

            logCallback?.Invoke("Conectado ao banco de dados. Verificando tabela flyway_schema_history...");
            await EnsureHistoryTableExistsAsync(conn);

            var appliedVersions = await GetAppliedVersionsAsync(conn);
            var pendingScripts = GetAvailableScripts()
                .Where(s => !appliedVersions.ContainsKey(s.Version))
                .OrderBy(s => s.Version)
                .ToList();

            if (pendingScripts.Count == 0)
            {
                result.Success = true;
                result.Message = "O banco de dados já está na versão mais recente (nenhuma migração pendente).";
                logCallback?.Invoke(result.Message);
                return result;
            }

            int nextRank = appliedVersions.Count > 0 ? appliedVersions.Values.Max(v => v.Rank) + 1 : 1;

            foreach (var script in pendingScripts)
            {
                logCallback?.Invoke($"Executando migração V{script.Version}: {script.Description}...");
                var sw = Stopwatch.StartNew();

                await using var trans = await conn.BeginTransactionAsync();
                try
                {
                    // Executa cada instrução do script SQL
                    var statements = SplitSqlStatements(script.SqlContent);
                    foreach (var stmt in statements)
                    {
                        if (string.IsNullOrWhiteSpace(stmt)) continue;
                        await using var cmd = new MySqlCommand(stmt, conn, trans);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    sw.Stop();
                    int executionTime = (int)sw.ElapsedMilliseconds;

                    // Registra no flyway_schema_history
                    var insertHistorySql = @"
                        INSERT INTO `flyway_schema_history`
                        (`installed_rank`, `version`, `description`, `type`, `script`, `checksum`, `installed_by`, `installed_on`, `execution_time`, `success`)
                        VALUES
                        (@rank, @version, @description, 'SQL', @script, @checksum, @installed_by, NOW(), @execution_time, 1);
                    ";

                    await using (var cmdHistory = new MySqlCommand(insertHistorySql, conn, trans))
                    {
                        cmdHistory.Parameters.AddWithValue("@rank", nextRank++);
                        cmdHistory.Parameters.AddWithValue("@version", script.Version.ToString());
                        cmdHistory.Parameters.AddWithValue("@description", script.Description);
                        cmdHistory.Parameters.AddWithValue("@script", script.FileName);
                        cmdHistory.Parameters.AddWithValue("@checksum", script.Checksum);
                        cmdHistory.Parameters.AddWithValue("@installed_by", config.Username);
                        cmdHistory.Parameters.AddWithValue("@execution_time", executionTime);
                        await cmdHistory.ExecuteNonQueryAsync();
                    }

                    await trans.CommitAsync();
                    result.AppliedScripts.Add(script.FileName);
                    logCallback?.Invoke($"Migração {script.FileName} aplicada com sucesso em {executionTime}ms.");
                }
                catch (Exception ex)
                {
                    await trans.RollbackAsync();
                    throw new InvalidOperationException($"Falha ao executar script {script.FileName}: {ex.Message}", ex);
                }
            }

            result.Success = true;
            result.Message = $"{result.AppliedScripts.Count} migrações executadas com sucesso.";
            logCallback?.Invoke(result.Message);
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Erro nas migrações Flyway: {ex.Message}";
            logCallback?.Invoke(result.Message);
            return result;
        }
    }

    private static async Task EnsureHistoryTableExistsAsync(MySqlConnection conn)
    {
        var sql = @"
            CREATE TABLE IF NOT EXISTS `flyway_schema_history` (
                `installed_rank` INT NOT NULL,
                `version` VARCHAR(50) DEFAULT NULL,
                `description` VARCHAR(200) NOT NULL,
                `type` VARCHAR(20) NOT NULL,
                `script` VARCHAR(1000) NOT NULL,
                `checksum` INT DEFAULT NULL,
                `installed_by` VARCHAR(100) NOT NULL,
                `installed_on` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                `execution_time` INT NOT NULL,
                `success` TINYINT(1) NOT NULL,
                PRIMARY KEY (`installed_rank`),
                INDEX `flyway_schema_history_s_idx` (`success`)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
        ";
        await using var cmd = new MySqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<Dictionary<int, (int Rank, int Checksum)>> GetAppliedVersionsAsync(MySqlConnection conn)
    {
        var dict = new Dictionary<int, (int Rank, int Checksum)>();
        var sql = "SELECT `installed_rank`, `version`, `checksum` FROM `flyway_schema_history` WHERE `success` = 1 AND `version` IS NOT NULL;";
        await using var cmd = new MySqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var rank = reader.GetInt32(0);
            var versionStr = reader.GetString(1);
            var checksum = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);

            if (int.TryParse(versionStr, out var v))
            {
                dict[v] = (rank, checksum);
            }
        }
        return dict;
    }

    public static List<(int Version, string Description, string FileName, string SqlContent, int Checksum)> GetAvailableScripts()
    {
        var list = new List<(int, string, string, string, int)>();

        // 1. Procura na pasta de compilação ou relativa
        var baseDir = AppContext.BaseDirectory;
        var possibleDirs = new[]
        {
            Path.Combine(baseDir, "Migrations", "Sql"),
            Path.Combine(baseDir, "..", "..", "..", "..", "MiBandDesktop.Infrastructure", "Migrations", "Sql"),
            Path.Combine(Directory.GetCurrentDirectory(), "Migrations", "Sql"),
            Path.Combine(Directory.GetCurrentDirectory(), "MiBandDesktop.Infrastructure", "Migrations", "Sql")
        };

        string? foundDir = possibleDirs.FirstOrDefault(Directory.Exists);
        if (foundDir != null)
        {
            var files = Directory.GetFiles(foundDir, "V*__*.sql");
            foreach (var f in files)
            {
                var fileName = Path.GetFileName(f);
                var match = ScriptPattern.Match(fileName);
                if (match.Success && int.TryParse(match.Groups["version"].Value, out var v))
                {
                    var desc = match.Groups["desc"].Value.Replace('_', ' ');
                    var content = File.ReadAllText(f);
                    int crc = unchecked((int)Crc32.HashToUInt32(System.Text.Encoding.UTF8.GetBytes(content)));
                    list.Add((v, desc, fileName, content, crc));
                }
            }
        }

        return list.OrderBy(s => s.Item1).ToList();
    }

    private static IEnumerable<string> SplitSqlStatements(string sql)
    {
        // Divide instruções separadas por ponto e vírgula ignorando linhas em branco
        return sql.Split(';')
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s));
    }
}

