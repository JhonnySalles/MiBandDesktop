using System;
using Microsoft.UI.Xaml;
using MiBandDesktop.Core.Configuration;
using MiBandDesktop.Infrastructure.Migrations;
using MiBandDesktop.Infrastructure.Services;
using MiBandDesktop_App.Views;

namespace MiBandDesktop_App;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private Window? _window;
    public Window? MainWindow => _window;

    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += (s, e) => LogError("AppDomain", e.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (s, e) => LogError("TaskScheduler", e.Exception);
        this.UnhandledException += (s, e) => { LogError("Xaml", e.Exception); e.Handled = true; };

        InitializeComponent();
    }

    public static void LogError(string source, Exception? ex)
    {
        try
        {
            var logPath = System.IO.Path.Combine(AppContext.BaseDirectory, "error_log.txt");
            var content = $"[{DateTime.Now}] [{source}]\n{ex}\n{new string('-', 50)}\n";
            System.IO.File.AppendAllText(logPath, content);
        }
        catch { }
    }

    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        var dbConfig = DatabaseConfigService.Load();

        if (dbConfig == null)
        {
            OpenDatabaseSetupWindow("Nenhum arquivo de configuração de banco de dados (database.config.json) encontrado.");
            return;
        }

        // Testa conexão com o banco MySQL
        var (success, message, _) = await DatabaseConfigService.TestConnectionAsync(dbConfig);
        if (!success)
        {
            OpenDatabaseSetupWindow($"Não foi possível conectar ao MySQL em {dbConfig.Host}:{dbConfig.Port}: {message}");
            return;
        }

        // Executa migrações Flyway
        var migrationResult = await FlywayMigrationRunner.MigrateAsync(dbConfig, log =>
        {
            System.Diagnostics.Debug.WriteLine($"[Flyway] {log}");
        });

        if (!migrationResult.Success)
        {
            OpenDatabaseSetupWindow($"Falha ao aplicar migrações no banco: {migrationResult.Message}");
            return;
        }

        OpenMainWindow();
    }

    private void OpenDatabaseSetupWindow(string reason)
    {
        _window = new DatabaseSetupWindow(() =>
        {
            OpenMainWindow();
        }, reason);
        _window.Activate();
    }

    private void OpenMainWindow()
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
