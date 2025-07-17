using MediatR;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Tracing;
using SmartAlarm.Observability.Metrics;
using FluentValidation;
using System.Diagnostics;

namespace SmartAlarm.AiService.Application.Queries
{
    /// <summary>
    /// Query para predizer horários ótimos para alarmes usando IA
    /// </summary>
    public record PredictOptimalTimeQuery(
        Guid UserId,
        System.DayOfWeek TargetDay,
        string? Context = null, // "work", "exercise", "appointment"
        TimeSpan? PreferredTimeRange = null
    ) : IRequest<PredictOptimalTimeResponse>;

    /// <summary>
    /// Response da predição de horários ótimos
    /// </summary>
    public record PredictOptimalTimeResponse(
        Guid UserId,
        System.DayOfWeek TargetDay,
        IEnumerable<OptimalTimeSlot> Predictions,
        PredictionMetrics Metrics,
        DateTime PredictionDate
    );

    /// <summary>
    /// Slot de horário otimizado previsto pela IA
    /// </summary>
    public record OptimalTimeSlot(
        TimeSpan SuggestedTime,
        double ConfidenceScore,
        string Reasoning,
        string Category, // "sleep_pattern", "historical_data", "context_based"
        TimeSpan? AlternativeTime = null
    );

    /// <summary>
    /// Métricas da predição realizada
    /// </summary>
    public record PredictionMetrics(
        int HistoricalDataPoints,
        double ModelAccuracy,
        string ModelVersion,
        IEnumerable<string> FactorsConsidered
    );

    /// <summary>
    /// Validator para query de predição de horários
    /// </summary>
    public class PredictOptimalTimeQueryValidator : AbstractValidator<PredictOptimalTimeQuery>
    {
        public PredictOptimalTimeQueryValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("UserId é obrigatório");

            RuleFor(x => x.TargetDay)
                .IsInEnum()
                .WithMessage("TargetDay deve ser um dia válido da semana");

            RuleFor(x => x.Context)
                .Must(context => context == null || new[] { "work", "exercise", "appointment", "personal", "sleep" }.Contains(context))
                .WithMessage("Context deve ser: work, exercise, appointment, personal ou sleep");
        }
    }

    /// <summary>
    /// Handler para predição de horários ótimos usando IA
    /// </summary>
    public class PredictOptimalTimeQueryHandler : IRequestHandler<PredictOptimalTimeQuery, PredictOptimalTimeResponse>
    {
        private readonly IAlarmRepository _alarmRepository;
        private readonly IUserRepository _userRepository;
        private readonly IValidator<PredictOptimalTimeQuery> _validator;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly ILogger<PredictOptimalTimeQueryHandler> _logger;

        public PredictOptimalTimeQueryHandler(
            IAlarmRepository alarmRepository,
            IUserRepository userRepository,
            IValidator<PredictOptimalTimeQuery> validator,
            SmartAlarmActivitySource activitySource,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            ILogger<PredictOptimalTimeQueryHandler> logger)
        {
            _alarmRepository = alarmRepository;
            _userRepository = userRepository;
            _validator = validator;
            _activitySource = activitySource;
            _meter = meter;
            _correlationContext = correlationContext;
            _logger = logger;
        }

        public async Task<PredictOptimalTimeResponse> Handle(PredictOptimalTimeQuery request, CancellationToken cancellationToken)
        {
            using var activity = _activitySource.StartActivity("PredictOptimalTimeQueryHandler.Handle");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Activity tags
                activity?.SetTag("user.id", request.UserId.ToString());
                activity?.SetTag("operation", "predict_optimal_time");
                activity?.SetTag("target.day", request.TargetDay.ToString());
                activity?.SetTag("context", request.Context ?? "none");
                activity?.SetTag("correlation.id", _correlationContext.CorrelationId);

                _logger.LogInformation("Iniciando predição de horário ótimo para usuário {UserId} - Dia: {TargetDay}, Context: {Context} - CorrelationId: {CorrelationId}",
                    request.UserId, request.TargetDay, request.Context, _correlationContext.CorrelationId);

                // Validação
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    activity?.SetTag("validation.failed", true);
                    _meter.IncrementErrorCount("query", "predict_optimal_time", "validation");
                    
                    var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning("Validação falhou para predição de horário: {Errors} - CorrelationId: {CorrelationId}",
                        errors, _correlationContext.CorrelationId);
                    
                    throw new ValidationException($"Dados inválidos: {errors}");
                }

                // Verificar se usuário existe
                var user = await _userRepository.GetByIdAsync(request.UserId);
                if (user == null)
                {
                    activity?.SetTag("user.found", false);
                    _meter.IncrementErrorCount("query", "predict_optimal_time", "user_not_found");
                    
                    _logger.LogWarning("Usuário {UserId} não encontrado para predição de horário - CorrelationId: {CorrelationId}",
                        request.UserId, _correlationContext.CorrelationId);
                    
                    throw new InvalidOperationException($"Usuário {request.UserId} não encontrado");
                }

                activity?.SetTag("user.found", true);
                activity?.SetTag("user.email", user.Email.Address);

                // Buscar histórico de alarmes do usuário
                var alarms = await _alarmRepository.GetByUserIdAsync(request.UserId);
                var relevantAlarms = alarms.Where(a => a.Enabled).ToList();

                activity?.SetTag("historical.alarms_count", relevantAlarms.Count.ToString());

                // Executar predição usando IA (simulação)
                var predictions = PredictOptimalTimes(relevantAlarms, request.TargetDay, request.Context, request.PreferredTimeRange);
                var metrics = new PredictionMetrics(
                    HistoricalDataPoints: relevantAlarms.Count,
                    ModelAccuracy: 0.87, // Simulado
                    ModelVersion: "SmartAlarm.AI.v1.2.3",
                    FactorsConsidered: GetFactorsConsidered(request.Context, relevantAlarms.Count)
                );

                activity?.SetTag("prediction.model_accuracy", metrics.ModelAccuracy.ToString("F2"));
                activity?.SetTag("prediction.slots_count", predictions.Count().ToString());

                stopwatch.Stop();

                // Métricas de sucesso
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "predict_optimal_time", "success", "200");

                // Log de sucesso
                _logger.LogInformation("Predição de horário concluída para usuário {UserId} - Dia: {TargetDay}, Slots: {SlotsCount}, Acurácia: {Accuracy:F2} - Duração: {Duration}ms - CorrelationId: {CorrelationId}",
                    request.UserId, request.TargetDay, predictions.Count(), metrics.ModelAccuracy, stopwatch.ElapsedMilliseconds, _correlationContext.CorrelationId);

                // Retornar response
                return new PredictOptimalTimeResponse(
                    request.UserId,
                    request.TargetDay,
                    predictions,
                    metrics,
                    DateTime.UtcNow
                );
            }
            catch (ValidationException)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "predict_optimal_time", "validation_error", "400");
                throw;
            }
            catch (InvalidOperationException)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "predict_optimal_time", "user_not_found", "404");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "predict_optimal_time", "error", "500");
                _meter.IncrementErrorCount("query", "predict_optimal_time", "exception");
                
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);
                
                _logger.LogError(ex, "Erro inesperado na predição de horário para usuário {UserId} - CorrelationId: {CorrelationId}",
                    request.UserId, _correlationContext.CorrelationId);
                
                throw;
            }
        }

        /// <summary>
        /// Prediz horários ótimos usando algoritmos de ML (simulação)
        /// </summary>
        private static IEnumerable<OptimalTimeSlot> PredictOptimalTimes(
            IEnumerable<Alarm> historicalAlarms, 
            System.DayOfWeek targetDay, 
            string? context,
            TimeSpan? preferredRange)
        {
            var predictions = new List<OptimalTimeSlot>();

            // Análise baseada em padrões históricos
            if (historicalAlarms.Any())
            {
                var avgTime = historicalAlarms.Average(a => a.Time.TimeOfDay.TotalMinutes);
                var historicalOptimal = TimeSpan.FromMinutes(avgTime);

                predictions.Add(new OptimalTimeSlot(
                    SuggestedTime: historicalOptimal,
                    ConfidenceScore: 0.85,
                    Reasoning: "Baseado no seu padrão histórico de alarmes",
                    Category: "historical_data",
                    AlternativeTime: historicalOptimal.Add(TimeSpan.FromMinutes(15))
                ));
            }

            // Análise baseada no contexto
            if (!string.IsNullOrEmpty(context))
            {
                var contextTime = context switch
                {
                    "work" => TimeSpan.FromHours(7), // 07:00
                    "exercise" => TimeSpan.FromHours(6), // 06:00
                    "appointment" => TimeSpan.FromHours(8.5), // 08:30
                    "personal" => TimeSpan.FromHours(9), // 09:00
                    "sleep" => TimeSpan.FromHours(22), // 22:00
                    _ => TimeSpan.FromHours(7.5) // 07:30 padrão
                };

                predictions.Add(new OptimalTimeSlot(
                    SuggestedTime: contextTime,
                    ConfidenceScore: 0.78,
                    Reasoning: $"Horário otimizado para contexto: {context}",
                    Category: "context_based"
                ));
            }

            // Análise baseada no dia da semana
            var dayBasedTime = targetDay switch
            {
                System.DayOfWeek.Saturday or System.DayOfWeek.Sunday => TimeSpan.FromHours(8.5), // Fins de semana mais tarde
                System.DayOfWeek.Monday => TimeSpan.FromHours(6.5), // Segunda mais cedo
                _ => TimeSpan.FromHours(7) // Dias normais
            };

            predictions.Add(new OptimalTimeSlot(
                SuggestedTime: dayBasedTime,
                ConfidenceScore: 0.72,
                Reasoning: $"Horário otimizado para {targetDay}",
                Category: "sleep_pattern"
            ));

            return predictions.OrderByDescending(p => p.ConfidenceScore);
        }

        /// <summary>
        /// Retorna fatores considerados na predição
        /// </summary>
        private static IEnumerable<string> GetFactorsConsidered(string? context, int historicalDataCount)
        {
            var factors = new List<string>
            {
                "Padrões de sono",
                "Histórico de alarmes",
                "Dia da semana"
            };

            if (!string.IsNullOrEmpty(context))
                factors.Add($"Contexto: {context}");

            if (historicalDataCount > 10)
                factors.Add("Análise estatística avançada");

            return factors;
        }
    }
}
