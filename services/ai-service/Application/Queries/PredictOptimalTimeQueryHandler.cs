using MediatR;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Tracing;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.AiService.Infrastructure.MachineLearning;
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
        private readonly IMachineLearningService _machineLearningService;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly ILogger<PredictOptimalTimeQueryHandler> _logger;

        public PredictOptimalTimeQueryHandler(
            IAlarmRepository alarmRepository,
            IUserRepository userRepository,
            IValidator<PredictOptimalTimeQuery> validator,
            IMachineLearningService machineLearningService,
            SmartAlarmActivitySource activitySource,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            ILogger<PredictOptimalTimeQueryHandler> logger)
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

                // Executar predição usando ML.NET
                _logger.LogInformation("Executando predição ML.NET para {TargetDay} com {AlarmCount} alarmes históricos - CorrelationId: {CorrelationId}",
                    request.TargetDay, relevantAlarms.Count, _correlationContext.CorrelationId);

                var mlResult = await _machineLearningService.PredictOptimalTimeAsync(
                    relevantAlarms, request.TargetDay, request.Context, request.PreferredTimeRange);

                // Converter resultado do ML.NET para múltiplos slots
                var predictions = CreateOptimalTimeSlotsFromMLResult(mlResult, request.TargetDay, request.Context);
                
                var metrics = new PredictionMetrics(
                    HistoricalDataPoints: relevantAlarms.Count,
                    ModelAccuracy: mlResult.ModelAccuracy,
                    ModelVersion: "ML.NET SmartAlarm v2.0.0",
                    FactorsConsidered: mlResult.FactorsConsidered ?? new[] { "Análise ML.NET", "Padrões históricos" }
                );

                activity?.SetTag("prediction.ml_model_accuracy", mlResult.ModelAccuracy.ToString("F2"));
                activity?.SetTag("prediction.ml_confidence", mlResult.ConfidenceScore.ToString("F2"));
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
        /// Cria múltiplos slots de horário ótimo baseado no resultado do ML.NET
        /// </summary>
        private static IEnumerable<OptimalTimeSlot> CreateOptimalTimeSlotsFromMLResult(
            OptimalTimePredictionResult mlResult, 
            DayOfWeek targetDay, 
            string? context)
        {
            var slots = new List<OptimalTimeSlot>();

            // Slot principal baseado na predição ML.NET
            slots.Add(new OptimalTimeSlot(
                SuggestedTime: mlResult.SuggestedTime,
                ConfidenceScore: mlResult.ConfidenceScore,
                Reasoning: $"ML.NET Prediction: {mlResult.Reasoning}",
                Category: "ml_net_primary",
                AlternativeTime: mlResult.AlternativeTime
            ));

            // Slot alternativo se fornecido pelo ML.NET
            if (mlResult.AlternativeTime.HasValue)
            {
                slots.Add(new OptimalTimeSlot(
                    SuggestedTime: mlResult.AlternativeTime.Value,
                    ConfidenceScore: Math.Max(0.1, mlResult.ConfidenceScore - 0.15),
                    Reasoning: "Horário alternativo baseado em análise ML.NET",
                    Category: "ml_net_alternative"
                ));
            }

            // Slot baseado no contexto específico (se fornecido)
            if (!string.IsNullOrEmpty(context))
            {
                var contextTime = GetContextBasedTime(context);
                var contextConfidence = CalculateContextConfidence(mlResult.SuggestedTime, contextTime, mlResult.ModelAccuracy);
                
                if (contextConfidence > 0.5) // Só adicionar se tiver confiança razoável
                {
                    slots.Add(new OptimalTimeSlot(
                        SuggestedTime: contextTime,
                        ConfidenceScore: contextConfidence,
                        Reasoning: $"Otimizado para contexto: {context} com base em ML.NET",
                        Category: "context_based_ml"
                    ));
                }
            }

            // Slot baseado no dia da semana (refinado com ML.NET)
            var dayBasedTime = GetDayBasedTime(targetDay, mlResult.SuggestedTime);
            var dayConfidence = CalculateDayConfidence(targetDay, mlResult.ModelAccuracy);
            
            slots.Add(new OptimalTimeSlot(
                SuggestedTime: dayBasedTime,
                ConfidenceScore: dayConfidence,
                Reasoning: $"Otimizado para {GetDayDescription(targetDay)} baseado em análise ML.NET",
                Category: "day_optimized_ml"
            ));

            return slots.OrderByDescending(s => s.ConfidenceScore).Take(4); // Top 4 slots
        }

        /// <summary>
        /// Obtém horário baseado no contexto
        /// </summary>
        private static TimeSpan GetContextBasedTime(string context)
        {
            return context.ToLower() switch
            {
                "work" => TimeSpan.FromHours(6.5),   // 06:30
                "exercise" => TimeSpan.FromHours(5.5), // 05:30
                "appointment" => TimeSpan.FromHours(8), // 08:00
                "personal" => TimeSpan.FromHours(8.5),  // 08:30
                "sleep" => TimeSpan.FromHours(22),      // 22:00
                _ => TimeSpan.FromHours(7)              // 07:00 padrão
            };
        }

        /// <summary>
        /// Calcula confiança baseada na distância entre horários ML.NET e contexto
        /// </summary>
        private static double CalculateContextConfidence(TimeSpan mlTime, TimeSpan contextTime, double baseAccuracy)
        {
            var timeDifference = Math.Abs((mlTime - contextTime).TotalHours);
            
            // Menor diferença = maior confiança
            var proximityScore = Math.Max(0, 1 - (timeDifference / 12)); // Normalizar em 12h
            return Math.Min(0.95, baseAccuracy * proximityScore);
        }

        /// <summary>
        /// Obtém horário baseado no dia da semana, refinado com resultado ML.NET
        /// </summary>
        private static TimeSpan GetDayBasedTime(DayOfWeek day, TimeSpan mlSuggestedTime)
        {
            var baseTime = day switch
            {
                DayOfWeek.Saturday or DayOfWeek.Sunday => TimeSpan.FromHours(8.5), // Fins de semana
                DayOfWeek.Monday => TimeSpan.FromHours(6.5), // Segunda mais cedo
                DayOfWeek.Friday => TimeSpan.FromHours(7.5), // Sexta um pouco mais tarde
                _ => TimeSpan.FromHours(7) // Dias normais
            };

            // Ajustar baseado na sugestão ML.NET (média ponderada)
            var mlHours = mlSuggestedTime.TotalHours;
            var baseHours = baseTime.TotalHours;
            var adjustedHours = (mlHours * 0.7) + (baseHours * 0.3); // 70% ML.NET, 30% dia

            return TimeSpan.FromHours(Math.Max(0, Math.Min(24, adjustedHours)));
        }

        /// <summary>
        /// Calcula confiança baseada no dia da semana
        /// </summary>
        private static double CalculateDayConfidence(DayOfWeek day, double baseAccuracy)
        {
            var dayFactor = day switch
            {
                DayOfWeek.Saturday or DayOfWeek.Sunday => 0.9, // Fins de semana mais previsíveis
                DayOfWeek.Monday => 0.8, // Segunda tem padrão específico
                DayOfWeek.Friday => 0.7, // Sexta pode variar
                _ => 0.85 // Dias normais
            };

            return Math.Min(0.9, baseAccuracy * dayFactor);
        }

        /// <summary>
        /// Obtém descrição amigável do dia
        /// </summary>
        private static string GetDayDescription(DayOfWeek day)
        {
            return day switch
            {
                DayOfWeek.Monday => "segunda-feira",
                DayOfWeek.Tuesday => "terça-feira", 
                DayOfWeek.Wednesday => "quarta-feira",
                DayOfWeek.Thursday => "quinta-feira",
                DayOfWeek.Friday => "sexta-feira",
                DayOfWeek.Saturday => "sábado",
                DayOfWeek.Sunday => "domingo",
                _ => day.ToString()
            };
        }
    }
}
