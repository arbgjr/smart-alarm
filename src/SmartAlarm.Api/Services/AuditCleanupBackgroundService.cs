using SmartAlarm.Application.Abstractions;
using SmartAlarm.Observability.Tracing;
using System.Diagnostics;

namespace SmartAlarm.Api.Services;

/// <summary>
/// Background service for cleaning up old audit logs
/// </summary>
public class AuditCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuditCleanupBackgroundService> _logger;
    private readonly SmartAlarmActivitySource _activitySource;
    private readonly IConfiguration _configuration;

    // Default to run cleanup daily at 2 AM
    private readonly TimeSpan _interval;
    private readonly int _retentionDays;

    public AuditCleanupBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<AuditCleanupBackgroundService> logger,
        SmartAlarmActivitySource activitySource,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _activitySource = activitySource;
        _configuration = configuration;

        // Configure cleanup interval (default: daily)
        var intervalHours = _configuration.GetValue("AuditCleanup:IntervalHours", 24);
        _interval = TimeSpan.FromHours(intervalHours);

        // Configure retention period (default: 365 days)
        _retentionDays = _configuration.GetValue("AuditCleanup:RetentionDays", 365);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Audit Cleanup Background Service started with {RetentionDays} days retention",
            _retentionDays);

        // Wait until the configured time (2 AM by default) for first execution
        await WaitUntilScheduledTime(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            using var activity = _activitySource.StartActivity("background.audit_cleanup");
            activity?.SetTag("background.service", "AuditCleanupBackgroundService");
            activity?.SetTag("background.retention_days", _retentionDays);
            activity?.SetTag("background.interval_hours", _interval.TotalHours);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

                var stopwatch = Stopwatch.StartNew();
                await auditService.CleanupOldLogsAsync(_retentionDays);
                stopwatch.Stop();

                activity?.SetTag("background.execution_time_ms", stopwatch.ElapsedMilliseconds);
                SmartAlarmActivitySource.SetSuccess(activity, "Audit logs cleaned up successfully");

                _logger.LogInformation("Audit log cleanup completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                SmartAlarmActivitySource.SetError(activity, ex);
                _logger.LogError(ex, "Error occurred during audit log cleanup");
            }

            // Wait for the next scheduled execution
            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Audit Cleanup Background Service stopped");
    }

    private async Task WaitUntilScheduledTime(CancellationToken stoppingToken)
    {
        var now = DateTime.Now;
        var scheduledHour = _configuration.GetValue("AuditCleanup:ScheduledHour", 2); // 2 AM default

        var nextRun = now.Date.AddHours(scheduledHour);
        if (nextRun <= now)
        {
            nextRun = nextRun.AddDays(1);
        }

        var delay = nextRun - now;

        _logger.LogInformation("Next audit cleanup scheduled for {NextRun} (in {Delay})",
            nextRun, delay);

        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay, stoppingToken);
        }
    }
}
