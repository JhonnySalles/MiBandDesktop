using System;

namespace MiBandDesktop.Core.Configuration;

public class DatabaseConfig
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 3306;
    public string Database { get; set; } = "miband_desktop";
    public string Username { get; set; } = "root";
    public string Password { get; set; } = string.Empty;
    public string SslMode { get; set; } = "Preferred"; // Preferred, None, Required
    public int ConnectionTimeout { get; set; } = 10;
    public bool AutoCreateDatabase { get; set; } = true;

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Host) &&
               Port > 0 &&
               !string.IsNullOrWhiteSpace(Database) &&
               !string.IsNullOrWhiteSpace(Username);
    }

    public string GetServerConnectionString()
    {
        return $"Server={Host};Port={Port};User ID={Username};Password={Password};SslMode={SslMode};ConnectionTimeout={ConnectionTimeout};CharSet=utf8mb4;AllowUserVariables=True;";
    }

    public string GetDatabaseConnectionString()
    {
        return $"Server={Host};Port={Port};Database={Database};User ID={Username};Password={Password};SslMode={SslMode};ConnectionTimeout={ConnectionTimeout};CharSet=utf8mb4;AllowUserVariables=True;";
    }
}

