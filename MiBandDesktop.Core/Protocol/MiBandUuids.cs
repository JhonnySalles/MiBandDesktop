using System;

namespace MiBandDesktop.Core.Protocol;

public static class MiBandUuids
{
    // Services
    public static readonly Guid ServiceHuami2021 = Guid.Parse("0000fee0-0000-3512-2118-0009af100700");
    public static readonly Guid ServiceHuamiLegacy = Guid.Parse("0000fee0-0000-1000-8000-00805f9b34fb");
    public static readonly Guid ServiceLegacyAuth = Guid.Parse("0000fee1-0000-1000-8000-00805f9b34fb");
    public static readonly Guid ServiceBattery = Guid.Parse("0000180f-0000-1000-8000-00805f9b34fb");
    public static readonly Guid ServiceHeartRate = Guid.Parse("0000180d-0000-1000-8000-00805f9b34fb");
    public static readonly Guid ServiceCurrentTime = Guid.Parse("00001805-0000-1000-8000-00805f9b34fb");
    public static readonly Guid ServiceImmediateAlert = Guid.Parse("00001802-0000-1000-8000-00805f9b34fb");
    public static readonly Guid ServiceAlertNotification = Guid.Parse("00001811-0000-1000-8000-00805f9b34fb");
    public static readonly Guid ServiceDeviceInformation = Guid.Parse("0000180a-0000-1000-8000-00805f9b34fb");

    // Huami 2021 Characteristics
    public static readonly Guid CharChunkedWrite = Guid.Parse("00000016-0000-3512-2118-0009af100700");
    public static readonly Guid CharChunkedRead = Guid.Parse("00000017-0000-3512-2118-0009af100700");
    public static readonly Guid CharOldChunkedWrite = Guid.Parse("00000020-0000-3512-2118-0009af100700");

    // Huami Legacy Characteristics
    public static readonly Guid CharSteps = Guid.Parse("00000007-0000-3512-2118-0009af100700");
    public static readonly Guid CharBatteryDetails = Guid.Parse("00000006-0000-3512-2118-0009af100700");
    public static readonly Guid CharActivityControl = Guid.Parse("00000005-0000-3512-2118-0009af100700");
    public static readonly Guid CharActivityData = Guid.Parse("00000004-0000-3512-2118-0009af100700");
    public static readonly Guid CharHuamiAlert = Guid.Parse("00000003-0000-3512-2118-0009af100700");
    public static readonly Guid CharLegacyAuth = Guid.Parse("00000009-0000-3512-2118-0009af100700");

    // Standard BLE Characteristics
    public static readonly Guid CharStandardBattery = Guid.Parse("00002a19-0000-1000-8000-00805f9b34fb");
    public static readonly Guid CharCurrentTime = Guid.Parse("00002a2b-0000-1000-8000-00805f9b34fb");
    public static readonly Guid CharHeartRateMeasure = Guid.Parse("00002a37-0000-1000-8000-00805f9b34fb");
    public static readonly Guid CharHeartRateControl = Guid.Parse("00002a39-0000-1000-8000-00805f9b34fb");
    public static readonly Guid CharImmediateAlert = Guid.Parse("00002a06-0000-1000-8000-00805f9b34fb");
    public static readonly Guid CharAlertControl = Guid.Parse("00002a44-0000-1000-8000-00805f9b34fb");
    public static readonly Guid CharNewAlert = Guid.Parse("00002a46-0000-1000-8000-00805f9b34fb");

    // DIS
    public static readonly Guid CharDisModelNumber = Guid.Parse("00002a24-0000-1000-8000-00805f9b34fb");
    public static readonly Guid CharDisSerialNumber = Guid.Parse("00002a25-0000-1000-8000-00805f9b34fb");
    public static readonly Guid CharDisFirmwareRevision = Guid.Parse("00002a26-0000-1000-8000-00805f9b34fb");
    public static readonly Guid CharDisHardwareRevision = Guid.Parse("00002a27-0000-1000-8000-00805f9b34fb");
    public static readonly Guid CharDisSoftwareRevision = Guid.Parse("00002a28-0000-1000-8000-00805f9b34fb");

    // Endpoints
    public const ushort EndpointAuth = 0x0082;
    public const ushort EndpointCompat = 0x0090;
}

