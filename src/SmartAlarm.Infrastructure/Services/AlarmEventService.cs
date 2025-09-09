using System.Diagnostics;
using Microsoft.Extensions.Logging;
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

    public AlarmEventService(
        IAlarmEventRepository repository,
        ILogger<AlarmEventService> logger,
        SmartAlarmMeter meter,
        ICorrelationContext correlationContext,
        SmartAlarmActivitySource activitySource)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _meter = meter ?? throw new ArgumentNullException(nameof(meter));
        _correlationContext = correlationContext ?? throw new ArgumentNullException(nameof(correlationContext));
        _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
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

        _logger.LogInformation(LogTemplates.ServiceOperationStarted,
            "RecordAlarmEvent",
            new { AlarmId = alarmId, UserId = userId, EventType = eventType });

        try
        {
            var alarmEvent = eventType switch
            {
                AlarmEventType.Created => AlarmEvent.AlarmCreated(alarmId, userId, metadata),
                AlarmEventType.Triggered => AlarmEvent.AlarmTriggered(alarmId, userId, location),
                AlarmEventType.Snoozed => AlarmEvent.AlarmSnoozed(alarmId, userId, snoozeMinutes ?? 5),
                AlarmEventType.Disabled => AlarmEvent.AlarmDisabled(alarmId, userId, metadata),
                AlarmEventType.Dismissed => AlarmEvent.AlarmDismissed(alarmId, userId),
                AlarmEventType.Modified => AlarmEvent.AlarmModified(alarmId, userId, metadata),
                _ => throw new ArgumentException($"Unsupported event type: {eventType}")
            };

            await _repository.AddAsync(alarmEvent, cancellationToken);
            stopwatch.Stop();

            _meter.RecordServiceOperationDuration(stopwatch.ElapsedMilliseconds, "RecordEvent", "AlarmEvent");

            _logger.LogInformation(LogTemplates.ServiceOperationCompleted,
                "RecordAlarmEvent",
                stopwatch.ElapsedMilliseconds,
                "event recorded successfully");

            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("SERVICE", "AlarmEvent", "RecordError");

            _logger.LogError(LogTemplates.ServiceOperationFailed,
                "RecordAlarmEvent",
                "AlarmEvent",
                stopwatch.ElapsedMilliseconds,
                ex.Message);

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

        _logger.LogInformation(LogTemplates.ServiceOperationStarted,
            "GetUserEventHistory",
            new { UserId = userId, Days = days });

        try
        {
            var events = await _repository.GetByUserIdAsync(userId, days, cancellationToken);
            stopwatch.Stop();

            _meter.RecordServiceOperationDuration(stopwatch.ElapsedMilliseconds, "GetHistory", "AlarmEvent");

            _logger.LogInformation(LogTemplates.ServiceOperationCompleted,
                "GetUserEventHistory",
                stopwatch.ElapsedMilliseconds,
                events.Count());

            activity?.SetStatus(ActivityStatusCode.Ok);
            return events.ToList();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("SERVICE", "AlarmEvent", "QueryError");

            _logger.LogError(LogTemplates.ServiceOperationFailed,
                "GetUserEventHistory",
                "AlarmEvent",
                stopwatch.ElapsedMilliseconds,
                ex.Message);

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

        _logger.LogInformation(LogTemplates.ServiceOperationStarted,
            "GetUserEventStats",
            new { UserId = userId, Days = days });

        try
        {
            var stats = await _repository.GetEventStatsByUserAsync(userId, days, cancellationToken);
            stopwatch.Stop();

            _meter.RecordServiceOperationDuration(stopwatch.ElapsedMilliseconds, "GetStats", "AlarmEvent");

            _logger.LogInformation(LogTemplates.ServiceOperationCompleted,
                "GetUserEventStats",
                stopwatch.ElapsedMilliseconds,
                stats.Count);

            activity?.SetStatus(ActivityStatusCode.Ok);
            return stats;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("SERVICE", "AlarmEvent", "QueryError");

            _logger.LogError(LogTemplates.ServiceOperationFailed,
                "GetUserEventStats",
                "AlarmEvent",
                stopwatch.ElapsedMilliseconds,
                ex.Message);

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

        _logger.LogInformation(LogTemplates.ServiceOperationStarted,
            "GetUserBehaviorPattern",
            new { UserId = userId, Days = days });

        try
        {
            var events = await _repository.GetByUserIdAsync(userId, days, cancellationToken);
            var pattern = AnalyzeBehaviorPattern(userId, events, days);
            stopwatch.Stop();

            _meter.RecordServiceOperationDuration(stopwatch.ElapsedMilliseconds, "GetPattern", "AlarmEvent");

            _logger.LogInformation(LogTemplates.ServiceOperationCompleted,
                "GetUserBehaviorPattern",
                stopwatch.ElapsedMilliseconds,
                pattern.TotalEvents);

            activity?.SetStatus(ActivityStatusCode.Ok);
            return pattern;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementErrorCount("SERVICE", "AlarmEvent", "AnalysisError");

            _logger.LogError(LogTemplates.ServiceOperationFailed,
                "GetUserBehaviorPattern",
                "AlarmEvent",
                stopwatch.ElapsedMilliseconds,
                ex.Message);

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
}