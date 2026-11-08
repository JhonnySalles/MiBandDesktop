using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;
using MiBandDesktop.App.Models;
using MiBandDesktop.Core.Protocol;

namespace MiBandDesktop.App.Services;

public class BleManagerService
{
    private static BleManagerService? _instance;
    public static BleManagerService Instance => _instance ??= new BleManagerService();

    private BluetoothLEAdvertisementWatcher? _watcher;
    private readonly ConcurrentDictionary<ulong, DiscoveredBleDevice> _devices = new();

    public event Action<DiscoveredBleDevice>? DeviceDiscovered;
    public event Action<bool>? ScanningStatusChanged;
    public event Action<string>? LogMessageReceived;

    public bool IsScanning => _watcher != null && _watcher.Status == BluetoothLEAdvertisementWatcherStatus.Started;

    public IReadOnlyCollection<DiscoveredBleDevice> DiscoveredDevices => _devices.Values.ToList();

    public void StartScanning()
    {
        StopScanning();

        _devices.Clear();
        _watcher = new BluetoothLEAdvertisementWatcher
        {
            ScanningMode = BluetoothLEScanningMode.Active
        };

        _watcher.Received += OnAdvertisementReceived;
        _watcher.Stopped += (s, e) =>
        {
            ScanningStatusChanged?.Invoke(false);
            LogMessageReceived?.Invoke($"[BLE] Scanner finalizado. Status: {e.Error}");
        };

        try
        {
            _watcher.Start();
            ScanningStatusChanged?.Invoke(true);
            LogMessageReceived?.Invoke("[BLE] Scanner Bluetooth LE iniciado.");
        }
        catch (Exception ex)
        {
            LogMessageReceived?.Invoke($"[BLE] Erro ao iniciar scanner: {ex.Message}");
            ScanningStatusChanged?.Invoke(false);
        }
    }

    public void StopScanning()
    {
        if (_watcher != null)
        {
            try
            {
                _watcher.Stop();
            }
            catch { }
            _watcher = null;
            ScanningStatusChanged?.Invoke(false);
            LogMessageReceived?.Invoke("[BLE] Scanner Bluetooth LE parado.");
        }
    }

    private void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
    {
        var rawName = args.Advertisement.LocalName;
        var address = args.BluetoothAddress;
        var mac = FormatMacAddress(address);

        bool isMiBand = false;
        if (!string.IsNullOrWhiteSpace(rawName))
        {
            var lower = rawName.ToLowerInvariant();
            if (lower.Contains("mi smart band") || lower.Contains("mi band") || lower.Contains("band") || lower.Contains("smart band"))
            {
                isMiBand = true;
            }
        }

        foreach (var uuid in args.Advertisement.ServiceUuids)
        {
            if (uuid == MiBandUuids.ServiceHuami2021 || uuid == MiBandUuids.ServiceHuamiLegacy || uuid == MiBandUuids.ServiceLegacyAuth)
            {
                isMiBand = true;
                break;
            }
        }

        var deviceName = string.IsNullOrWhiteSpace(rawName) ? (isMiBand ? "Dispositivo Mi Band" : "Dispositivo BLE Desconhecido") : rawName;

        var dev = _devices.AddOrUpdate(address,
            addValueFactory: _ =>
            {
                var newDev = new DiscoveredBleDevice
                {
                    BluetoothAddress = address,
                    MacAddress = mac,
                    Name = deviceName,
                    Rssi = args.RawSignalStrengthInDBm,
                    IsMiBand = isMiBand,
                    LastSeen = DateTime.Now
                };
                DeviceDiscovered?.Invoke(newDev);
                return newDev;
            },
            updateValueFactory: (_, existing) =>
            {
                if (!string.IsNullOrWhiteSpace(rawName)) existing.Name = rawName;
                existing.Rssi = args.RawSignalStrengthInDBm;
                existing.LastSeen = DateTime.Now;
                if (isMiBand) existing.IsMiBand = true;
                DeviceDiscovered?.Invoke(existing);
                return existing;
            });
    }

    public async Task<(bool Success, int? BatteryLevel, string Message)> ReadBatteryLevelAsync(string macAddress)
    {
        var (ok, dev, err) = await ConnectDeviceAsync(macAddress);
        if (!ok || dev == null) return (false, null, err);

        using (dev)
        {
            try
            {
                var servicesResult = await dev.GetGattServicesForUuidAsync(MiBandUuids.ServiceBattery);
                if (servicesResult.Status != GattCommunicationStatus.Success || servicesResult.Services.Count == 0)
                {
                    return (false, null, $"Serviço de bateria não encontrado no dispositivo {macAddress}.");
                }

                var battService = servicesResult.Services[0];
                var charsResult = await battService.GetCharacteristicsForUuidAsync(MiBandUuids.CharStandardBattery);
                if (charsResult.Status != GattCommunicationStatus.Success || charsResult.Characteristics.Count == 0)
                {
                    return (false, null, "Característica de nível de bateria não encontrada.");
                }

                var battChar = charsResult.Characteristics[0];
                var readResult = await battChar.ReadValueAsync(BluetoothCacheMode.Uncached);
                if (readResult.Status != GattCommunicationStatus.Success)
                {
                    return (false, null, $"Falha ao ler característica de bateria: {readResult.Status}");
                }

                var reader = DataReader.FromBuffer(readResult.Value);
                byte level = reader.ReadByte();
                return (true, (int)level, $"Nível de bateria lido com sucesso: {level}%");
            }
            catch (Exception ex)
            {
                return (false, null, $"Erro ao consultar bateria: {ex.Message}");
            }
        }
    }

    public async Task<(bool Success, string Message)> PingDeviceAsync(string macAddress)
    {
        var (ok, dev, err) = await ConnectDeviceAsync(macAddress);
        if (!ok || dev == null) return (false, err);

        using (dev)
        {
            return (true, $"Pulseira {dev.Name} ({macAddress}) conectada com sucesso!");
        }
    }

    private async Task<(bool Success, BluetoothLEDevice? Device, string Error)> ConnectDeviceAsync(string macAddress)
    {
        if (!TryParseMac(macAddress, out ulong address))
        {
            return (false, null, $"Endereço MAC inválido: {macAddress}");
        }

        try
        {
            LogMessageReceived?.Invoke($"[BLE] Conectando a {macAddress}...");
            var device = await BluetoothLEDevice.FromBluetoothAddressAsync(address);
            if (device == null)
            {
                return (false, null, $"Não foi possível encontrar/emparelhar com o dispositivo {macAddress}. Verifique se o Bluetooth do Windows está ativado e a pulseira próxima.");
            }

            return (true, device, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, null, $"Exceção ao conectar a {macAddress}: {ex.Message}");
        }
    }

    public static string FormatMacAddress(ulong address)
    {
        var bytes = BitConverter.GetBytes(address);
        return $"{bytes[5]:X2}:{bytes[4]:X2}:{bytes[3]:X2}:{bytes[2]:X2}:{bytes[1]:X2}:{bytes[0]:X2}";
    }

    public static bool TryParseMac(string mac, out ulong address)
    {
        address = 0;
        if (string.IsNullOrWhiteSpace(mac)) return false;

        var clean = mac.Replace(":", "").Replace("-", "").Trim();
        if (clean.Length != 12) return false;

        try
        {
            address = Convert.ToUInt64(clean, 16);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

