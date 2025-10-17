using MediatR;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Tracing;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.AiService.Infrastructure.MachineLearning;
using FluentValidation;
using System.Diagnostics;

namespace SmartAlarm.AiService.Application.Commands
{
    /// <summary>
    /// Validator para comando de análise de padrões
    /// </summary>
    public class AnalyzeAlarmPatternsCommandValidator : AbstractValidator<AnalyzeAlarmPatternsCommand>
    {
        public AnalyzeAlarmPatternsCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("UserId é obrigatório");

            RuleFor(x => x.MaxDaysToAnalyze)
                .InclusiveBetween(7, 365)
                .WithMessage("MaxDaysToAnalyze deve estar entre 7 e 365 dias");

            RuleFor(x => x.StartDate)
                .LessThan(x => x.EndDate)
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
                .WithMessage("StartDate deve ser anterior a EndDate");
        }
    }

    /// <summary>
    /// Handler para análise de padrões de alarmes usando IA
    /// </summary>
    public class AnalyzeAlarmPatternsCommandHandler : IRequestHandler<AnalyzeAlarmPatternsCommand, AnalyzeAlarmPatternsResponse>
    {
        private readonly IAlarmRepository _alarmRepository;
        private readonly IUserRepository _userRepository;
        private readonly IValidator<AnalyzeAlarmPatternsCommand> _validator;
        private readonly IMachineLearningService _machineLearningService;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly ILogger<AnalyzeAlarmPatternsCommandHandler> _logger;

        public AnalyzeAlarmPatternsCommandHandler(
            IAlarmRepository alarmRepository,
            IUserRepository userRepository,
            IValidator<AnalyzeAlarmPatternsCommand> validator,
            IMachineLearningService machineLearningService,
            SmartAlarmActivitySource activitySource,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            ILogger<AnalyzeAlarmPatternsCommandHandler> logger)
        {
            _alarmRepository = alarmRepository;
            _userRepository = userRepository;
            _validator = validator;
            _machineLearningService = machineLearningService;
            _activitySource = activitySource;
            _meter = meter;
            _correlationContext = correlationContext;
            _logger = logger;
        }

        public async Task<AnalyzeAlarmPatternsResponse> Handle(AnalyzeAlarmPatternsCommand request, CancellationToken cancellationToken)
        {
            using var activity = _activitySource.StartActivity("AnalyzeAlarmPatternsCommandHandler.Handle");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Activity tags
                activity?.SetTag("user.id", request.UserId.ToString());
                activity?.SetTag("operation", "analyze_alarm_patterns");
                activity?.SetTag("analysis.max_days", request.MaxDaysToAnalyze.ToString());
                activity?.SetTag("correlation.id", _correlationContext.CorrelationId);

                _logger.LogInformation("Iniciando análise de padrões de alarmes para usuário {UserId} - MaxDays: {MaxDays} - CorrelationId: {CorrelationId}",
                    request.UserId, request.MaxDaysToAnalyze, _correlationContext.CorrelationId);

                // Validação
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    activity?.SetTag("validation.failed", true);
                    _meter.IncrementErrorCount("command", "analyze_alarm_patterns", "validation");

                    var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning("Validação falhou para análise de padrões: {Errors} - CorrelationId: {CorrelationId}",
                        errors, _correlationContext.CorrelationId);

                    throw new ValidationException($"Dados inválidos: {errors}");
                }

                // Verificar se usuário existe
                var user = await _userRepository.GetByIdAsync(request.UserId);
                if (user == null)
                {
                    activity?.SetTag("user.found", false);
                    _meter.IncrementErrorCount("command", "analyze_alarm_patterns", "user_not_found");

                    _logger.LogWarning("Usuário {UserId} não encontrado para análise de padrões - CorrelationId: {CorrelationId}",
                        request.UserId, _correlationContext.CorrelationId);

                    throw new InvalidOperationException($"Usuário {request.UserId} não encontrado");
                }

                activity?.SetTag("user.found", true);
                activity?.SetTag("user.email", user.Email.Address);

                // Buscar alarmes do usuário para análise
                var alarms = await _alarmRepository.GetByUserIdAsync(request.UserId);
                var alarmsToAnalyze = alarms.Where(a => a.Enabled).ToList();

                if (!alarmsToAnalyze.Any())
                {
                    activity?.SetTag("alarms.found", false);
                    _logger.LogWarning("Nenhum alarme ativo encontrado para análise - UserId: {UserId} - CorrelationId: {CorrelationId}",
                        request.UserId, _correlationContext.CorrelationId);

                    throw new InvalidOperationException("Nenhum alarme ativo encontrado para análise");
                }

                activity?.SetTag("alarms.count", alarmsToAnalyze.Count.ToString());

                // Definir período de análise
                var endDate = request.EndDate ?? DateTime.UtcNow;
                var startDate = request.StartDate ?? endDate.AddDays(-request.MaxDaysToAnalyze);
                var daysAnalyzed = (int)(endDate - startDate).TotalDays;

                activity?.SetTag("analysis.start_date", startDate.ToString("yyyy-MM-dd"));
                activity?.SetTag("analysis.end_date", endDate.ToString("yyyy-MM-dd"));
                activity?.SetTag("analysis.days_analyzed", daysAnalyzed.ToString());

                // Executar análise de IA usando ML.NET
                _logger.LogInformation("Executando análise ML.NET para {AlarmCount} alarmes - UserId: {UserId} - CorrelationId: {CorrelationId}",
                    alarmsToAnalyze.Count, request.UserId, _correlationContext.CorrelationId);

                var mlResult = await _machineLearningService.AnalyzeAlarmPatternsAsync(
                    alarmsToAnalyze, startDate, endDate);

                // Converter resultado do ML.NET para o formato esperado
                var patterns = new AlarmUsagePatterns(
                    MostCommonAlarmTime: mlResult.MostCommonAlarmTime,
                    MostActiveDays: mlResult.MostActiveDays,
                    AverageSnoozeCount: mlResult.AverageSnoozeCount,
                    AverageWakeupDelay: mlResult.AverageWakeupDelay,
                    SleepPattern: mlResult.SleepPattern
                );

                var recommendations = GenerateRecommendationsFromMLResult(mlResult, alarmsToAnalyze);
                var metrics = new AnalysisMetrics(
                    AlarmsAnalyzed: alarmsToAnalyze.Count,
                    DaysAnalyzed: daysAnalyzed,
                    PatternConfidence: mlResult.PatternConfidence,
                    LastAlarmActivity: alarmsToAnalyze.Max(a => a.LastTriggeredAt ?? a.CreatedAt)
                );

                activity?.SetTag("ml.model_accuracy", mlResult.ModelAccuracy.ToString("F2"));
                activity?.SetTag("analysis.pattern_confidence", mlResult.PatternConfidence.ToString("F2"));
                activity?.SetTag("analysis.recommendations_count", recommendations.Count().ToString());

                stopwatch.Stop();

                // Métricas de sucesso
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "analyze_alarm_patterns", "success", "200");

                // Log de sucesso
                _logger.LogInformation("Análise de padrões concluída para usuário {UserId} - Alarmes: {AlarmsCount}, Recomendações: {RecommendationsCount}, Confiança: {Confidence:F2}% - Duração: {Duration}ms - CorrelationId: {CorrelationId}",
                    request.UserId, alarmsToAnalyze.Count, recommendations.Count(), metrics.PatternConfidence, stopwatch.ElapsedMilliseconds, _correlationContext.CorrelationId);

                // Retornar response
                return new AnalyzeAlarmPatternsResponse(
                    request.UserId,
                    DateTime.UtcNow,
                    patterns,
                    recommendations,
                    metrics
                );
            }
            catch (ValidationException)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "analyze_alarm_patterns", "validation_error", "400");
                throw;
            }
            catch (InvalidOperationException)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "analyze_alarm_patterns", "business_error", "409");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "analyze_alarm_patterns", "error", "500");
                _meter.IncrementErrorCount("command", "analyze_alarm_patterns", "exception");

                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);

                _logger.LogError(ex, "Erro inesperado na análise de padrões para usuário {UserId} - CorrelationId: {CorrelationId}",
                    request.UserId, _correlationContext.CorrelationId);

                throw;
            }
        }

        /// <summary>
        /// Gera recomendações inteligentes baseadas no resultado do ML.NET
        /// </summary>
        private static IEnumerable<SmartRecommendation> GenerateRecommendationsFromMLResult(AlarmPatternAnalysisResult mlResult, IEnumerable<Alarm> alarms)
        {
            var recommendations = new List<SmartRecommendation>();

            // Recomendação de otimização de horário baseada no ML.NET
            if (mlResult.SleepPattern == "Irregular")
            {
                recommendations.Add(new SmartRecommendation(
                    Type: "SCHEDULE_OPTIMIZATION",
                    Title: "Padronização de Horários (ML.NET Analysis)",
                    Description: $"Nosso modelo ML.NET identificou padrão irregular com {mlResult.PatternConfidence:F1}% de confiança. Considere manter um horário mais consistente para melhorar a qualidade do sono.",
                    ConfidenceScore: mlResult.PatternConfidence,
                    SuggestedImplementationDate: DateTime.Today.AddDays(1)
                ));
            }

            // Recomendação de ajuste de horário baseada no ML.NET
            if (mlResult.AverageWakeupDelay.TotalMinutes > 10)
            {
                var adjustmentMinutes = (int)Math.Ceiling(mlResult.AverageWakeupDelay.TotalMinutes);
                recommendations.Add(new SmartRecommendation(
                    Type: "TIME_ADJUSTMENT",
                    Title: "Ajuste de Horário (ML.NET Prediction)",
                    Description: $"Análise ML.NET mostra atraso médio de {adjustmentMinutes} minutos. Modelo sugere adiantar seus alarmes com {mlResult.ModelAccuracy:F1}% de precisão.",
                    ConfidenceScore: Math.Min(0.9, mlResult.ModelAccuracy),
                    SuggestedImplementationDate: DateTime.Today.AddDays(2)
                ));
            }

            // Recomendação baseada na contagem de soneca
            if (mlResult.AverageSnoozeCount > 2)
            {
                recommendations.Add(new SmartRecommendation(
                    Type: "SLEEP_HYGIENE",
                    Title: "Redução de Sonecas (ML Analysis)",
                    Description: $"Detectada média de {mlResult.AverageSnoozeCount:F1} sonecas por alarme. Considere ajustar horário de dormir para reduzir dependência de soneca.",
                    ConfidenceScore: Math.Min(0.85, mlResult.PatternConfidence),
                    SuggestedImplementationDate: DateTime.Today.AddDays(7)
                ));
            }

            // Recomendação para Early Birds ou Night Owls
            if (mlResult.SleepPattern == "Early Bird" && mlResult.MostCommonAlarmTime.Hours < 6)
            {
                recommendations.Add(new SmartRecommendation(
                    Type: "SCHEDULE_OPTIMIZATION",
                    Title: "Otimização para Madrugadores",
                    Description: "Padrão Early Bird detectado. Considere criar rotinas noturnas mais estruturadas para maximizar a qualidade do sono limitado.",
                    ConfidenceScore: mlResult.ModelAccuracy,
                    SuggestedImplementationDate: DateTime.Today.AddDays(3)
                ));
            }
            else if (mlResult.SleepPattern == "Night Owl" && mlResult.MostCommonAlarmTime.Hours > 9)
            {
                recommendations.Add(new SmartRecommendation(
                    Type: "SCHEDULE_OPTIMIZATION",
                    Title: "Ajuste para Notívagos",
                    Description: "Padrão Night Owl identificado. Considere gradualmente adiantar horários para melhor alinhamento com responsabilidades matutinas.",
                    ConfidenceScore: mlResult.ModelAccuracy,
                    SuggestedImplementationDate: DateTime.Today.AddDays(5)
                ));
            }

            return recommendations.OrderByDescending(r => r.ConfidenceScore);
        }
    }
}
