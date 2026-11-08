using System.IO;
using Xunit;
using MiBandDesktop.Core.Configuration;
using MiBandDesktop.Infrastructure.Migrations;
using MiBandDesktop.Infrastructure.Services;

namespace MiBandDesktop.Tests;

public class DatabaseAndFlywayTests
{
    [Fact]
    public void TestDatabaseConfigConnectionStrings()
    {
        var config = new DatabaseConfig
        {
            Host = "127.0.0.1",
            Port = 3306,
            Database = "miband_test",
            Username = "app_user",
            Password = "secret_password",
            SslMode = "Preferred"
        };

        Assert.True(config.IsValid());

        var serverConn = config.GetServerConnectionString();
        Assert.Contains("Server=127.0.0.1", serverConn);
        Assert.Contains("Port=3306", serverConn);
        Assert.Contains("User ID=app_user", serverConn);
        Assert.DoesNotContain("Database=", serverConn);

        var dbConn = config.GetDatabaseConnectionString();
        Assert.Contains("Database=miband_test", dbConn);
    }

    [Fact]
    public void TestFlywayScriptsDiscoveryAndChecksum()
    {
        var scripts = FlywayMigrationRunner.GetAvailableScripts();
        Assert.NotEmpty(scripts);

        var v1 = scripts.Find(s => s.Version == 1);
        Assert.NotNull(v1.FileName);
        Assert.Contains("CREATE TABLE IF NOT EXISTS `device_configs`", v1.SqlContent);
        Assert.True(v1.Checksum != 0);
    }
}
