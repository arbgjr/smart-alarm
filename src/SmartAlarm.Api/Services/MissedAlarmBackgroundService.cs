using SmartAlarm.Application.Abstractions;

namespace SmartAlarm.Api.Services;

public class MissedAlarmBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MissedAlarmBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5); // Check every 5 minutes

    public MissedAlarmBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<MissedAlarmBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Missed Alarm Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var alarmTriggerService = scope.ServiceProvider.GetRequiredService<IAlarmTriggerService>();

                await alarmTriggerService.ProcessMissedAlarmsAsync(stoppingToken);

                _logger.LogDebug("Processed missed alarms check");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing missed alarms");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Missed Alarm Background Service stopped");
    }
}
