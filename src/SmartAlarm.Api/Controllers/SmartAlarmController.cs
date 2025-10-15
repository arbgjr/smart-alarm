using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.Annotations;
using SmartAlarm.Application.Services;
using SmartAlarm.Application.Services.External;
using SmartAlarm.Api.Services;

namespace SmartAlarm.Api.Controllers;

/// <summary>
/// Controller para funcionalidades inteligentes de alarme
/// </summary>
[ApiController]
[Route("api/smart-alarm")]
[Authorize]
[SwaggerTag("Funcionalidades inteligentes de alarme")]
public class SmartAlarmController : ControllerBase
{
    private readonly ISmartAlarmService _smartAlarmService;
    private readonly IHolidayCacheService _holidayCacheService;
    private readonly IGoogleCalendarService _googleCalendarService;
    private readonly IPatternDetectionService _patternDetectionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<SmartAlarmController> _logger;

    public SmartAlarmController(
        ISmartAlarmService smartAlarmService,
        IHolidayCacheService holidayCacheService,
        IGoogleCalendarService googleCalendarService,
        IPatternDetectionService patternDetectionService,
        ICurrentUserService currentUserService,
        ILogger<SmartAlarmController> logger)
    {
        _smartAlarmService = smartAlarmService ?? throw new ArgumentNullException(nameof(smartAlarmService));
        _holidayCacheService = holidayCacheService ?? throw new ArgumentNullException(nameof(holidayCacheService));
        _googleCalendarService = googleCalendarService ?? throw new ArgumentNullException(nameof(googleCalendarService));
        _patternDetectionService = patternDetectionService ?? throw new ArgumentNullException(nameof(patternDetectionService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Verifica se um alarme deve ser disparado considerando feriados e calendário
    /// </summary>
    [HttpPost("should-trigger")]
    [SwaggerOperation("Verifica se alarme deve ser disparado com base na lógica inteligente")]
    [SwaggerResponse(200, "Análise concluída", typeof(ShouldTriggerResponse))]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult<ShouldTriggerResponse>> ShouldTrigger(
        [FromBody] ShouldTriggerRequest request, 
        CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(_currentUserService.UserId ?? throw new InvalidOperationException("User ID not available"));
        
        // Para usar ShouldAlarmTriggerAsync, precisamos do objeto Alarm
        // Por enquanto, vamos simular a lógica básica
        var isHoliday = await _smartAlarmService.IsTodayHolidayAsync(userId, cancellationToken);
        var isOnVacation = await _smartAlarmService.IsUserOnVacationAsync(userId, cancellationToken);
        var shouldTrigger = !isHoliday && !isOnVacation;

        var response = new ShouldTriggerResponse
        {
            ShouldTrigger = shouldTrigger,
            AnalyzedAt = DateTime.UtcNow,
            Reasons = new List<string>()
        };

        // Adicionar contexto sobre por que não deve disparar
        if (!shouldTrigger)
        {
            var isHolidayCheck = await _holidayCacheService.IsHolidayAsync(
                request.ScheduledTime, 
                "BR", 
                null,
                cancellationToken);
            
            if (isHolidayCheck)
            {
                response.Reasons.Add("Data é feriado nacional");
            }

            // Verificar se há eventos de férias no Google Calendar
            var hasVacation = await _googleCalendarService.HasVacationOrDayOffAsync(
                userId, 
                request.ScheduledTime.Date, 
                cancellationToken);
                
            if (hasVacation)
            {
                response.Reasons.Add("Período de férias detectado no calendário");
            }
        }

        _logger.LogInformation("Análise de alarme inteligente: AlarmId={AlarmId}, ShouldTrigger={ShouldTrigger}", 
            request.AlarmId, shouldTrigger);

        return Ok(response);
    }

    /// <summary>
    /// Verifica se uma data é feriado
    /// </summary>
    [HttpGet("is-holiday")]
    [SwaggerOperation("Verifica se uma data é feriado")]
    [SwaggerResponse(200, "Verificação concluída", typeof(IsHolidayResponse))]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult<IsHolidayResponse>> IsHoliday(
        [FromQuery] DateTime date,
        [FromQuery] string country = "BR",
        CancellationToken cancellationToken = default)
    {
        var isHoliday = await _holidayCacheService.IsHolidayAsync(
            date, 
            country, 
            null,
            cancellationToken);

        var holidays = await _holidayCacheService.GetHolidaysAsync(
            country, 
            date.Year, 
            null,
            cancellationToken);

        var response = new IsHolidayResponse
        {
            Date = date,
            Country = country,
            IsHoliday = isHoliday,
            Holidays = holidays?.Where(h => h.Date.Date == date.Date).Select(h => new HolidayInfo
            {
                Name = h.Description,
                Date = h.Date,
                IsRecurring = h.Date.Year == 1
            }).ToList() ?? new List<HolidayInfo>()
        };

        return Ok(response);
    }

    /// <summary>
    /// Detecta padrões de rotina do usuário
    /// </summary>
    [HttpGet("routine-patterns")]
    [SwaggerOperation("Detecta padrões de rotina comportamental do usuário")]
    [SwaggerResponse(200, "Padrões detectados", typeof(RoutinePatternsResponse))]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult<RoutinePatternsResponse>> GetRoutinePatterns(
        [FromQuery] int analysisWindowDays = 30,
        CancellationToken cancellationToken = default)
    {
        var userId = Guid.Parse(_currentUserService.UserId ?? throw new InvalidOperationException("User ID not available"));
        
        var patterns = await _patternDetectionService.DetectUserRoutinePatternsAsync(
            userId, 
            analysisWindowDays, 
            cancellationToken);

        var response = new RoutinePatternsResponse
        {
            UserId = userId,
            AnalysisWindowDays = analysisWindowDays,
            DetectedAt = DateTime.UtcNow,
            Patterns = patterns.Select(p => new RoutinePatternInfo
            {
                Type = p.Type.ToString(),
                DayOfWeek = p.DayOfWeek.ToString(),
                AverageTime = p.AverageTime.ToString(@"hh\:mm"),
                TimeVariation = p.TimeVariation.ToString(@"hh\:mm"),
                Frequency = p.Frequency,
                Confidence = p.Confidence,
                Description = p.Description
            }).ToList()
        };

        return Ok(response);
    }

    /// <summary>
    /// Sugere horários ótimos para alarmes
    /// </summary>
    [HttpGet("optimal-times")]
    [SwaggerOperation("Sugere horários ótimos para alarmes baseado em padrões comportamentais")]
    [SwaggerResponse(200, "Sugestões geradas", typeof(OptimalTimesResponse))]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult<OptimalTimesResponse>> GetOptimalTimes(
        [FromQuery] DayOfWeek dayOfWeek,
        CancellationToken cancellationToken = default)
    {
        var userId = Guid.Parse(_currentUserService.UserId ?? throw new InvalidOperationException("User ID not available"));
        
        var optimalSlots = await _patternDetectionService.SuggestOptimalTimeSlotsAsync(
            userId, 
            dayOfWeek, 
            cancellationToken);

        var response = new OptimalTimesResponse
        {
            UserId = userId,
            DayOfWeek = dayOfWeek.ToString(),
            GeneratedAt = DateTime.UtcNow,
            OptimalSlots = optimalSlots.Select(slot => new OptimalTimeSlotInfo
            {
                StartTime = slot.StartTime.ToString(@"hh\:mm"),
                EndTime = slot.EndTime.ToString(@"hh\:mm"),
                OptimalityScore = slot.OptimalityScore,
                Reasoning = slot.Reasoning,
                SupportingEvidence = slot.SupportingEvidence
            }).ToList()
        };

        return Ok(response);
    }
}

// Request/Response DTOs
public record ShouldTriggerRequest(Guid AlarmId, DateTime ScheduledTime);

public class ShouldTriggerResponse
{
    public bool ShouldTrigger { get; set; }
    public DateTime AnalyzedAt { get; set; }
    public List<string> Reasons { get; set; } = new();
}

public class IsHolidayResponse
{
    public DateTime Date { get; set; }
    public string Country { get; set; } = string.Empty;
    public bool IsHoliday { get; set; }
    public List<HolidayInfo> Holidays { get; set; } = new();
}

public class HolidayInfo
{
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public bool IsRecurring { get; set; }
}

public class RoutinePatternsResponse
{
    public Guid UserId { get; set; }
    public int AnalysisWindowDays { get; set; }
    public DateTime DetectedAt { get; set; }
    public List<RoutinePatternInfo> Patterns { get; set; } = new();
}

public class RoutinePatternInfo
{
    public string Type { get; set; } = string.Empty;
    public string DayOfWeek { get; set; } = string.Empty;
    public string AverageTime { get; set; } = string.Empty;
    public string TimeVariation { get; set; } = string.Empty;
    public int Frequency { get; set; }
    public double Confidence { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class OptimalTimesResponse
{
    public Guid UserId { get; set; }
    public string DayOfWeek { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public List<OptimalTimeSlotInfo> OptimalSlots { get; set; } = new();
}

public class OptimalTimeSlotInfo
{
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public double OptimalityScore { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public List<string> SupportingEvidence { get; set; } = new();
}