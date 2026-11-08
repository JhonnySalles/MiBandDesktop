using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using MiBandDesktop.Core.Configuration;
using MiBandDesktop.Infrastructure.Migrations;
using MiBandDesktop.Infrastructure.Services;

namespace MiBandDesktop_App.Views;

public sealed partial class DatabaseSetupWindow : Window
{
    private readonly Action _onSuccessCallback;

    public DatabaseSetupWindow(Action onSuccessCallback, string? initialErrorMessage = null)
    {
        _onSuccessCallback = onSuccessCallback;
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        MiBandDesktop.App.Services.WindowIconHelper.SetWindowIcon(this, "AppIcon.ico");

        LoadCurrentConfig();

        if (!string.IsNullOrEmpty(initialErrorMessage))
        {
            TxtStatusDetail.Text = initialErrorMessage;
            StatusDot.Fill = new SolidColorBrush(ColorHelper.FromArgb(255, 239, 68, 68)); // Vermelho
        }
    }

    private void LoadCurrentConfig()
    {
        var cfg = DatabaseConfigService.Load() ?? new DatabaseConfig();
        TxtHost.Text = cfg.Host;
        TxtPort.Text = cfg.Port.ToString();
        TxtDatabase.Text = cfg.Database;
        TxtUsername.Text = cfg.Username;
        TxtPassword.Password = cfg.Password;
        SwAutoCreate.IsOn = cfg.AutoCreateDatabase;

        int sslIndex = cfg.SslMode switch
        {
            "None" => 1,
            "Required" => 2,
            _ => 0
        };
        CmbSslMode.SelectedIndex = sslIndex;
    }

    private DatabaseConfig BuildConfigFromUi()
    {
        int.TryParse(TxtPort.Text, out var port);
        if (port <= 0) port = 3306;

        var sslModeItem = CmbSslMode.SelectedItem as ComboBoxItem;
        var sslMode = sslModeItem?.Content?.ToString() ?? "Preferred";

        return new DatabaseConfig
        {
            Host = TxtHost.Text.Trim(),
            Port = port,
            Database = TxtDatabase.Text.Trim(),
            Username = TxtUsername.Text.Trim(),
            Password = TxtPassword.Password,
            SslMode = sslMode,
            AutoCreateDatabase = SwAutoCreate.IsOn
        };
    }

    private async void BtnTestConnection_Click(object sender, RoutedEventArgs e)
    {
        BtnTestConnection.IsEnabled = false;
        TxtLogOutput.Text = "[INFO] Testando conexão com o servidor MySQL...\n";

        var cfg = BuildConfigFromUi();
        var (success, msg, _) = await DatabaseConfigService.TestConnectionAsync(cfg);

        if (success)
        {
            StatusDot.Fill = new SolidColorBrush(ColorHelper.FromArgb(255, 34, 197, 94)); // Verde
            TxtStatusTitle.Text = "Conexão Bem-Sucedida!";
            TxtStatusDetail.Text = msg;
            TxtLogOutput.Text += $"[SUCESSO] {msg}\n";
        }
        else
        {
            StatusDot.Fill = new SolidColorBrush(ColorHelper.FromArgb(255, 239, 68, 68)); // Vermelho
            TxtStatusTitle.Text = "Falha na Conexão";
            TxtStatusDetail.Text = msg;
            TxtLogOutput.Text += $"[ERRO] {msg}\n";
        }

        BtnTestConnection.IsEnabled = true;
    }

    private async void BtnSaveAndMigrate_Click(object sender, RoutedEventArgs e)
    {
        BtnSaveAndMigrate.IsEnabled = false;
        BtnTestConnection.IsEnabled = false;

        var cfg = BuildConfigFromUi();

        TxtLogOutput.Text = "[INFO] Validando servidor e garantindo schema do banco...\n";
        var (testOk, testMsg, _) = await DatabaseConfigService.TestConnectionAsync(cfg);

        if (!testOk)
        {
            StatusDot.Fill = new SolidColorBrush(ColorHelper.FromArgb(255, 239, 68, 68));
            TxtStatusTitle.Text = "Não foi possível conectar";
            TxtStatusDetail.Text = testMsg;
            TxtLogOutput.Text += $"[ERRO] {testMsg}\n";
            BtnSaveAndMigrate.IsEnabled = true;
            BtnTestConnection.IsEnabled = true;
            return;
        }

        // 1. Salva configuração no JSON
        DatabaseConfigService.Save(cfg);
        TxtLogOutput.Text += $"[INFO] Configurações salvas em database.config.json com sucesso.\n";

        // 2. Executa migrações Flyway
        TxtLogOutput.Text += $"[INFO] Iniciando motor de migrações Flyway...\n";
        var migrationResult = await FlywayMigrationRunner.MigrateAsync(cfg, log =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                TxtLogOutput.Text += $"[FLYWAY] {log}\n";
            });
        });

        if (!migrationResult.Success)
        {
            StatusDot.Fill = new SolidColorBrush(ColorHelper.FromArgb(255, 239, 68, 68));
            TxtStatusTitle.Text = "Erro nas Migrações";
            TxtStatusDetail.Text = migrationResult.Message;
            BtnSaveAndMigrate.IsEnabled = true;
            BtnTestConnection.IsEnabled = true;
            return;
        }

        TxtLogOutput.Text += $"[SUCESSO] Banco de dados inicializado e atualizado. Abrindo aplicação...\n";

        // 3. Fecha janela de configuração e abre aplicação principal
        _onSuccessCallback();
        this.Close();
    }
}

