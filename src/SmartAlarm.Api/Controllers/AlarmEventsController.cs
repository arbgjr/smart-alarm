using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.Annotations;
using SmartAlarm.Application.Services;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Api.Services;

namespace SmartAlarm.Api.Controllers;

/// <summary>
/// Controller para gerenciamento de eventos de alarme e análise de padrões comportamentais
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[SwaggerTag("Eventos de alarme e análise comportamental")]
public class AlarmEventsController : ControllerBase
{
    private readonly IAlarmEventService _alarmEventService;
    private readonly IPatternDetectionService _patternDetectionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AlarmEventsController> _logger;

    public AlarmEventsController(
        IAlarmEventService alarmEventService,
        IPatternDetectionService patternDetectionService,
        ICurrentUserService currentUserService,
        ILogger<AlarmEventsController> logger)
    {
        _alarmEventService = alarmEventService ?? throw new ArgumentNullException(nameof(alarmEventService));
        _patternDetectionService = patternDetectionService ?? throw new ArgumentNullException(nameof(patternDetectionService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registra evento de alarme disparado
    /// </summary>
    [HttpPost("triggered")]
    [SwaggerOperation("Registra evento de alarme disparado")]
    [SwaggerResponse(200, "Evento registrado com sucesso")]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<IActionResult> RecordTriggered([FromBody] RecordTriggeredRequest request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(_currentUserService.UserId ?? throw new InvalidOperationException("User ID not available"));
        
        await _alarmEventService.RecordAlarmTriggeredAsync(
            request.AlarmId, 
            userId, 
            request.Location, 
            cancellationToken);

        _logger.LogInformation("Evento de alarme disparado registrado: AlarmId={AlarmId}, UserId={UserId}", 
            request.AlarmId, userId);

        return Ok();
    }

    /// <summary>
    /// Registra evento de soneca
    /// </summary>
    [HttpPost("snoozed")]
    [SwaggerOperation("Registra evento de soneca")]
    [SwaggerResponse(200, "Evento registrado com sucesso")]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<IActionResult> RecordSnoozed([FromBody] RecordSnoozedRequest request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(_currentUserService.UserId ?? throw new InvalidOperationException("User ID not available"));
        
        await _alarmEventService.RecordAlarmSnoozedAsync(
            request.AlarmId, 
            userId, 
            request.SnoozeMinutes, 
            cancellationToken);

        _logger.LogInformation("Evento de soneca registrado: AlarmId={AlarmId}, UserId={UserId}, Minutes={Minutes}", 
            request.AlarmId, userId, request.SnoozeMinutes);

        return Ok();
    }

    /// <summary>
    /// Registra evento de alarme desativado
    /// </summary>
    [HttpPost("disabled")]
    [SwaggerOperation("Registra evento de alarme desativado")]
    [SwaggerResponse(200, "Evento registrado com sucesso")]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<IActionResult> RecordDisabled([FromBody] RecordDisabledRequest request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(_currentUserService.UserId ?? throw new InvalidOperationException("User ID not available"));
        
        await _alarmEventService.RecordAlarmDisabledAsync(
            request.AlarmId, 
            userId, 
            request.Reason, 
            cancellationToken);

        _logger.LogInformation("Evento de alarme desativado registrado: AlarmId={AlarmId}, UserId={UserId}, Reason={Reason}", 
            request.AlarmId, userId, request.Reason);

        return Ok();
    }

    /// <summary>
    /// Registra evento de alarme ignorado/descartado
    /// </summary>
    [HttpPost("dismissed")]
    [SwaggerOperation("Registra evento de alarme descartado")]
    [SwaggerResponse(200, "Evento registrado com sucesso")]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<IActionResult> RecordDismissed([FromBody] RecordDismissedRequest request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(_currentUserService.UserId ?? throw new InvalidOperationException("User ID not available"));
        
        await _alarmEventService.RecordAlarmDismissedAsync(
            request.AlarmId, 
            userId, 
            cancellationToken);

        _logger.LogInformation("Evento de alarme descartado registrado: AlarmId={AlarmId}, UserId={UserId}", 
            request.AlarmId, userId);

        return Ok();
    }

    /// <summary>
    /// Obtém histórico de eventos do usuário
    /// </summary>
    [HttpGet("history")]
    [SwaggerOperation("Obtém histórico de eventos de alarme do usuário")]
    [SwaggerResponse(200, "Histórico obtido com sucesso", typeof(List<AlarmEventResponse>))]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult<List<AlarmEventResponse>>> GetHistory(
        [FromQuery] int days = 30, 
        CancellationToken cancellationToken = default)
    {
        var userId = Guid.Parse(_currentUserService.UserId ?? throw new InvalidOperationException("User ID not available"));
        
        var events = await _alarmEventService.GetUserEventHistoryAsync(userId, days, cancellationToken);
        
        var response = events.Select(e => new AlarmEventResponse
        {
            Id = e.Id,
            AlarmId = e.AlarmId,
            EventType = e.EventType.ToString(),
            Timestamp = e.Timestamp,
            Metadata = e.Metadata,
            Location = e.Location
        }).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Obtém estatísticas de eventos do usuário
    /// </summary>
    [HttpGet("stats")]
    [SwaggerOperation("Obtém estatísticas de eventos de alarme do usuário")]
    [SwaggerResponse(200, "Estatísticas obtidas com sucesso", typeof(EventStatsResponse))]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult<EventStatsResponse>> GetStats(
        [FromQuery] int days = 30, 
        CancellationToken cancellationToken = default)
    {
        var userId = Guid.Parse(_currentUserService.UserId ?? throw new InvalidOperationException("User ID not available"));
        
        var stats = await _alarmEventService.GetUserEventStatsAsync(userId, days, cancellationToken);
        
        var response = new EventStatsResponse
        {
            AnalysisPeriodDays = days,
            EventsByType = stats.ToDictionary(
                kvp => kvp.Key.ToString(), 
                kvp => kvp.Value)
        };

        return Ok(response);
    }

    /// <summary>
    /// Obtém padrão comportamental do usuário
    /// </summary>
    [HttpGet("behavior-pattern")]
    [SwaggerOperation("Obtém análise de padrão comportamental do usuário")]
    [SwaggerResponse(200, "Padrão comportamental obtido com sucesso", typeof(UserBehaviorPatternResponse))]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult<UserBehaviorPatternResponse>> GetBehaviorPattern(
        [FromQuery] int days = 30, 
        CancellationToken cancellationToken = default)
    {
        var userId = Guid.Parse(_currentUserService.UserId ?? throw new InvalidOperationException("User ID not available"));
        
        var pattern = await _alarmEventService.GetUserBehaviorPatternAsync(userId, days, cancellationToken);
        
        var response = new UserBehaviorPatternResponse
        {
            UserId = pattern.UserId,
            AnalysisPeriodStart = pattern.AnalysisPeriodStart,
            AnalysisPeriodEnd = pattern.AnalysisPeriodEnd,
            TotalEvents = pattern.TotalEvents,
            ConsistencyScore = pattern.ConsistencyScore,
            AverageSnoozeMinutes = pattern.AverageSnoozeMinutes,
            DismissalRate = pattern.DismissalRate,
            EventsByDayOfWeek = pattern.EventsByDayOfWeek.ToDictionary(
                kvp => kvp.Key.ToString(), 
                kvp => kvp.Value),
            EventsByType = pattern.EventsByType.ToDictionary(
                kvp => kvp.Key.ToString(), 
                kvp => kvp.Value),
            EventsByHour = pattern.EventsByHour,
            MostActiveDays = pattern.MostActiveDays.Select(d => d.ToString()).ToList(),
            MostActiveHours = pattern.MostActiveHours,
            Insights = pattern.Insights.Select(i => new BehaviorInsightResponse
            {
                Type = i.Type,
                Description = i.Description,
                Confidence = i.Confidence,
                CreatedAt = i.CreatedAt
            }).ToList()
        };

        return Ok(response);
    }
}

// Request/Response DTOs
public record RecordTriggeredRequest(Guid AlarmId, string? Location = null);
public record RecordSnoozedRequest(Guid AlarmId, int SnoozeMinutes);
public record RecordDisabledRequest(Guid AlarmId, string? Reason = null);
public record RecordDismissedRequest(Guid AlarmId);

public class AlarmEventResponse
{
    public Guid Id { get; set; }
    public Guid AlarmId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? Metadata { get; set; }
    public string? Location { get; set; }
}

public class EventStatsResponse
{
    public int AnalysisPeriodDays { get; set; }
    public Dictionary<string, int> EventsByType { get; set; } = new();
}

public class UserBehaviorPatternResponse
{
    public Guid UserId { get; set; }
    public DateTime AnalysisPeriodStart { get; set; }
    public DateTime AnalysisPeriodEnd { get; set; }
    public int TotalEvents { get; set; }
    public double ConsistencyScore { get; set; }
    public int AverageSnoozeMinutes { get; set; }
    public double DismissalRate { get; set; }
    public Dictionary<string, int> EventsByDayOfWeek { get; set; } = new();
    public Dictionary<string, int> EventsByType { get; set; } = new();
    public Dictionary<int, int> EventsByHour { get; set; } = new();
    public List<string> MostActiveDays { get; set; } = new();
    public List<int> MostActiveHours { get; set; } = new();
    public List<BehaviorInsightResponse> Insights { get; set; } = new();
}

public class BehaviorInsightResponse
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public DateTime CreatedAt { get; set; }
}