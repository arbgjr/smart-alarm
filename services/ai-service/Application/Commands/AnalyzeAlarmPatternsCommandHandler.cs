using MediatR;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Tracing;
using SmartAlarm.Observability.Metrics;
using FluentValidation;
using System.Diagnostics;

namespace SmartAlarm.AiService.Application.Commands
{
    /// <summary>
    /// Command para analisar padrões de uso de alarmes do usuário
    /// </summary>
    public record AnalyzeAlarmPatternsCommand(
        Guid UserId,
        DateTime? StartDate = null,
        DateTime? EndDate = null,
        int MaxDaysToAnalyze = 30
    ) : IRequest<AnalyzeAlarmPatternsResponse>;

    /// <summary>
    /// Response da análise de padrões de alarmes
    /// </summary>
    public record AnalyzeAlarmPatternsResponse(
        Guid UserId,
        DateTime AnalysisDate,
        AlarmUsagePatterns Patterns,
        IEnumerable<SmartRecommendation> Recommendations,
        AnalysisMetrics Metrics
    );

    /// <summary>
    /// Padrões de uso identificados pela IA
    /// </summary>
    public record AlarmUsagePatterns(
        TimeSpan MostCommonAlarmTime,
        IEnumerable<DayOfWeek> MostActiveDays,
        double AverageSnoozeCount,
        TimeSpan AverageWakeupDelay,
        string SleepPattern // "Early Bird", "Night Owl", "Irregular"
    );

    /// <summary>
    /// Recomendação inteligente gerada pela IA
    /// </summary>
    public record SmartRecommendation(
        string Type, // "TIME_ADJUSTMENT", "SCHEDULE_OPTIMIZATION", "SLEEP_HYGIENE"
        string Title,
        string Description,
        double ConfidenceScore,
        DateTime SuggestedImplementationDate
    );

    /// <summary>
    /// Métricas da análise realizada
    /// </summary>
    public record AnalysisMetrics(
        int AlarmsAnalyzed,
        int DaysAnalyzed,
        double PatternConfidence,
        DateTime LastAlarmActivity
    );

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
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly ILogger<AnalyzeAlarmPatternsCommandHandler> _logger;

        public AnalyzeAlarmPatternsCommandHandler(
            IAlarmRepository alarmRepository,
            IUserRepository userRepository,
            IValidator<AnalyzeAlarmPatternsCommand> validator,
            SmartAlarmActivitySource activitySource,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            ILogger<AnalyzeAlarmPatternsCommandHandler> logger)
        {
            _alarmRepository = alarmRepository;
            _userRepository = userRepository;
            _validator = validator;
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

                // Executar análise de IA (simulação)
                var patterns = AnalyzePatterns(alarmsToAnalyze, startDate, endDate);
                var recommendations = GenerateRecommendations(patterns, alarmsToAnalyze);
                var metrics = new AnalysisMetrics(
                    AlarmsAnalyzed: alarmsToAnalyze.Count,
                    DaysAnalyzed: daysAnalyzed,
                    PatternConfidence: CalculatePatternConfidence(alarmsToAnalyze),
                    LastAlarmActivity: alarmsToAnalyze.Max(a => a.LastTriggeredAt ?? a.CreatedAt)
                );

                activity?.SetTag("analysis.pattern_confidence", metrics.PatternConfidence.ToString("F2"));
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
        /// Analisa padrões de uso dos alarmes (simulação de ML)
        /// </summary>
        private static AlarmUsagePatterns AnalyzePatterns(IEnumerable<Alarm> alarms, DateTime startDate, DateTime endDate)
        {
            // Simulação de análise de ML - em produção seria ML.NET
            var averageTime = alarms.Average(a => a.Time.TimeOfDay.TotalMinutes);
            var mostCommonTime = TimeSpan.FromMinutes(averageTime);
            
            // Analisar dias mais ativos baseado nos schedules
            var daysFrequency = new Dictionary<System.DayOfWeek, int>();
            foreach (var alarm in alarms)
            {
                foreach (var schedule in alarm.Schedules.Where(s => s.IsActive))
                {
                    var daysOfWeek = schedule.DaysOfWeek;
                    if (daysOfWeek.HasFlag(SmartAlarm.Domain.Entities.DaysOfWeek.Monday)) daysFrequency[System.DayOfWeek.Monday] = daysFrequency.GetValueOrDefault(System.DayOfWeek.Monday) + 1;
                    if (daysOfWeek.HasFlag(SmartAlarm.Domain.Entities.DaysOfWeek.Tuesday)) daysFrequency[System.DayOfWeek.Tuesday] = daysFrequency.GetValueOrDefault(System.DayOfWeek.Tuesday) + 1;
                    if (daysOfWeek.HasFlag(SmartAlarm.Domain.Entities.DaysOfWeek.Wednesday)) daysFrequency[System.DayOfWeek.Wednesday] = daysFrequency.GetValueOrDefault(System.DayOfWeek.Wednesday) + 1;
                    if (daysOfWeek.HasFlag(SmartAlarm.Domain.Entities.DaysOfWeek.Thursday)) daysFrequency[System.DayOfWeek.Thursday] = daysFrequency.GetValueOrDefault(System.DayOfWeek.Thursday) + 1;
                    if (daysOfWeek.HasFlag(SmartAlarm.Domain.Entities.DaysOfWeek.Friday)) daysFrequency[System.DayOfWeek.Friday] = daysFrequency.GetValueOrDefault(System.DayOfWeek.Friday) + 1;
                    if (daysOfWeek.HasFlag(SmartAlarm.Domain.Entities.DaysOfWeek.Saturday)) daysFrequency[System.DayOfWeek.Saturday] = daysFrequency.GetValueOrDefault(System.DayOfWeek.Saturday) + 1;
                    if (daysOfWeek.HasFlag(SmartAlarm.Domain.Entities.DaysOfWeek.Sunday)) daysFrequency[System.DayOfWeek.Sunday] = daysFrequency.GetValueOrDefault(System.DayOfWeek.Sunday) + 1;
                }
            }

            var activeDays = daysFrequency
                .OrderByDescending(kvp => kvp.Value)
                .Take(3)
                .Select(kvp => kvp.Key)
                .ToList();

            return new AlarmUsagePatterns(
                MostCommonAlarmTime: mostCommonTime,
                MostActiveDays: activeDays,
                AverageSnoozeCount: 1.5, // Simulado
                AverageWakeupDelay: TimeSpan.FromMinutes(8), // Simulado
                SleepPattern: mostCommonTime.Hours < 7 ? "Early Bird" : mostCommonTime.Hours > 9 ? "Night Owl" : "Regular"
            );
        }

        /// <summary>
        /// Gera recomendações inteligentes baseadas nos padrões
        /// </summary>
        private static IEnumerable<SmartRecommendation> GenerateRecommendations(AlarmUsagePatterns patterns, IEnumerable<Alarm> alarms)
        {
            var recommendations = new List<SmartRecommendation>();

            // Recomendação de otimização de horário
            if (patterns.SleepPattern == "Irregular")
            {
                recommendations.Add(new SmartRecommendation(
                    Type: "SCHEDULE_OPTIMIZATION",
                    Title: "Padronização de Horários",
                    Description: "Seus horários de alarme variam muito. Considere manter um horário mais consistente para melhorar a qualidade do sono.",
                    ConfidenceScore: 0.85,
                    SuggestedImplementationDate: DateTime.Today.AddDays(1)
                ));
            }

            // Recomendação de ajuste de horário
            if (patterns.AverageWakeupDelay.TotalMinutes > 10)
            {
                recommendations.Add(new SmartRecommendation(
                    Type: "TIME_ADJUSTMENT",
                    Title: "Ajuste de Horário",
                    Description: $"Você demora em média {patterns.AverageWakeupDelay.TotalMinutes:F0} minutos para levantar. Considere adiantar seus alarmes.",
                    ConfidenceScore: 0.78,
                    SuggestedImplementationDate: DateTime.Today.AddDays(3)
                ));
            }

            return recommendations;
        }

        /// <summary>
        /// Calcula a confiança dos padrões identificados
        /// </summary>
        private static double CalculatePatternConfidence(IEnumerable<Alarm> alarms)
        {
            // Simulação de cálculo de confiança baseado na quantidade de dados
            var alarmCount = alarms.Count();
            return Math.Min(0.95, 0.3 + (alarmCount * 0.1));
        }
    }
}
