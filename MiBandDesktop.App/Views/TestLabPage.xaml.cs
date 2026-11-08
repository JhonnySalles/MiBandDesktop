using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MiBandDesktop.App.Services;
using MiBandDesktop.Infrastructure.Data;

namespace MiBandDesktop_App.Views;

public sealed partial class TestLabPage : Page
{
    public TestLabPage()
    {
        this.InitializeComponent();

        BleManagerService.Instance.LogMessageReceived += msg =>
        {
            DispatcherQueue.TryEnqueue(() => AppendLog(msg));
        };
        BleManagerService.Instance.DeviceDiscovered += dev =>
        {
            DispatcherQueue.TryEnqueue(() => AppendLog($"[BLE-DISCOVERED] {dev.Name} [{dev.MacAddress}] RSSI: {dev.Rssi} dBm (MiBand: {dev.IsMiBand})"));
        };
        BleManagerService.Instance.ScanningStatusChanged += scanning =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                BtnToggleScan.Content = scanning ? "Parar Scanner BLE" : "Iniciar Scanner BLE";
            });
        };
    }

    private void AppendLog(string message)
    {
        TxtTerminal.Text += $"\n[{DateTime.Now:HH:mm:ss}] {message}";
    }

    private void BtnClearLog_Click(object sender, RoutedEventArgs e)
    {
        TxtTerminal.Text = $"[{DateTime.Now:HH:mm:ss}] Terminal limpo.";
    }

    private void BtnToggleScan_Click(object sender, RoutedEventArgs e)
    {
        if (BleManagerService.Instance.IsScanning)
        {
            BleManagerService.Instance.StopScanning();
        }
        else
        {
            AppendLog("[BLE] Iniciando varredura BLE...");
            BleManagerService.Instance.StartScanning();
        }
    }

    private async void BtnReadBattery_Click(object sender, RoutedEventArgs e)
    {
        var mac = await GetActiveMacAsync();
        if (string.IsNullOrWhiteSpace(mac))
        {
            AppendLog("[AVISO] Nenhuma pulseira ativa cadastrada no banco.");
            return;
        }

        AppendLog($"[BATERIA] Solicitando nível de bateria de {mac} via BLE...");
        var (ok, batt, msg) = await BleManagerService.Instance.ReadBatteryLevelAsync(mac);
        AppendLog(ok ? $"[SUCESSO] {msg}" : $"[FALHA] {msg}");
    }

    private async void BtnPingDevice_Click(object sender, RoutedEventArgs e)
    {
        var mac = await GetActiveMacAsync();
        if (string.IsNullOrWhiteSpace(mac))
        {
            AppendLog("[AVISO] Nenhuma pulseira ativa cadastrada no banco.");
            return;
        }

        AppendLog($"[CONEXÃO] Testando conexão com {mac}...");
        var (ok, msg) = await BleManagerService.Instance.PingDeviceAsync(mac);
        AppendLog(ok ? $"[SUCESSO] {msg}" : $"[FALHA] {msg}");
    }

    private void BtnTestConnection_Click(object sender, RoutedEventArgs e)
    {
        BtnPingDevice_Click(sender, e);
    }

    private static async System.Threading.Tasks.Task<string?> GetActiveMacAsync()
    {
        try
        {
            using var db = new AppDbContext();
            var dev = await db.DeviceConfigs.FirstOrDefaultAsync(d => d.IsActive);
            return dev?.MacAddress;
        }
        catch
        {
            return null;
        }
    }
}
