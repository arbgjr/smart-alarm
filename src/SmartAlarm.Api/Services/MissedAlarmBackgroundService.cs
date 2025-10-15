using SmartAlarm.Application.Abstractions;
using SmartAlarm.Observability.Tracing;
using System.Diagnostics;

namespace SmartAlarm.Api.Services;

public class MissedAlarmBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MissedAlarmBackgroundService> _logger;
    private readonly SmartAlarmActivitySource _activitySource;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5); // Check every 5 minutes

    public MissedAlarmBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<MissedAlarmBackgroundService> logger,
        SmartAlarmActivitySource activitySource)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _activitySource = activitySource;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Missed Alarm Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            using var activity = _activitySource.StartActivity("background.process_missed_alarms");
            activity?.SetTag("background.service", "MissedAlarmBackgroundService");
            activity?.SetTag("background.interval_minutes", _interval.TotalMinutes);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var alarmTriggerService = scope.ServiceProvider.GetRequiredService<IAlarmTriggerService>();

                var stopwatch = Stopwatch.StartNew();
                await alarmTriggerService.ProcessMissedAlarmsAsync(stoppingToken);
                stopwatch.Stop();

                activity?.SetTag("background.execution_time_ms", stopwatch.ElapsedMilliseconds);
                SmartAlarmActivitySource.SetSuccess(activity, "Missed alarms processed successfully");

                _logger.LogDebug("Processed missed alarms check in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                SmartAlarmActivitySource.SetError(activity, ex);
                _logger.LogError(ex, "Error occurred while processing missed alarms");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Missed Alarm Background Service stopped");
    }
}
