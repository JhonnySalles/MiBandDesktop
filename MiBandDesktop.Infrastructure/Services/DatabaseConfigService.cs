using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using MySqlConnector;
using MiBandDesktop.Core.Configuration;

namespace MiBandDesktop.Infrastructure.Services;

public class DatabaseConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string GetConfigFilePath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(localAppData, "MiBandDesktop");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "database.config.json");
    }

    public static DatabaseConfig? Load()
    {
        try
        {
            var path = GetConfigFilePath();
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var config = JsonSerializer.Deserialize<DatabaseConfig>(json);
                if (config != null && config.IsValid())
                {
                    return config;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DatabaseConfigService.Load] Erro: {ex.Message}");
        }
        return null;
    }

    public static void Save(DatabaseConfig config)
    {
        var path = GetConfigFilePath();
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(path, json);
    }

    public static async Task<(bool Success, string Message, string? ServerVersion)> TestConnectionAsync(DatabaseConfig config)
    {
        if (!config.IsValid())
        {
            return (false, "Preencha todos os campos obrigatórios (Host, Porta, Banco e Usuário).", null);
        }

        try
        {
            // 1. Testa conectividade no servidor MySQL
            await using var serverConn = new MySqlConnection(config.GetServerConnectionString());
            await serverConn.OpenAsync();
            var serverVersion = serverConn.ServerVersion;

            // 2. Cria o banco se solicitado e não existir
            if (config.AutoCreateDatabase)
            {
                var createSql = $"CREATE DATABASE IF NOT EXISTS `{config.Database}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";
                await using var cmd = new MySqlCommand(createSql, serverConn);
                await cmd.ExecuteNonQueryAsync();
            }

            // 3. Testa conexão no banco específico
            await using var dbConn = new MySqlConnection(config.GetDatabaseConnectionString());
            await dbConn.OpenAsync();

            return (true, $"Conexão estabelecida com sucesso! MySQL Versão: {serverVersion}", serverVersion);
        }
        catch (Exception ex)
        {
            return (false, $"Falha ao conectar: {ex.Message}", null);
        }
    }
}

