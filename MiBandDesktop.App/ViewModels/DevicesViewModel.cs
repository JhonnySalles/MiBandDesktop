using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Dispatching;
using MiBandDesktop.App.Models;
using MiBandDesktop.App.Services;
using MiBandDesktop.Core.Entities;
using MiBandDesktop.Infrastructure.Data;

namespace MiBandDesktop.App.ViewModels;

public partial class DevicesViewModel : ObservableObject
{
    private readonly DispatcherQueue _dispatcherQueue;

    [ObservableProperty]
    private ObservableCollection<DiscoveredBleDevice> _discoveredDevices = [];

    [ObservableProperty]
    private DiscoveredBleDevice? _selectedDiscoveredDevice;

    [ObservableProperty]
    private string _deviceName = "Mi Smart Band 6";

    [ObservableProperty]
    private string _macAddress = string.Empty;

    [ObservableProperty]
    private string _authKey = string.Empty;

    [ObservableProperty]
    private bool _isScanning = false;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private ObservableCollection<DeviceConfig> _savedDevices = [];

    public DevicesViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        BleManagerService.Instance.DeviceDiscovered += OnDeviceDiscovered;
        BleManagerService.Instance.ScanningStatusChanged += OnScanningStatusChanged;
        BleManagerService.Instance.LogMessageReceived += OnLogMessageReceived;
    }

    private void OnDeviceDiscovered(DiscoveredBleDevice device)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            var existing = DiscoveredDevices.FirstOrDefault(d => d.BluetoothAddress == device.BluetoothAddress);
            if (existing == null)
            {
                DiscoveredDevices.Add(device);
            }
            else
            {
                int idx = DiscoveredDevices.IndexOf(existing);
                DiscoveredDevices[idx] = device;
            }
        });
    }

    private void OnScanningStatusChanged(bool scanning)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            IsScanning = scanning;
            StatusMessage = scanning ? "Buscando dispositivos Bluetooth LE próximos..." : "Busca finalizada.";
        });
    }

    private void OnLogMessageReceived(string msg)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            StatusMessage = msg;
        });
    }

    partial void OnSelectedDiscoveredDeviceChanged(DiscoveredBleDevice? value)
    {
        if (value != null)
        {
            MacAddress = value.MacAddress;
            if (!string.IsNullOrWhiteSpace(value.Name) && value.Name != "Dispositivo BLE Desconhecido")
            {
                DeviceName = value.Name;
            }
        }
    }

    [RelayCommand]
    public void ToggleScan()
    {
        if (IsScanning)
        {
            BleManagerService.Instance.StopScanning();
        }
        else
        {
            DiscoveredDevices.Clear();
            BleManagerService.Instance.StartScanning();
        }
    }

    [RelayCommand]
    public async Task LoadSavedDevicesAsync()
    {
        try
        {
            using var db = new AppDbContext();
            var list = await db.DeviceConfigs.OrderByDescending(d => d.IsActive).ToListAsync();
            SavedDevices.Clear();
            foreach (var item in list)
            {
                SavedDevices.Add(item);
            }

            var active = list.FirstOrDefault(d => d.IsActive);
            if (active != null)
            {
                DeviceName = active.DeviceName;
                MacAddress = active.MacAddress;
                AuthKey = active.AuthKey;
            }
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Erro ao carregar dispositivos salvos: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task SaveDeviceAsync()
    {
        if (string.IsNullOrWhiteSpace(MacAddress))
        {
            StatusMessage = "Informe o endereço MAC da pulseira.";
            return;
        }

        try
        {
            using var db = new AppDbContext();
            var existing = await db.DeviceConfigs.FirstOrDefaultAsync(d => d.MacAddress == MacAddress);
            if (existing == null)
            {
                existing = new DeviceConfig
                {
                    MacAddress = MacAddress.Trim().ToUpperInvariant(),
                    DeviceName = string.IsNullOrWhiteSpace(DeviceName) ? "Mi Smart Band 6" : DeviceName.Trim(),
                    AuthKey = AuthKey?.Trim() ?? string.Empty,
                    IsActive = true,
                    CreatedAt = System.DateTime.UtcNow,
                    UpdatedAt = System.DateTime.UtcNow
                };
                db.DeviceConfigs.Add(existing);
            }
            else
            {
                existing.DeviceName = string.IsNullOrWhiteSpace(DeviceName) ? existing.DeviceName : DeviceName.Trim();
                existing.AuthKey = AuthKey?.Trim() ?? existing.AuthKey;
                existing.IsActive = true;
                existing.UpdatedAt = System.DateTime.UtcNow;
            }

            await db.SaveChangesAsync();
            StatusMessage = $"Pulseira {existing.DeviceName} ({existing.MacAddress}) salva com sucesso!";
            await LoadSavedDevicesAsync();
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Erro ao salvar pulseira: {ex.Message}";
        }
    }
}
