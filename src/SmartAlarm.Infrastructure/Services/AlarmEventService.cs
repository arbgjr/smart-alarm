using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Abstractions;
using SmartAlarm.Application.DTOs.Notifications;
using SmartAlarm.Application.Services;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Logging;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;

namespace SmartAlarm.Infrastructure.Services;

/// <summary>
/// Implementação do serviço de eventos de alarme com observabilidade completa.
/// Seguindo padrões estabelecidos no projeto para serviços de infraestrutura.
/// </summary>
public class AlarmEventService : IAlarmEventService
{
    private readonly IAlarmEventRepository _repository;
    private readonly ILogger<AlarmEventService> _logger;
    private readonly SmartAlarmMeter _meter;
    private readonly ICorrelationContext _correlationContext;
    private readonly SmartAlarmActivitySource _activitySource;
    private readonly SmartAlarm.Application.Abstractions.INotificationService _notificationService;
    private readonly IPushNotificationService _pushNotificationService;

    public AlarmEventService(
        IAlarmEventRepository repository,
        ILogger<AlarmEventService> logger,
        SmartAlarmMeter meter,
        ICorrelationContext correlationContext,
        SmartAlarmActivitySource activitySource,
        SmartAlarm.Application.Abstractions.INotificationService notificationService,
        IPushNotificationService pushNotificationService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _meter = meter ?? throw new ArgumentNullException(nameof(meter));
        _correlationContext = correlationContext ?? throw new ArgumentNullException(nameof(correlationContext));
        _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _pushNotificationService = pushNotificationService ?? throw new ArgumentNullException(nameof(pushNotificationService));
    }

    public async Task RecordEventAsync(
        Guid alarmId,
        Guid userId,
        AlarmEventType eventType,
        int? snoozeMinutes = null,
        string? metadata = null,
        string? location = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("RecordAlarmEvent");
        activity?.SetTag("alarm.id", alarmId.ToString());
        activity?.SetTag("user.id", userId.ToString());
        activity?.SetTag("event.type", eventType.ToString());

        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Recording alarm event: AlarmId={AlarmId}, UserId={UserId}, EventType={EventType}",
            alarmId, userId, eventType);

        try
        {
            var timestamp = DateTime.UtcNow;
            var alarmEvent = eventType switch
            {
                AlarmEventType.Created => AlarmEvent.AlarmCreated(alarmId, userId, timestamp, metadata),
                AlarmEventType.Triggered => AlarmEvent.AlarmTriggered(alarmId, userId, timestamp, location),
                AlarmEventType.Snoozed => AlarmEvent.AlarmSnoozed(alarmId, userId, timestamp, snoozeMinutes ?? 5),
                AlarmEventType.Disabled => AlarmEvent.AlarmDisabled(alarmId, userId, timestamp, metadata),
                AlarmEventType.Dismissed => AlarmEvent.AlarmDismissed(alarmId, userId, timestamp),
                AlarmEventType.Modified => AlarmEvent.AlarmModified(alarmId, userId, timestamp, metadata),
                _ => throw new ArgumentException($"Unsupported event type: {eventType}")
            };

            await _repository.AddAsync(alarmEvent, cancellationToken);

            // Send notifications for relevant events
            await SendEventNotificationAsync(alarmEvent, cancellationToken);

            stopwatch.Stop();

            _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "RecordEvent", "AlarmEvent");

            _logger.LogInformation("Alarm event recorded successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("SERVICE", "AlarmEvent", "RecordError");

            _logger.LogError(ex, "Failed to record alarm event in {ElapsedMs}ms: {Error}",
                stopwatch.ElapsedMilliseconds, ex.Message);

            throw;
        }
    }

    public async Task RecordAlarmCreatedAsync(
        Guid alarmId,
        Guid userId,
        string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        await RecordEventAsync(alarmId, userId, AlarmEventType.Created, metadata: metadata, cancellationToken: cancellationToken);
    }

    public async Task RecordAlarmTriggeredAsync(
        Guid alarmId,
        Guid userId,
        string? location = null,
        CancellationToken cancellationToken = default)
    {
        await RecordEventAsync(alarmId, userId, AlarmEventType.Triggered, location: location, cancellationToken: cancellationToken);
    }

    public async Task RecordAlarmSnoozedAsync(
        Guid alarmId,
        Guid userId,
        int snoozeMinutes,
        CancellationToken cancellationToken = default)
    {
        await RecordEventAsync(alarmId, userId, AlarmEventType.Snoozed, snoozeMinutes: snoozeMinutes, cancellationToken: cancellationToken);
    }

    public async Task RecordAlarmDisabledAsync(
        Guid alarmId,
        Guid userId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        await RecordEventAsync(alarmId, userId, AlarmEventType.Disabled, metadata: reason, cancellationToken: cancellationToken);
    }

    public async Task RecordAlarmDismissedAsync(
        Guid alarmId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await RecordEventAsync(alarmId, userId, AlarmEventType.Dismissed, cancellationToken: cancellationToken);
    }

    public async Task RecordAlarmModifiedAsync(
        Guid alarmId,
        Guid userId,
        string? changes = null,
        CancellationToken cancellationToken = default)
    {
        await RecordEventAsync(alarmId, userId, AlarmEventType.Modified, metadata: changes, cancellationToken: cancellationToken);
    }

    public async Task<List<AlarmEvent>> GetUserEventHistoryAsync(
        Guid userId,
        int days = 30,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("GetUserEventHistory");
        activity?.SetTag("user.id", userId.ToString());
        activity?.SetTag("days", days.ToString());

        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Starting GetUserEventHistory for user {UserId}, days {Days}", userId, days);

        try
        {
            var startDate = DateTime.UtcNow.AddDays(-days);
            var endDate = DateTime.UtcNow;
            var events = await _repository.GetUserEventsAsync(userId, startDate, endDate, cancellationToken);
            stopwatch.Stop();

            _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetHistory", "AlarmEvent");

            _logger.LogInformation("Completed GetUserEventHistory in {ElapsedMs}ms, found {Count} events",
                stopwatch.ElapsedMilliseconds, events.Count());

            activity?.SetStatus(ActivityStatusCode.Ok);
            return events.ToList();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("SERVICE", "AlarmEvent", "QueryError");

            _logger.LogError(ex, "Failed GetUserEventHistory for AlarmEvent in {ElapsedMs}ms: {Error}",
                stopwatch.ElapsedMilliseconds, ex.Message);

            throw;
        }
    }

    public async Task<Dictionary<AlarmEventType, int>> GetUserEventStatsAsync(
        Guid userId,
        int days = 30,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("GetUserEventStats");
        activity?.SetTag("user.id", userId.ToString());
        activity?.SetTag("days", days.ToString());

        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Starting GetUserEventStats for user {UserId}, days {Days}", userId, days);

        try
        {
            // Get events and calculate stats manually since GetEventStatsByUserAsync doesn't exist
            var startDate = DateTime.UtcNow.AddDays(-days);
            var endDate = DateTime.UtcNow;
            var events = await _repository.GetUserEventsAsync(userId, startDate, endDate, cancellationToken);
            var stats = events.GroupBy(e => e.EventType)
                            .ToDictionary(g => g.Key, g => g.Count());
            stopwatch.Stop();

            _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetStats", "AlarmEvent");

            _logger.LogInformation("Completed GetUserEventStats in {ElapsedMs}ms, found {Count} event types",
                stopwatch.ElapsedMilliseconds, stats.Count);

            activity?.SetStatus(ActivityStatusCode.Ok);
            return stats;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("SERVICE", "AlarmEvent", "QueryError");

            _logger.LogError(ex, "Failed GetUserEventStats for AlarmEvent in {ElapsedMs}ms: {Error}",
                stopwatch.ElapsedMilliseconds, ex.Message);

            throw;
        }
    }

    public async Task<UserBehaviorPattern> GetUserBehaviorPatternAsync(
        Guid userId,
        int days = 30,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("GetUserBehaviorPattern");
        activity?.SetTag("user.id", userId.ToString());
        activity?.SetTag("days", days.ToString());

        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Starting GetUserBehaviorPattern for user {UserId}, days {Days}", userId, days);

        try
        {
            var startDate = DateTime.UtcNow.AddDays(-days);
            var endDate = DateTime.UtcNow;
            var events = await _repository.GetUserEventsAsync(userId, startDate, endDate, cancellationToken);
            var pattern = AnalyzeBehaviorPattern(userId, events, days);
            stopwatch.Stop();

            _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetPattern", "AlarmEvent");

            _logger.LogInformation("Completed GetUserBehaviorPattern in {ElapsedMs}ms, found {TotalEvents} events",
                stopwatch.ElapsedMilliseconds, pattern.TotalEvents);

            activity?.SetStatus(ActivityStatusCode.Ok);
            return pattern;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("SERVICE", "AlarmEvent", "AnalysisError");

            _logger.LogError(ex, "Failed GetUserBehaviorPattern for AlarmEvent in {ElapsedMs}ms: {Error}",
                stopwatch.ElapsedMilliseconds, ex.Message);

            throw;
        }
    }

    private UserBehaviorPattern AnalyzeBehaviorPattern(Guid userId, IEnumerable<AlarmEvent> events, int days)
    {
        var eventList = events.ToList();
        var startDate = DateTime.UtcNow.AddDays(-days);
        var endDate = DateTime.UtcNow;

        var pattern = new UserBehaviorPattern
        {
            UserId = userId,
            AnalysisPeriodStart = startDate,
            AnalysisPeriodEnd = endDate,
            TotalEvents = eventList.Count
        };

        if (eventList.Count == 0)
        {
            return pattern;
        }

        // Análise por dia da semana
        pattern.EventsByDayOfWeek = eventList
            .GroupBy(e => e.Timestamp.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.Count());

        // Análise por tipo de evento
        pattern.EventsByType = eventList
            .GroupBy(e => e.EventType)
            .ToDictionary(g => g.Key, g => g.Count());

        // Análise por hora do dia
        pattern.EventsByHour = eventList
            .GroupBy(e => e.Timestamp.Hour)
            .ToDictionary(g => g.Key, g => g.Count());

        // Dias mais ativos
        pattern.MostActiveDays = pattern.EventsByDayOfWeek
            .OrderByDescending(kvp => kvp.Value)
            .Take(3)
            .Select(kvp => kvp.Key)
            .ToList();

        // Horas mais ativas
        pattern.MostActiveHours = pattern.EventsByHour
            .OrderByDescending(kvp => kvp.Value)
            .Take(3)
            .Select(kvp => kvp.Key)
            .ToList();

        // Tempo médio de soneca
        var snoozeEvents = eventList.Where(e => e.EventType == AlarmEventType.Snoozed).ToList();
        if (snoozeEvents.Any())
        {
            pattern.AverageSnoozeMinutes = (int)snoozeEvents
                .Where(e => !string.IsNullOrEmpty(e.Metadata))
                .Select(e => int.TryParse(e.Metadata, out var minutes) ? minutes : 5)
                .Average();
        }

        // Taxa de descarte
        var triggeredCount = eventList.Count(e => e.EventType == AlarmEventType.Triggered);
        var dismissedCount = eventList.Count(e => e.EventType == AlarmEventType.Dismissed);
        if (triggeredCount > 0)
        {
            pattern.DismissalRate = (double)dismissedCount / triggeredCount * 100;
        }

        // Score de consistência (baseado na variação de horários)
        pattern.ConsistencyScore = CalculateConsistencyScore(eventList);

        // Insights comportamentais
        pattern.Insights = GenerateBehaviorInsights(eventList, pattern);

        return pattern;
    }

    private double CalculateConsistencyScore(List<AlarmEvent> events)
    {
        var triggeredEvents = events
            .Where(e => e.EventType == AlarmEventType.Triggered)
            .ToList();

        if (triggeredEvents.Count < 3)
        {
            return 0.5; // Score neutro para poucos dados
        }

        var hourlyVariations = triggeredEvents
            .GroupBy(e => e.Timestamp.DayOfWeek)
            .Select(g => g.Select(e => e.Timestamp.Hour).ToList())
            .Where(hours => hours.Count > 1)
            .Select(hours =>
            {
                var avg = hours.Average();
                return hours.Sum(h => Math.Pow(h - avg, 2)) / hours.Count;
            })
            .ToList();

        if (hourlyVariations.Count == 0)
        {
            return 1.0; // Perfeita consistência se todos os eventos são no mesmo horário
        }

        var avgVariation = hourlyVariations.Average();
        return Math.Max(0, 1.0 - (avgVariation / 12.0)); // Normaliza para 0-1
    }

    private List<BehaviorInsight> GenerateBehaviorInsights(List<AlarmEvent> events, UserBehaviorPattern pattern)
    {
        var insights = new List<BehaviorInsight>();

        // Insight sobre consistência
        if (pattern.ConsistencyScore > 0.8)
        {
            insights.Add(new BehaviorInsight
            {
                Type = "Consistency",
                Description = "Usuário tem rotina muito consistente de alarmes",
                Confidence = pattern.ConsistencyScore,
                Data = new Dictionary<string, object> { { "score", pattern.ConsistencyScore } },
                CreatedAt = DateTime.UtcNow
            });
        }
        else if (pattern.ConsistencyScore < 0.3)
        {
            insights.Add(new BehaviorInsight
            {
                Type = "Inconsistency",
                Description = "Usuário tem rotina irregular de alarmes",
                Confidence = 1.0 - pattern.ConsistencyScore,
                Data = new Dictionary<string, object> { { "score", pattern.ConsistencyScore } },
                CreatedAt = DateTime.UtcNow
            });
        }

        // Insight sobre soneca excessiva
        if (pattern.AverageSnoozeMinutes > 15)
        {
            insights.Add(new BehaviorInsight
            {
                Type = "ExcessiveSnooze",
                Description = $"Usuário faz soneca por muito tempo em média ({pattern.AverageSnoozeMinutes} min)",
                Confidence = Math.Min(1.0, pattern.AverageSnoozeMinutes / 30.0),
                Data = new Dictionary<string, object> { { "averageMinutes", pattern.AverageSnoozeMinutes } },
                CreatedAt = DateTime.UtcNow
            });
        }

        // Insight sobre taxa de descarte alta
        if (pattern.DismissalRate > 30)
        {
            insights.Add(new BehaviorInsight
            {
                Type = "HighDismissalRate",
                Description = $"Usuário ignora muitos alarmes ({pattern.DismissalRate:F1}%)",
                Confidence = Math.Min(1.0, pattern.DismissalRate / 100.0),
                Data = new Dictionary<string, object> { { "dismissalRate", pattern.DismissalRate } },
                CreatedAt = DateTime.UtcNow
            });
        }

        return insights;
    }

    private async Task SendEventNotificationAsync(AlarmEvent alarmEvent, CancellationToken cancellationToken)
    {
        try
        {
            var notification = CreateNotificationForEvent(alarmEvent);
            if (notification != null)
            {
                // Send real-time notification via SignalR
                await _notificationService.SendNotificationAsync(
                    alarmEvent.UserId.ToString(),
                    notification,
                    cancellationToken);

                // Send push notification for critical events
                if (ShouldSendPushNotification(alarmEvent.EventType))
                {
                    var pushNotification = CreatePushNotificationForEvent(alarmEvent);
                    if (pushNotification != null)
                    {
                        await _pushNotificationService.SendPushNotificationAsync(
                            alarmEvent.UserId.ToString(),
                            pushNotification,
                            cancellationToken);
                    }
                }

                _logger.LogInformation("Notification sent for alarm event {EventType} for user {UserId}",
                    alarmEvent.EventType, alarmEvent.UserId);
            }
        }
        catch (Exception ex)
        {
            // Don't fail the main operation if notification fails
            _logger.LogWarning(ex, "Failed to send notification for alarm event {EventType} for user {UserId}",
                alarmEvent.EventType, alarmEvent.UserId);
        }
    }

    private NotificationDto? CreateNotificationForEvent(AlarmEvent alarmEvent)
    {
        return alarmEvent.EventType switch
        {
            AlarmEventType.Triggered => new NotificationDto
            {
                Title = "Alarm Triggered",
                Message = "Your alarm is now active!",
                Type = NotificationType.AlarmTriggered,
                Priority = 4, // Critical
                Data = new Dictionary<string, object>
                {
                    { "alarmId", alarmEvent.AlarmId.ToString() },
                    { "eventId", alarmEvent.Id.ToString() }
                }
            },
            AlarmEventType.Snoozed => new NotificationDto
            {
                Title = "Alarm Snoozed",
                Message = $"Alarm snoozed for {alarmEvent.Metadata ?? "5"} minutes",
                Type = NotificationType.AlarmSnoozed,
                Priority = 2, // Normal
                Data = new Dictionary<string, object>
                {
                    { "alarmId", alarmEvent.AlarmId.ToString() },
                    { "snoozeMinutes", alarmEvent.Metadata ?? "5" }
                }
            },
            AlarmEventType.Dismissed => new NotificationDto
            {
                Title = "Alarm Dismissed",
                Message = "Alarm has been dismissed",
                Type = NotificationType.AlarmDismissed,
                Priority = 1, // Low
                Data = new Dictionary<string, object>
                {
                    { "alarmId", alarmEvent.AlarmId.ToString() }
                }
            },
            _ => null // Don't send notifications for other event types
        };
    }

    private PushNotificationDto? CreatePushNotificationForEvent(AlarmEvent alarmEvent)
    {
        return alarmEvent.EventType switch
        {
            AlarmEventType.Triggered => new PushNotificationDto
            {
                Title = "Smart Alarm",
                Body = "Your alarm is now active!",
                Sound = "alarm_sound",
                Priority = 3, // High
                Data = new Dictionary<string, string>
                {
                    { "alarmId", alarmEvent.AlarmId.ToString() },
                    { "eventType", "triggered" }
                },
                ClickAction = "/alarms"
            },
            _ => null // Only send push notifications for triggered alarms
        };
    }

    private static bool ShouldSendPushNotification(AlarmEventType eventType)
    {
        return eventType == AlarmEventType.Triggered;
    }

    public async Task RecordAlarmEscalatedAsync(
        Guid alarmId,
        Guid userId,
        int escalationLevel,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("AlarmEventService.RecordAlarmEscalated");
        activity?.SetTag("alarm.id", alarmId.ToString());
        activity?.SetTag("user.id", userId.ToString());
        activity?.SetTag("escalation.level", escalationLevel.ToString());

        try
        {
            await RecordEventAsync(
                alarmId,
                userId,
                AlarmEventType.Escalated,
                metadata: escalationLevel.ToString(),
                cancellationToken: cancellationToken);

            _logger.LogWarning("Alarm {AlarmId} escalated to level {EscalationLevel} for user {UserId}",
                alarmId, escalationLevel, userId);

            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record alarm escalation for alarm {AlarmId}, user {UserId}, level {EscalationLevel}",
                alarmId, userId, escalationLevel);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
