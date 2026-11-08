using System;

namespace MiBandDesktop.App.Models;

public class DiscoveredBleDevice
{
    public string Name { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public ulong BluetoothAddress { get; set; }
    public short Rssi { get; set; }
    public bool IsMiBand { get; set; }
    public DateTime LastSeen { get; set; } = DateTime.Now;

    public string DisplaySubtitle => $"{MacAddress}  •  Sinal: {Rssi} dBm";
}

