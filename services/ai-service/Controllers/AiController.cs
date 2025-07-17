using Microsoft.AspNetCore.Mvc;
using MediatR;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Tracing;
using SmartAlarm.Observability.Metrics;
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
        /// Obtém recomendações de IA para configuração de alarmes
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <returns>Recomendações personalizadas</returns>
        [HttpGet("recommendations/{userId:guid}")]
        public async Task<IActionResult> GetRecommendations(Guid userId)
        {
            using var activity = _activitySource.StartActivity("AiController.GetRecommendations");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                activity?.SetTag("user.id", userId.ToString());
                activity?.SetTag("operation", "get_ai_recommendations");
                
                _logger.LogInformation("Iniciando busca de recomendações de IA para usuário {UserId} - CorrelationId: {CorrelationId}", 
                    userId, _correlationContext.CorrelationId);

                // TODO: Implementar comando para buscar recomendações
                // var recommendations = await _mediator.Send(new GetAiRecommendationsQuery(userId));
                
                var mockRecommendations = new
                {
                    UserId = userId,
                    Recommendations = new[]
                    {
                        new { Type = "OptimalTime", Value = "07:30", Confidence = 0.85 },
                        new { Type = "AlarmTone", Value = "Nature Sounds", Confidence = 0.72 },
                        new { Type = "SnoozeInterval", Value = "5 minutes", Confidence = 0.68 }
                    },
                    GeneratedAt = DateTime.UtcNow
                };

                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "get_ai_recommendations", "success", "200");
                
                _logger.LogInformation("Recomendações de IA obtidas com sucesso para usuário {UserId} em {Duration}ms", 
                    userId, stopwatch.ElapsedMilliseconds);

                return Ok(mockRecommendations);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "get_ai_recommendations", "error", "500");
                _meter.IncrementErrorCount("controller", "ai_recommendations", "exception");
                
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);
                
                _logger.LogError(ex, "Erro ao obter recomendações de IA para usuário {UserId} - CorrelationId: {CorrelationId}", 
                    userId, _correlationContext.CorrelationId);

                return StatusCode(500, new { error = "Erro interno do servidor", correlationId = _correlationContext.CorrelationId });
            }
        }

        /// <summary>
        /// Analisa padrões de uso do usuário
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <returns>Análise de padrões comportamentais</returns>
        [HttpGet("analysis/{userId:guid}")]
        public async Task<IActionResult> GetBehavioralAnalysis(Guid userId)
        {
            using var activity = _activitySource.StartActivity("AiController.GetBehavioralAnalysis");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                activity?.SetTag("user.id", userId.ToString());
                activity?.SetTag("operation", "behavioral_analysis");
                
                _logger.LogInformation("Iniciando análise comportamental para usuário {UserId} - CorrelationId: {CorrelationId}", 
                    userId, _correlationContext.CorrelationId);

                // TODO: Implementar análise comportamental real
                var mockAnalysis = new
                {
                    UserId = userId,
                    Patterns = new
                    {
                        WakeUpTimes = new[] { "07:00", "07:15", "07:30" },
                        SleepPatterns = "Regular",
                        AlarmUsage = "High",
                        SnoozeFrequency = "Low"
                    },
                    Score = 0.82,
                    AnalyzedAt = DateTime.UtcNow
                };

                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "behavioral_analysis", "success", "200");
                
                _logger.LogInformation("Análise comportamental concluída para usuário {UserId} em {Duration}ms", 
                    userId, stopwatch.ElapsedMilliseconds);

                return Ok(mockAnalysis);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "behavioral_analysis", "error", "500");
                _meter.IncrementErrorCount("controller", "behavioral_analysis", "exception");
                
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);
                
                _logger.LogError(ex, "Erro na análise comportamental para usuário {UserId} - CorrelationId: {CorrelationId}", 
                    userId, _correlationContext.CorrelationId);

                return StatusCode(500, new { error = "Erro interno do servidor", correlationId = _correlationContext.CorrelationId });
            }
        }
    }
}
