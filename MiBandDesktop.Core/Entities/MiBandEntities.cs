using System;
using System.ComponentModel.DataAnnotations;

namespace MiBandDesktop.Core.Entities;

public class DeviceConfig
{
    [Key]
    public int Id { get; set; }
    public string MacAddress { get; set; } = string.Empty;
    public string AuthKey { get; set; } = string.Empty;
    public string DeviceName { get; set; } = "Mi Smart Band 6";
    public bool IsActive { get; set; } = true;
    public int SyncIntervalHours { get; set; } = 1;
    public string SyncIntervals { get; set; } = "08:00,12:00,18:00,22:00";
    public bool AutoWeather { get; set; } = true;
    public DateTime? LastSyncTime { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class SyncSchedule
{
    [Key]
    public int Id { get; set; }
    public string DeviceMac { get; set; } = string.Empty;
    public string ScheduledTime { get; set; } = "08:00"; // HH:mm
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class IntegrationConfig
{
    [Key]
    public int Id { get; set; }
    public string WeatherProvider { get; set; } = "open-meteo"; // "open-meteo", "openweathermap", "weatherapi"
    public string? WeatherApiToken { get; set; }
    public string WeatherCity { get; set; } = "São Paulo";
    public double Latitude { get; set; } = -23.5505;
    public double Longitude { get; set; } = -46.6333;
    public int CacheTtlMinutes { get; set; } = 30;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class ActivityLog
{
    [Key]
    public int Id { get; set; }
    public string DeviceMac { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int Steps { get; set; }
    public int DistanceMeters { get; set; }
    public int Calories { get; set; }
    public int? HeartRate { get; set; }
    public int? BatteryLevel { get; set; }
    public int? Spo2 { get; set; }
    public int? Stress { get; set; }
    public int? PaiToday { get; set; }
    public int? PaiTotal { get; set; }
}

public class SyncHistory
{
    [Key]
    public int Id { get; set; }
    public string? DeviceMac { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "SUCCESS"; // SUCCESS, ERROR, IN_PROGRESS
    public string? Message { get; set; }
    public string? Details { get; set; }
}

public class WeatherHistory
{
    [Key]
    public int Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string City { get; set; } = string.Empty;
    public int Temp { get; set; }
    public int? TempMin { get; set; }
    public int? TempMax { get; set; }
    public string Condition { get; set; } = string.Empty;
    public int? Humidity { get; set; }
    public int? WindSpeed { get; set; }
    public int? WeatherCode { get; set; }
    public string Provider { get; set; } = "open-meteo";
    public string Source { get; set; } = "SYNC_AUTO"; // SYNC_AUTO, SYNC_MANUAL, PREVIEW, MANUAL
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class BandSettings
{
    [Key]
    public int Id { get; set; }
    public string DeviceMac { get; set; } = string.Empty;
    public string EventRemindersJson { get; set; } = "[]";
    public string AlarmsJson { get; set; } = "[]";
    public string WorldClocksJson { get; set; } = "[]";
    public string LiftWristMode { get; set; } = "all_day"; // off, all_day, scheduled
    public string LiftWristStart { get; set; } = "07:00";
    public string LiftWristEnd { get; set; } = "22:00";
    public string LiftWristSpeed { get; set; } = "normal"; // normal, fast
    public string NightMode { get; set; } = "off"; // off, sunset, scheduled
    public string NightModeStart { get; set; } = "22:00";
    public string NightModeEnd { get; set; } = "07:00";
    public string DndMode { get; set; } = "off"; // off, always, smart, scheduled
    public string WearLocation { get; set; } = "left"; // left, right
    public string Language { get; set; } = "pt_BR";
    public int HeartRateInterval { get; set; } = 5;
    public int StepGoal { get; set; } = 8000;
    public int CalorieGoal { get; set; } = 300;
    public bool AllDayStress { get; set; } = true;
    public int StressInterval { get; set; } = 5;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class IntradayActivitySample
{
    [Key]
    public int Id { get; set; }
    public string DeviceMac { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int Category { get; set; }
    public int Steps { get; set; }
    public int Intensity { get; set; }
    public int? HeartRate { get; set; }
    public int? Spo2 { get; set; }
    public int? Stress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class SleepSummary
{
    [Key]
    public int Id { get; set; }
    public string DeviceMac { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty; // YYYY-MM-DD
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int TotalSleepMinutes { get; set; }
    public int DeepSleepMinutes { get; set; }
    public int LightSleepMinutes { get; set; }
    public int RemSleepMinutes { get; set; }
    public int AwakeMinutes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

