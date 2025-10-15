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
    /// Command para gerar recomendações personalizadas usando IA
    /// </summary>
    public record GenerateRecommendationsCommand(
        Guid UserId,
        string RecommendationType = "all", // "schedule", "sleep_hygiene", "optimization", "all"
        int MaxRecommendations = 5
    ) : IRequest<GenerateRecommendationsResponse>;

    /// <summary>
    /// Response das recomendações geradas
    /// </summary>
    public record GenerateRecommendationsResponse(
        Guid UserId,
        IEnumerable<AIRecommendation> Recommendations,
        RecommendationMetrics Metrics,
        DateTime GeneratedAt
    );

    /// <summary>
    /// Recomendação gerada pela IA
    /// </summary>
    public record AIRecommendation(
        string Id,
        string Type,
        string Title,
        string Description,
        string ActionableSteps,
        double ConfidenceScore,
        string Priority, // "high", "medium", "low"
        DateTime SuggestedImplementationDate,
        TimeSpan? EstimatedImpact = null,
        IEnumerable<string>? Tags = null
    );

    /// <summary>
    /// Métricas das recomendações
    /// </summary>
    public record RecommendationMetrics(
        int TotalRecommendations,
        double AverageConfidence,
        string ModelVersion,
        IEnumerable<string> DataSourcesUsed
    );

    /// <summary>
    /// Validator para comando de geração de recomendações
    /// </summary>
    public class GenerateRecommendationsCommandValidator : AbstractValidator<GenerateRecommendationsCommand>
    {
        public GenerateRecommendationsCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("UserId é obrigatório");

            RuleFor(x => x.RecommendationType)
                .Must(type => new[] { "schedule", "sleep_hygiene", "optimization", "all" }.Contains(type))
                .WithMessage("RecommendationType deve ser: schedule, sleep_hygiene, optimization ou all");

            RuleFor(x => x.MaxRecommendations)
                .InclusiveBetween(1, 20)
                .WithMessage("MaxRecommendations deve estar entre 1 e 20");
        }
    }

    /// <summary>
    /// Handler para geração de recomendações personalizadas usando IA
    /// </summary>
    public class GenerateRecommendationsCommandHandler : IRequestHandler<GenerateRecommendationsCommand, GenerateRecommendationsResponse>
    {
        private readonly IAlarmRepository _alarmRepository;
        private readonly IUserRepository _userRepository;
        private readonly IValidator<GenerateRecommendationsCommand> _validator;
        private readonly IMachineLearningService _machineLearningService;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly ILogger<GenerateRecommendationsCommandHandler> _logger;

        public GenerateRecommendationsCommandHandler(
            IAlarmRepository alarmRepository,
            IUserRepository userRepository,
            IValidator<GenerateRecommendationsCommand> validator,
            IMachineLearningService machineLearningService,
            SmartAlarmActivitySource activitySource,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            ILogger<GenerateRecommendationsCommandHandler> logger)
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

        public async Task<GenerateRecommendationsResponse> Handle(GenerateRecommendationsCommand request, CancellationToken cancellationToken)
        {
            using var activity = _activitySource.StartActivity("GenerateRecommendationsCommandHandler.Handle");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Activity tags
                activity?.SetTag("user.id", request.UserId.ToString());
                activity?.SetTag("operation", "generate_recommendations");
                activity?.SetTag("recommendation.type", request.RecommendationType);
                activity?.SetTag("recommendation.max_count", request.MaxRecommendations.ToString());
                activity?.SetTag("correlation.id", _correlationContext.CorrelationId);

                _logger.LogInformation("Iniciando geração de recomendações para usuário {UserId} - Tipo: {Type}, Max: {Max} - CorrelationId: {CorrelationId}",
                    request.UserId, request.RecommendationType, request.MaxRecommendations, _correlationContext.CorrelationId);

                // Validação
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    activity?.SetTag("validation.failed", true);
                    _meter.IncrementErrorCount("command", "generate_recommendations", "validation");

                    var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning("Validação falhou para geração de recomendações: {Errors} - CorrelationId: {CorrelationId}",
                        errors, _correlationContext.CorrelationId);

                    throw new ValidationException($"Dados inválidos: {errors}");
                }

                // Verificar se usuário existe
                var user = await _userRepository.GetByIdAsync(request.UserId);
                if (user == null)
                {
                    activity?.SetTag("user.found", false);
                    _meter.IncrementErrorCount("command", "generate_recommendations", "user_not_found");

                    _logger.LogWarning("Usuário {UserId} não encontrado para geração de recomendações - CorrelationId: {CorrelationId}",
                        request.UserId, _correlationContext.CorrelationId);

                    throw new InvalidOperationException($"Usuário {request.UserId} não encontrado");
                }

                activity?.SetTag("user.found", true);
                activity?.SetTag("user.email", user.Email.Address);

                // Buscar alarmes do usuário
                var alarms = await _alarmRepository.GetByUserIdAsync(request.UserId);
                var activeAlarms = alarms.Where(a => a.Enabled).ToList();

                if (!activeAlarms.Any())
                {
                    activity?.SetTag("alarms.found", false);
                    _logger.LogWarning("Nenhum alarme ativo encontrado para gerar recomendações - UserId: {UserId} - CorrelationId: {CorrelationId}",
                        request.UserId, _correlationContext.CorrelationId);

                    // Retornar recomendações básicas para usuários sem alarmes
                    var basicRecommendations = GenerateBasicRecommendations(request.UserId, request.RecommendationType);
                    return CreateResponse(request.UserId, basicRecommendations, "basic");
                }

                activity?.SetTag("alarms.count", activeAlarms.Count.ToString());

                // Analisar padrões usando ML.NET
                var analysisResult = await _machineLearningService.AnalyzeAlarmPatternsAsync(
                    activeAlarms, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, cancellationToken);

                // Gerar recomendações baseadas na análise ML.NET
                var recommendations = GenerateMLBasedRecommendations(
                    request.UserId,
                    activeAlarms,
                    analysisResult,
                    request.RecommendationType,
                    request.MaxRecommendations);

                activity?.SetTag("recommendations.count", recommendations.Count().ToString());
                activity?.SetTag("ml.model_accuracy", analysisResult.ModelAccuracy.ToString("F2"));

                stopwatch.Stop();

                // Métricas de sucesso
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "generate_recommendations", "success", "200");

                // Log de sucesso
                _logger.LogInformation("Recomendações geradas para usuário {UserId} - Count: {Count}, Tipo: {Type} - Duração: {Duration}ms - CorrelationId: {CorrelationId}",
                    request.UserId, recommendations.Count(), request.RecommendationType, stopwatch.ElapsedMilliseconds, _correlationContext.CorrelationId);

                return CreateResponse(request.UserId, recommendations, "ml_based");
            }
            catch (ValidationException)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "generate_recommendations", "validation_error", "400");
                throw;
            }
            catch (InvalidOperationException)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "generate_recommendations", "business_error", "409");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "generate_recommendations", "error", "500");
                _meter.IncrementErrorCount("command", "generate_recommendations", "exception");

                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);

                _logger.LogError(ex, "Erro inesperado na geração de recomendações para usuário {UserId} - CorrelationId: {CorrelationId}",
                    request.UserId, _correlationContext.CorrelationId);

                throw;
            }
        }

        /// <summary>
        /// Gera recomendações baseadas na análise ML.NET
        /// </summary>
        private static IEnumerable<AIRecommendation> GenerateMLBasedRecommendations(
            Guid userId,
            IEnumerable<Alarm> alarms,
            AlarmPatternAnalysisResult analysisResult,
            string recommendationType,
            int maxRecommendations)
        {
            var recommendations = new List<AIRecommendation>();
            var recommendationId = 1;

            // Recomendações de otimização de horário
            if (recommendationType is "optimization" or "all")
            {
                if (analysisResult.AverageWakeupDelay.TotalMinutes > 10)
                {
                    recommendations.Add(new AIRecommendation(
                        Id: $"opt_{recommendationId++}",
                        Type: "optimization",
                        Title: "Otimização de Horário Baseada em ML",
                        Description: $"Nossa análise ML.NET detectou um atraso médio de {analysisResult.AverageWakeupDelay.TotalMinutes:F0} minutos. Recomendamos ajustar seus alarmes.",
                        ActionableSteps: $"1. Adiante seus alarmes em {Math.Ceiling(analysisResult.AverageWakeupDelay.TotalMinutes)} minutos\n2. Mantenha consistência por 1 semana\n3. Monitore os resultados",
                        ConfidenceScore: Math.Min(0.95, analysisResult.ModelAccuracy),
                        Priority: analysisResult.AverageWakeupDelay.TotalMinutes > 15 ? "high" : "medium",
                        SuggestedImplementationDate: DateTime.Today.AddDays(1),
                        EstimatedImpact: TimeSpan.FromMinutes(analysisResult.AverageWakeupDelay.TotalMinutes * 0.7),
                        Tags: new[] { "ml_analysis", "time_optimization", "productivity" }
                    ));
                }

                if (analysisResult.PatternConfidence < 0.7)
                {
                    recommendations.Add(new AIRecommendation(
                        Id: $"opt_{recommendationId++}",
                        Type: "optimization",
                        Title: "Padronização de Rotina (ML Insight)",
                        Description: $"Análise ML.NET mostra baixa consistência ({analysisResult.PatternConfidence:P0}). Rotinas mais regulares melhoram a qualidade do sono.",
                        ActionableSteps: "1. Escolha um horário fixo para dormir\n2. Mantenha o mesmo horário de alarme nos dias úteis\n3. Limite variações a ±30 minutos nos fins de semana",
                        ConfidenceScore: 0.85,
                        Priority: "high",
                        SuggestedImplementationDate: DateTime.Today.AddDays(2),
                        EstimatedImpact: TimeSpan.FromHours(1), // Melhoria estimada na qualidade do sono
                        Tags: new[] { "consistency", "sleep_quality", "routine" }
                    ));
                }
            }

            // Recomendações de higiene do sono
            if (recommendationType is "sleep_hygiene" or "all")
            {
                if (analysisResult.AverageSnoozeCount > 2)
                {
                    recommendations.Add(new AIRecommendation(
                        Id: $"sleep_{recommendationId++}",
                        Type: "sleep_hygiene",
                        Title: "Redução de Sonecas (ML Analysis)",
                        Description: $"Detectamos média de {analysisResult.AverageSnoozeCount:F1} sonecas por alarme. Isso pode indicar sono insuficiente.",
                        ActionableSteps: "1. Avance seu horário de dormir em 30 minutos\n2. Coloque o despertador longe da cama\n3. Use luz natural ao acordar\n4. Evite telas 1h antes de dormir",
                        ConfidenceScore: Math.Min(0.9, analysisResult.PatternConfidence + 0.1),
                        Priority: "medium",
                        SuggestedImplementationDate: DateTime.Today.AddDays(3),
                        EstimatedImpact: TimeSpan.FromMinutes(analysisResult.AverageSnoozeCount * 5),
                        Tags: new[] { "snooze_reduction", "sleep_hygiene", "energy" }
                    ));
                }

                if (analysisResult.SleepPattern == "Irregular")
                {
                    recommendations.Add(new AIRecommendation(
                        Id: $"sleep_{recommendationId++}",
                        Type: "sleep_hygiene",
                        Title: "Regularização do Padrão de Sono",
                        Description: "ML.NET identificou padrão irregular. Um cronograma consistente melhora significativamente a qualidade do sono.",
                        ActionableSteps: "1. Defina um horário fixo para dormir e acordar\n2. Use a regra 3-2-1: 3h sem comida, 2h sem trabalho, 1h sem telas\n3. Crie uma rotina relaxante pré-sono",
                        ConfidenceScore: 0.88,
                        Priority: "high",
                        SuggestedImplementationDate: DateTime.Today.AddDays(1),
                        EstimatedImpact: TimeSpan.FromHours(2), // Melhoria na qualidade do sono
                        Tags: new[] { "sleep_schedule", "circadian_rhythm", "health" }
                    ));
                }
            }

            // Recomendações de cronograma
            if (recommendationType is "schedule" or "all")
            {
                var mostActiveDays = analysisResult.MostActiveDays.ToList();
                if (mostActiveDays.Count < 5) // Menos de 5 dias ativos
                {
                    recommendations.Add(new AIRecommendation(
                        Id: $"schedule_{recommendationId++}",
                        Type: "schedule",
                        Title: "Expansão de Cronograma",
                        Description: $"Você tem alarmes ativos apenas em {mostActiveDays.Count} dias. Considere expandir para uma rotina mais completa.",
                        ActionableSteps: "1. Identifique dias sem alarmes\n2. Adicione alarmes para atividades importantes\n3. Mantenha consistência mesmo nos fins de semana",
                        ConfidenceScore: 0.75,
                        Priority: "low",
                        SuggestedImplementationDate: DateTime.Today.AddDays(7),
                        EstimatedImpact: TimeSpan.FromMinutes(30), // Tempo economizado com melhor organização
                        Tags: new[] { "schedule_expansion", "productivity", "organization" }
                    ));
                }

                // Recomendação baseada no tipo de pessoa (Early Bird/Night Owl)
                if (analysisResult.SleepPattern == "Early Bird")
                {
                    recommendations.Add(new AIRecommendation(
                        Id: $"schedule_{recommendationId++}",
                        Type: "schedule",
                        Title: "Otimização para Madrugadores",
                        Description: "Perfil Early Bird detectado. Aproveite sua energia matinal para tarefas importantes.",
                        ActionableSteps: "1. Agende tarefas complexas para manhã\n2. Use alarmes para pausas regulares\n3. Planeje atividades sociais para início da noite",
                        ConfidenceScore: analysisResult.ModelAccuracy,
                        Priority: "medium",
                        SuggestedImplementationDate: DateTime.Today.AddDays(2),
                        EstimatedImpact: TimeSpan.FromHours(1), // Melhoria na produtividade
                        Tags: new[] { "chronotype", "productivity", "energy_management" }
                    ));
                }
                else if (analysisResult.SleepPattern == "Night Owl")
                {
                    recommendations.Add(new AIRecommendation(
                        Id: $"schedule_{recommendationId++}",
                        Type: "schedule",
                        Title: "Estratégias para Notívagos",
                        Description: "Perfil Night Owl identificado. Ajuste gradual pode melhorar alinhamento com horários convencionais.",
                        ActionableSteps: "1. Adiante alarmes gradualmente (15 min/semana)\n2. Use luz brilhante pela manhã\n3. Evite cafeína após 14h",
                        ConfidenceScore: analysisResult.ModelAccuracy,
                        Priority: "medium",
                        SuggestedImplementationDate: DateTime.Today.AddDays(5),
                        EstimatedImpact: TimeSpan.FromMinutes(45), // Melhoria na adaptação
                        Tags: new[] { "chronotype", "adaptation", "circadian_shift" }
                    ));
                }
            }

            return recommendations
                .OrderByDescending(r => r.ConfidenceScore)
                .ThenBy(r => r.Priority == "high" ? 1 : r.Priority == "medium" ? 2 : 3)
                .Take(maxRecommendations);
        }

        /// <summary>
        /// Gera recomendações básicas para usuários sem alarmes
        /// </summary>
        private static IEnumerable<AIRecommendation> GenerateBasicRecommendations(Guid userId, string recommendationType)
        {
            var recommendations = new List<AIRecommendation>();

            if (recommendationType is "schedule" or "all")
            {
                recommendations.Add(new AIRecommendation(
                    Id: "basic_1",
                    Type: "schedule",
                    Title: "Criação de Primeiro Alarme",
                    Description: "Comece sua jornada no Smart Alarm criando seu primeiro alarme personalizado.",
                    ActionableSteps: "1. Clique em 'Novo Alarme'\n2. Defina um horário consistente\n3. Escolha os dias da semana\n4. Adicione uma descrição motivacional",
                    ConfidenceScore: 0.95,
                    Priority: "high",
                    SuggestedImplementationDate: DateTime.Today,
                    Tags: new[] { "getting_started", "first_alarm" }
                ));
            }

            if (recommendationType is "sleep_hygiene" or "all")
            {
                recommendations.Add(new AIRecommendation(
                    Id: "basic_2",
                    Type: "sleep_hygiene",
                    Title: "Estabelecimento de Rotina",
                    Description: "Uma rotina consistente é a base para um sono de qualidade e dias produtivos.",
                    ActionableSteps: "1. Defina um horário fixo para dormir\n2. Crie alarmes para atividades regulares\n3. Mantenha consistência por pelo menos 2 semanas",
                    ConfidenceScore: 0.90,
                    Priority: "medium",
                    SuggestedImplementationDate: DateTime.Today.AddDays(1),
                    Tags: new[] { "routine", "consistency", "sleep_hygiene" }
                ));
            }

            return recommendations;
        }

        /// <summary>
        /// Cria a response com métricas
        /// </summary>
        private static GenerateRecommendationsResponse CreateResponse(
            Guid userId,
            IEnumerable<AIRecommendation> recommendations,
            string dataSource)
        {
            var recommendationsList = recommendations.ToList();
            var avgConfidence = recommendationsList.Any() ? recommendationsList.Average(r => r.ConfidenceScore) : 0.0;

            var metrics = new RecommendationMetrics(
                TotalRecommendations: recommendationsList.Count,
                AverageConfidence: avgConfidence,
                ModelVersion: "SmartAlarm ML.NET v2.0.0",
                DataSourcesUsed: new[] { dataSource, "user_patterns", "ml_analysis" }
            );

            return new GenerateRecommendationsResponse(
                userId,
                recommendationsList,
                metrics,
                DateTime.UtcNow
            );
        }
    }
}
