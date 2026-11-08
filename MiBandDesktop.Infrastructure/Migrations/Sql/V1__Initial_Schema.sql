-- V1__Initial_Schema.sql
-- Migração inicial de criação do esquema do MiBandDesktop (Padrão MySQL InnoDB / utf8mb4)

CREATE TABLE IF NOT EXISTS `device_configs` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `MacAddress` VARCHAR(50) NOT NULL,
    `AuthKey` VARCHAR(100) NOT NULL,
    `DeviceName` VARCHAR(100) NOT NULL DEFAULT 'Mi Smart Band 6',
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    `SyncIntervalHours` INT NOT NULL DEFAULT 1,
    `SyncIntervals` VARCHAR(255) NOT NULL DEFAULT '08:00,12:00,18:00,22:00',
    `AutoWeather` TINYINT(1) NOT NULL DEFAULT 1,
    `LastSyncTime` DATETIME NULL,
    `CreatedAt` DATETIME NOT NULL,
    `UpdatedAt` DATETIME NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UX_device_configs_MacAddress` (`MacAddress`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `sync_schedules` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `DeviceMac` VARCHAR(50) NOT NULL,
    `ScheduledTime` VARCHAR(10) NOT NULL DEFAULT '08:00',
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    `CreatedAt` DATETIME NOT NULL,
    PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `integration_configs` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `WeatherProvider` VARCHAR(50) NOT NULL DEFAULT 'open-meteo',
    `WeatherApiToken` VARCHAR(255) NULL,
    `WeatherCity` VARCHAR(100) NOT NULL DEFAULT 'São Paulo',
    `Latitude` DOUBLE NOT NULL DEFAULT -23.5505,
    `Longitude` DOUBLE NOT NULL DEFAULT -46.6333,
    `CacheTtlMinutes` INT NOT NULL DEFAULT 30,
    `UpdatedAt` DATETIME NOT NULL,
    PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `activity_logs` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `DeviceMac` VARCHAR(50) NOT NULL,
    `Timestamp` DATETIME NOT NULL,
    `Steps` INT NOT NULL,
    `DistanceMeters` INT NOT NULL,
    `Calories` INT NOT NULL,
    `HeartRate` INT NULL,
    `BatteryLevel` INT NULL,
    `Spo2` INT NULL,
    `Stress` INT NULL,
    `PaiToday` INT NULL,
    `PaiTotal` INT NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_activity_logs_Device_Time` (`DeviceMac`, `Timestamp`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `sync_histories` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `DeviceMac` VARCHAR(50) NULL,
    `Timestamp` DATETIME NOT NULL,
    `Status` VARCHAR(20) NOT NULL DEFAULT 'SUCCESS',
    `Message` TEXT NULL,
    `Details` LONGTEXT NULL,
    PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `weather_histories` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `Timestamp` DATETIME NOT NULL,
    `City` VARCHAR(100) NOT NULL,
    `Temp` INT NOT NULL,
    `TempMin` INT NULL,
    `TempMax` INT NULL,
    `Condition` VARCHAR(100) NOT NULL,
    `Humidity` INT NULL,
    `WindSpeed` INT NULL,
    `WeatherCode` INT NULL,
    `Provider` VARCHAR(50) NOT NULL DEFAULT 'open-meteo',
    `Source` VARCHAR(50) NOT NULL DEFAULT 'SYNC_AUTO',
    `CreatedAt` DATETIME NOT NULL,
    PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `band_settings` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `DeviceMac` VARCHAR(50) NOT NULL,
    `EventRemindersJson` LONGTEXT NULL,
    `AlarmsJson` LONGTEXT NULL,
    `WorldClocksJson` LONGTEXT NULL,
    `LiftWristMode` VARCHAR(30) NOT NULL DEFAULT 'all_day',
    `LiftWristStart` VARCHAR(10) NOT NULL DEFAULT '07:00',
    `LiftWristEnd` VARCHAR(10) NOT NULL DEFAULT '22:00',
    `LiftWristSpeed` VARCHAR(20) NOT NULL DEFAULT 'normal',
    `NightMode` VARCHAR(30) NOT NULL DEFAULT 'off',
    `NightModeStart` VARCHAR(10) NOT NULL DEFAULT '22:00',
    `NightModeEnd` VARCHAR(10) NOT NULL DEFAULT '07:00',
    `DndMode` VARCHAR(30) NOT NULL DEFAULT 'off',
    `WearLocation` VARCHAR(20) NOT NULL DEFAULT 'left',
    `Language` VARCHAR(20) NOT NULL DEFAULT 'pt_BR',
    `HeartRateInterval` INT NOT NULL DEFAULT 5,
    `StepGoal` INT NOT NULL DEFAULT 8000,
    `CalorieGoal` INT NOT NULL DEFAULT 300,
    `AllDayStress` TINYINT(1) NOT NULL DEFAULT 1,
    `StressInterval` INT NOT NULL DEFAULT 5,
    `UpdatedAt` DATETIME NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UX_band_settings_DeviceMac` (`DeviceMac`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `intraday_activity_samples` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `DeviceMac` VARCHAR(50) NOT NULL,
    `Timestamp` DATETIME NOT NULL,
    `Category` INT NOT NULL,
    `Steps` INT NOT NULL,
    `Intensity` INT NOT NULL,
    `HeartRate` INT NULL,
    `Spo2` INT NULL,
    `Stress` INT NULL,
    `CreatedAt` DATETIME NOT NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_intraday_samples_Device_Time` (`DeviceMac`, `Timestamp`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `sleep_summaries` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `DeviceMac` VARCHAR(50) NOT NULL,
    `Date` VARCHAR(20) NOT NULL,
    `StartTime` DATETIME NOT NULL,
    `EndTime` DATETIME NOT NULL,
    `TotalSleepMinutes` INT NOT NULL,
    `DeepSleepMinutes` INT NOT NULL,
    `LightSleepMinutes` INT NOT NULL,
    `RemSleepMinutes` INT NOT NULL,
    `AwakeMinutes` INT NOT NULL,
    `CreatedAt` DATETIME NOT NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_sleep_summaries_Device_Date` (`DeviceMac`, `Date`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

