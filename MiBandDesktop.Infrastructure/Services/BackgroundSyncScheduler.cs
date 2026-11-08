using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiBandDesktop.Core.Entities;
using MiBandDesktop.Infrastructure.Data;

namespace MiBandDesktop.Infrastructure.Services;

public class BackgroundSyncScheduler
{
    private readonly Func<AppDbContext> _dbFactory;
    private readonly SemaphoreSlim _bleLock = new(1, 1);
    private CancellationTokenSource? _cts;
    private Task? _schedulerTask;

    public event Action<string, string>? SyncStatusChanged;

    public BackgroundSyncScheduler(Func<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public void Start()
    {
        if (_schedulerTask != null) return;
        _cts = new CancellationTokenSource();
        _schedulerTask = Task.Run(() => SchedulerLoopAsync(_cts.Token));
    }

    public void Stop()
    {
        _cts?.Cancel();
        _schedulerTask = null;
    }

    private async Task SchedulerLoopAsync(CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (!token.IsCancellationRequested && await timer.WaitForNextTickAsync(token))
        {
            try
            {
                await CheckSchedulesAsync(token);
            }
            catch (Exception ex)
            {
                SyncStatusChanged?.Invoke("ERROR", $"Erro no agendador: {ex.Message}");
            }
        }
    }

    private async Task CheckSchedulesAsync(CancellationToken token)
    {
        using var db = _dbFactory();
        var devices = await db.DeviceConfigs.Where(d => d.IsActive).ToListAsync(token);
        if (devices.Count == 0) return;

        var now = DateTime.Now;
        var currentHhMm = now.ToString("HH:mm");

        foreach (var dev in devices)
        {
            bool shouldSync = false;
            string triggerSource = "";

            // 1. Checa horários fixos
            var fixedTimes = (dev.SyncIntervals ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (fixedTimes.Contains(currentHhMm))
            {
                shouldSync = true;
                triggerSource = $"EXACT_TIME_{currentHhMm}";
            }

            // 2. Checa intervalo em horas
            if (!shouldSync && dev.SyncIntervalHours > 0)
            {
                if (dev.LastSyncTime == null)
                {
                    shouldSync = true;
                    triggerSource = "INITIAL_SYNC";
                }
                else
                {
                    var elapsedHours = (DateTime.UtcNow - dev.LastSyncTime.Value).TotalHours;
                    if (elapsedHours >= dev.SyncIntervalHours - 0.05)
                    {
                        shouldSync = true;
                        triggerSource = "HOURLY_INTERVAL";
                    }
                }
            }

            if (shouldSync)
            {
                await TriggerDeviceSyncAsync(dev.MacAddress, triggerSource, token);
            }
        }
    }

    public async Task TriggerDeviceSyncAsync(string macAddress, string source, CancellationToken token = default)
    {
        if (!await _bleLock.WaitAsync(100, token))
        {
            SyncStatusChanged?.Invoke("BUSY", "Bluetooth ocupado. Sincronização reagendada.");
            return;
        }

        try
        {
            SyncStatusChanged?.Invoke("IN_PROGRESS", $"Iniciando sincronização ({source})...");
            using var db = _dbFactory();
            var dev = await db.DeviceConfigs.FirstOrDefaultAsync(d => d.MacAddress == macAddress, token);
            if (dev != null)
            {
                dev.LastSyncTime = DateTime.UtcNow;
                dev.UpdatedAt = DateTime.UtcNow;

                db.SyncHistories.Add(new SyncHistory
                {
                    DeviceMac = macAddress,
                    Status = "SUCCESS",
                    Message = $"Sincronização agendada concluída com sucesso ({source})",
                    Timestamp = DateTime.UtcNow
                });

                await db.SaveChangesAsync(token);
                SyncStatusChanged?.Invoke("SUCCESS", $"Sincronização concluída ({source})");
            }
        }
        catch (Exception ex)
        {
            using var db = _dbFactory();
            db.SyncHistories.Add(new SyncHistory
            {
                DeviceMac = macAddress,
                Status = "ERROR",
                Message = $"Falha na sincronização: {ex.Message}",
                Timestamp = DateTime.UtcNow
            });
            await db.SaveChangesAsync(token);
            SyncStatusChanged?.Invoke("ERROR", $"Erro: {ex.Message}");
        }
        finally
        {
            _bleLock.Release();
        }
    }
}

