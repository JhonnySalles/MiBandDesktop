using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using MiBandDesktop.App.Services;
using MiBandDesktop.Core.Entities;
using MiBandDesktop.Infrastructure.Data;

namespace MiBandDesktop.App.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty]
    private string _stepsToday = "0";

    [ObservableProperty]
    private double _stepGoalPercent = 0;

    [ObservableProperty]
    private string _stepDetails = "0 km (0% da meta)";

    [ObservableProperty]
    private string _heartRate = "-- BPM";

    [ObservableProperty]
    private string _heartRateSubtitle = "Nenhuma leitura registrada";

    [ObservableProperty]
    private string _calories = "0 kcal";

    [ObservableProperty]
    private double _caloriesGoalPercent = 0;

    [ObservableProperty]
    private string _caloriesSubtitle = "Meta: 300 kcal (0%)";

    [ObservableProperty]
    private string _batteryLevel = "N/A";

    [ObservableProperty]
    private double _batteryPercent = 0;

    [ObservableProperty]
    private string _batterySubtitle = "Sem dados de bateria";

    [ObservableProperty]
    private string _spo2 = "--";

    [ObservableProperty]
    private string _spo2Subtitle = "Sem medição";

    [ObservableProperty]
    private string _stress = "-- / 100";

    [ObservableProperty]
    private string _stressSubtitle = "Sem medição";

    [ObservableProperty]
    private string _pai = "0 pts";

    [ObservableProperty]
    private string _paiSubtitle = "Hoje: 0 pts (Meta semanal > 100)";

    [ObservableProperty]
    private string _weatherTemp = "--°C";

    [ObservableProperty]
    private string _weatherCity = "Nenhuma cidade configurada";

    [ObservableProperty]
    private string _weatherCondition = "Sem dados climáticos";

    [ObservableProperty]
    private string _weatherDetails = "--";

    [ObservableProperty]
    private string _syncIntervalDescription = "Sem agendamentos";

    [ObservableProperty]
    private ObservableCollection<string> _scheduledTimes = [];

    [ObservableProperty]
    private string _deviceStatus = "Nenhum dispositivo cadastrado";

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public DashboardViewModel()
    {
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        IsLoading = true;
        StatusMessage = "Carregando dados do banco...";

        try
        {
            using var db = new AppDbContext();

            // 1. Dispositivo Ativo
            var device = await db.DeviceConfigs.FirstOrDefaultAsync(d => d.IsActive);
            if (device != null)
            {
                DeviceStatus = $"{device.DeviceName} ({device.MacAddress})";
                SyncIntervalDescription = $"A cada {device.SyncIntervalHours} hora(s)";

                ScheduledTimes.Clear();
                var times = (device.SyncIntervals ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var t in times)
                {
                    ScheduledTimes.Add(t);
                }

                // 2. Último ActivityLog
                var latestLog = await db.ActivityLogs
                    .Where(a => a.DeviceMac == device.MacAddress)
                    .OrderByDescending(a => a.Timestamp)
                    .FirstOrDefaultAsync();

                // 3. Metas do BandSettings
                var bandSettings = await db.BandSettings
                    .FirstOrDefaultAsync(b => b.DeviceMac == device.MacAddress);

                int stepGoal = bandSettings?.StepGoal ?? 8000;
                int calGoal = bandSettings?.CalorieGoal ?? 300;

                if (latestLog != null)
                {
                    StepsToday = latestLog.Steps.ToString("N0");
                    double distanceKm = latestLog.DistanceMeters / 1000.0;
                    StepGoalPercent = stepGoal > 0 ? Math.Min(100, (latestLog.Steps / (double)stepGoal) * 100) : 0;
                    StepDetails = $"{distanceKm:F1} km ({StepGoalPercent:F0}% da meta de {stepGoal:N0})";

                    HeartRate = latestLog.HeartRate.HasValue ? $"{latestLog.HeartRate.Value} BPM" : "-- BPM";
                    HeartRateSubtitle = latestLog.HeartRate.HasValue
                        ? $"Registrado em {latestLog.Timestamp.ToLocalTime():HH:mm}"
                        : "Sem frequência registrada";

                    Calories = $"{latestLog.Calories} kcal";
                    CaloriesGoalPercent = calGoal > 0 ? Math.Min(100, (latestLog.Calories / (double)calGoal) * 100) : 0;
                    CaloriesSubtitle = $"Meta: {calGoal} kcal ({CaloriesGoalPercent:F0}%)";

                    if (latestLog.BatteryLevel.HasValue)
                    {
                        BatteryLevel = $"{latestLog.BatteryLevel.Value}%";
                        BatteryPercent = latestLog.BatteryLevel.Value;
                        BatterySubtitle = "Nível atual registrado";
                    }
                    else
                    {
                        BatteryLevel = "N/A";
                        BatteryPercent = 0;
                        BatterySubtitle = "Sem leitura de bateria";
                    }

                    if (latestLog.Spo2.HasValue)
                    {
                        Spo2 = $"{latestLog.Spo2.Value}%";
                        Spo2Subtitle = latestLog.Spo2.Value >= 95 ? "Normal (Excelente)" : "Atenção (Baixo)";
                    }
                    else
                    {
                        Spo2 = "--";
                        Spo2Subtitle = "Sem medição recente";
                    }

                    if (latestLog.Stress.HasValue)
                    {
                        Stress = $"{latestLog.Stress.Value} / 100";
                        StressSubtitle = latestLog.Stress.Value <= 29 ? "Relaxado" :
                                         latestLog.Stress.Value <= 59 ? "Normal" :
                                         latestLog.Stress.Value <= 79 ? "Médio" : "Alto";
                    }
                    else
                    {
                        Stress = "-- / 100";
                        StressSubtitle = "Sem medição recente";
                    }

                    int paiTot = latestLog.PaiTotal ?? 0;
                    int paiTod = latestLog.PaiToday ?? 0;
                    Pai = $"{paiTot} pts";
                    PaiSubtitle = $"Hoje: +{paiTod} pts (Meta semanal > 100)";
                }
                else
                {
                    ResetMetricsToEmpty("Nenhum registro de atividade sincronizado.");
                }
            }
            else
            {
                DeviceStatus = "Nenhuma pulseira cadastrada. Configure na aba Dispositivos.";
                SyncIntervalDescription = "Sem sincronização ativa";
                ScheduledTimes.Clear();
                ResetMetricsToEmpty("Cadastre sua pulseira para visualizar métricas.");
            }

            // 4. Clima recente
            var weather = await db.WeatherHistories
                .OrderByDescending(w => w.Timestamp)
                .FirstOrDefaultAsync();

            if (weather != null)
            {
                WeatherTemp = $"{weather.Temp}°C";
                WeatherCity = weather.City;
                WeatherCondition = weather.Condition;
                WeatherDetails = $"Umidade: {weather.Humidity ?? 0}% | Vento: {weather.WindSpeed ?? 0} km/h";
            }
            else
            {
                WeatherTemp = "--°C";
                WeatherCity = "Sem dados climáticos";
                WeatherCondition = "Consulte o clima para carregar previsões";
                WeatherDetails = "--";
            }

            StatusMessage = "Dados atualizados com sucesso.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro ao carregar dados: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ResetMetricsToEmpty(string emptyReason)
    {
        StepsToday = "0";
        StepGoalPercent = 0;
        StepDetails = emptyReason;

        HeartRate = "-- BPM";
        HeartRateSubtitle = "Nenhuma leitura registrada";

        Calories = "0 kcal";
        CaloriesGoalPercent = 0;
        CaloriesSubtitle = "Meta: 300 kcal (0%)";

        BatteryLevel = "N/A";
        BatteryPercent = 0;
        BatterySubtitle = "Sem dados";

        Spo2 = "--";
        Spo2Subtitle = "Sem medição";

        Stress = "-- / 100";
        StressSubtitle = "Sem medição";

        Pai = "0 pts";
        PaiSubtitle = "Hoje: 0 pts";
    }

    [RelayCommand]
    public async Task SyncNowAsync()
    {
        IsLoading = true;
        StatusMessage = "Buscando pulseira ativa para sincronizar...";

        try
        {
            using var db = new AppDbContext();
            var device = await db.DeviceConfigs.FirstOrDefaultAsync(d => d.IsActive);
            if (device == null)
            {
                StatusMessage = "Nenhuma pulseira ativa cadastrada. Vá até a aba Dispositivos.";
                return;
            }

            StatusMessage = $"Consultando pulseira {device.DeviceName} ({device.MacAddress})...";
            var (ok, batt, msg) = await BleManagerService.Instance.ReadBatteryLevelAsync(device.MacAddress);

            if (ok && batt.HasValue)
            {
                StatusMessage = $"Pulseira conectada! Bateria lida: {batt.Value}%. Atualizando banco...";
                var log = new ActivityLog
                {
                    DeviceMac = device.MacAddress,
                    Timestamp = DateTime.UtcNow,
                    BatteryLevel = batt.Value,
                    Steps = 0,
                    Calories = 0,
                    DistanceMeters = 0
                };
                db.ActivityLogs.Add(log);

                device.LastSyncTime = DateTime.UtcNow;
                device.UpdatedAt = DateTime.UtcNow;

                db.SyncHistories.Add(new SyncHistory
                {
                    DeviceMac = device.MacAddress,
                    Status = "SUCCESS",
                    Message = $"Sincronização manual via BLE: bateria {batt.Value}%",
                    Timestamp = DateTime.UtcNow
                });

                await db.SaveChangesAsync();
                await RefreshAsync();
                StatusMessage = $"Sincronização concluída com sucesso! Bateria: {batt.Value}%.";
            }
            else
            {
                StatusMessage = $"Não foi possível conectar à pulseira: {msg}";
                db.SyncHistories.Add(new SyncHistory
                {
                    DeviceMac = device.MacAddress,
                    Status = "ERROR",
                    Message = $"Falha na sincronização manual: {msg}",
                    Timestamp = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro na sincronização: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
