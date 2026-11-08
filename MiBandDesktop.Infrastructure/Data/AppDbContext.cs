using System;
using Microsoft.EntityFrameworkCore;
using MiBandDesktop.Core.Configuration;
using MiBandDesktop.Core.Entities;
using MiBandDesktop.Infrastructure.Services;

namespace MiBandDesktop.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<DeviceConfig> DeviceConfigs => Set<DeviceConfig>();
    public DbSet<SyncSchedule> SyncSchedules => Set<SyncSchedule>();
    public DbSet<IntegrationConfig> IntegrationConfigs => Set<IntegrationConfig>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<SyncHistory> SyncHistories => Set<SyncHistory>();
    public DbSet<WeatherHistory> WeatherHistories => Set<WeatherHistory>();
    public DbSet<BandSettings> BandSettings => Set<BandSettings>();
    public DbSet<IntradayActivitySample> IntradayActivitySamples => Set<IntradayActivitySample>();
    public DbSet<SleepSummary> SleepSummaries => Set<SleepSummary>();

    private readonly string? _connectionString;

    public AppDbContext()
    {
        var config = DatabaseConfigService.Load() ?? new DatabaseConfig();
        _connectionString = config.GetDatabaseConnectionString();
    }

    public AppDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured && !string.IsNullOrEmpty(_connectionString))
        {
            var serverVersion = ServerVersion.AutoDetect(_connectionString);
            optionsBuilder.UseMySql(_connectionString, serverVersion);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DeviceConfig>(entity =>
        {
            entity.ToTable("device_configs");
            entity.HasIndex(d => d.MacAddress).IsUnique();
        });

        modelBuilder.Entity<SyncSchedule>(entity =>
        {
            entity.ToTable("sync_schedules");
        });

        modelBuilder.Entity<IntegrationConfig>(entity =>
        {
            entity.ToTable("integration_configs");
        });

        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.ToTable("activity_logs");
            entity.HasIndex(a => new { a.DeviceMac, a.Timestamp });
        });

        modelBuilder.Entity<SyncHistory>(entity =>
        {
            entity.ToTable("sync_histories");
        });

        modelBuilder.Entity<WeatherHistory>(entity =>
        {
            entity.ToTable("weather_histories");
        });

        modelBuilder.Entity<BandSettings>(entity =>
        {
            entity.ToTable("band_settings");
            entity.HasIndex(b => b.DeviceMac).IsUnique();
        });

        modelBuilder.Entity<IntradayActivitySample>(entity =>
        {
            entity.ToTable("intraday_activity_samples");
            entity.HasIndex(i => new { i.DeviceMac, i.Timestamp });
        });

        modelBuilder.Entity<SleepSummary>(entity =>
        {
            entity.ToTable("sleep_summaries");
            entity.HasIndex(s => new { s.DeviceMac, s.Date });
        });
    }
}
