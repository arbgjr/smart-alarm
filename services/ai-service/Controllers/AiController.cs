using Microsoft.AspNetCore.Mvc;
using MediatR;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Tracing;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.AiService.Application.Commands;
using SmartAlarm.AiService.Application.Queries;
using System.Diagnostics;

namespace SmartAlarm.AiService.Controllers
{
    /// <summary>
    /// Controller para operações de IA e análise comportamental
    /// </summary>
    [ApiController]
    [Route("api/v1/ai")]
    [Produces("application/json")]
    public class AiController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<AiController> _logger;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;

        public AiController(
            IMediator mediator,
            ILogger<AiController> logger,
            SmartAlarmActivitySource activitySource,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext)
        {
            _mediator = mediator;
            _logger = logger;
            _activitySource = activitySource;
            _meter = meter;
            _correlationContext = correlationContext;
        }

        /// <summary>
        /// Analisa padrões de uso de alarmes do usuário
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <param name="startDate">Data de início da análise (opcional)</param>
        /// <param name="endDate">Data de fim da análise (opcional)</param>
        /// <param name="maxDaysToAnalyze">Máximo de dias para analisar (padrão: 30)</param>
        /// <returns>Análise de padrões de alarmes</returns>
        [HttpPost("analyze-patterns")]
        public async Task<IActionResult> AnalyzeAlarmPatterns(
            [FromQuery] Guid userId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int maxDaysToAnalyze = 30)
        {
            using var activity = _activitySource.StartActivity("AiController.AnalyzeAlarmPatterns");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                activity?.SetTag("user.id", userId.ToString());
                activity?.SetTag("operation", "analyze_alarm_patterns");
                activity?.SetTag("max_days", maxDaysToAnalyze.ToString());
                
                _logger.LogInformation("Iniciando análise de padrões de alarmes para usuário {UserId} - CorrelationId: {CorrelationId}", 
                    userId, _correlationContext.CorrelationId);

                var command = new AnalyzeAlarmPatternsCommand(userId, startDate, endDate, maxDaysToAnalyze);
                var result = await _mediator.Send(command);

                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "analyze_alarm_patterns", "success", "200");
                
                _logger.LogInformation("Análise de padrões de alarmes concluída para usuário {UserId} em {Duration}ms", 
                    userId, stopwatch.ElapsedMilliseconds);

                return Ok(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "analyze_alarm_patterns", "error", "500");
                _meter.IncrementErrorCount("controller", "analyze_alarm_patterns", "exception");
                
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);
                
                _logger.LogError(ex, "Erro ao analisar padrões de alarmes para usuário {UserId} - CorrelationId: {CorrelationId}", 
                    userId, _correlationContext.CorrelationId);

                return StatusCode(500, new { error = "Erro interno do servidor", correlationId = _correlationContext.CorrelationId });
            }
        }

        /// <summary>
        /// Prediz horários ótimos para alarmes usando IA
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <param name="targetDay">Dia da semana desejado</param>
        /// <param name="context">Contexto do alarme (trabalho, exercício, compromisso)</param>
        /// <param name="preferredTimeRangeHours">Faixa de horário preferida em horas (opcional)</param>
        /// <returns>Predições de horários ótimos</returns>
        [HttpGet("predict-optimal-time")]
        public async Task<IActionResult> PredictOptimalTime(
            [FromQuery] Guid userId,
            [FromQuery] DayOfWeek targetDay,
            [FromQuery] string? context = null,
            [FromQuery] int? preferredTimeRangeHours = null)
        {
            using var activity = _activitySource.StartActivity("AiController.PredictOptimalTime");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                activity?.SetTag("user.id", userId.ToString());
                activity?.SetTag("operation", "predict_optimal_time");
                activity?.SetTag("target_day", targetDay.ToString());
                activity?.SetTag("context", context ?? "none");
                
                _logger.LogInformation("Iniciando predição de horário ótimo para usuário {UserId}, dia {TargetDay} - CorrelationId: {CorrelationId}", 
                    userId, targetDay, _correlationContext.CorrelationId);

                TimeSpan? preferredTimeRange = preferredTimeRangeHours.HasValue 
                    ? TimeSpan.FromHours(preferredTimeRangeHours.Value) 
                    : null;

                var query = new PredictOptimalTimeQuery(userId, targetDay, context, preferredTimeRange);
                var result = await _mediator.Send(query);

                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "predict_optimal_time", "success", "200");
                
                _logger.LogInformation("Predição de horário ótimo concluída para usuário {UserId} em {Duration}ms", 
                    userId, stopwatch.ElapsedMilliseconds);

                return Ok(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "predict_optimal_time", "error", "500");
                _meter.IncrementErrorCount("controller", "predict_optimal_time", "exception");
                
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);
                
                _logger.LogError(ex, "Erro ao predizer horário ótimo para usuário {UserId} - CorrelationId: {CorrelationId}", 
                    userId, _correlationContext.CorrelationId);

                return StatusCode(500, new { error = "Erro interno do servidor", correlationId = _correlationContext.CorrelationId });
            }
        }
    }
}
