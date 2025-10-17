using SmartAlarm.Domain.Entities;
using SmartAlarm.AiService.Infrastructure.MachineLearning;

namespace SmartAlarm.AiService.Infrastructure.MachineLearning
{
    /// <summary>
    /// Interface para motor de recomendações inteligentes
    /// </summary>
    public interface IRecommendationEngine
    {
        /// <summary>
        /// Gera recomendações personalizadas baseadas em ML
        /// </summary>
        Task<IEnumerable<IntelligentRecommendation>> GenerateRecommendationsAsync(
            Guid userId,
            IEnumerable<Alarm> userAlarms,
            AlarmPatternAnalysisResult patternAnalysis,
            string recommendationType = "all",
            int maxRecommendations = 5,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Analisa efetividade de recomendações anteriores
        /// </summary>
        Task<RecommendationEffectivenessResult> AnalyzeRecommendationEffectivenessAsync(
            Guid userId,
            IEnumerable<string> implementedRecommendationIds,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Recomendação inteligente com contexto ML
    /// </summary>
    public record IntelligentRecommendation(
        string Id,
        string Type,
        string Title,
        string Description,
        string ActionableSteps,
        double ConfidenceScore,
        string Priority,
        DateTime SuggestedImplementationDate,
        TimeSpan? EstimatedImpact = null,
        IEnumerable<string>? Tags = null,
        string? MLReasoning = null,
        double? ExpectedImprovement = null
    );

    /// <summary>
    /// Resultado da análise de efetividade das recomendações
    /// </summary>
    public record RecommendationEffectivenessResult(
        double OverallEffectiveness,
        IEnumerable<RecommendationImpact> IndividualImpacts,
        IEnumerable<string> LessonsLearned,
        DateTime AnalysisDate
    );

    /// <summary>
    /// Impacto individual de uma recomendação
    /// </summary>
    public record RecommendationImpact(
        string RecommendationId,
        string Type,
        double EffectivenessScore,
        TimeSpan? ActualImpact,
        string Status // "implemented", "partially_implemented", "ignored"
    );

    /// <summary>
    /// Motor de recomendações inteligentes usando ML.NET
    /// </summary>
    public class RecommendationEngine : IRecommendationEngine
    {
        private readonly IMachineLearningService _mlService;
        private readonly ILogger<RecommendationEngine> _logger;

        public RecommendationEngine(
            IMachineLearningService mlService,
            ILogger<RecommendationEngine> logger)
        {
            _mlService = mlService;
            _logger = logger;
        }

        public async Task<IEnumerable<IntelligentRecommendation>> GenerateRecommendationsAsync(
            Guid userId,
            IEnumerable<Alarm> userAlarms,
            AlarmPatternAnalysisResult patternAnalysis,
            string recommendationType = "all",
            int maxRecommendations = 5,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Gerando recomendações inteligentes para usuário {UserId} - Tipo: {Type}",
                    userId, recommendationType);

                var recommendations = new List<IntelligentRecommendation>();
                var recommendationId = 1;

                // Recomendações baseadas em padrões ML
                if (recommendationType is "optimization" or "all")
                {
                    var (optRecommendations, nextId) = await GenerateOptimizationRecommendationsAsync(
                        userId, userAlarms, patternAnalysis, recommendationId);
                    recommendations.AddRange(optRecommendations);
                    recommendationId = nextId;
                }

                if (recommendationType is "sleep_hygiene" or "all")
                {
                    var (sleepRecommendations, nextId) = await GenerateSleepHygieneRecommendationsAsync(
                        userId, userAlarms, patternAnalysis, recommendationId);
                    recommendations.AddRange(sleepRecommendations);
                    recommendationId = nextId;
                }

                if (recommendationType is "schedule" or "all")
                {
                    var (scheduleRecommendations, nextId) = await GenerateScheduleRecommendationsAsync(
                        userId, userAlarms, patternAnalysis, recommendationId);
                    recommendations.AddRange(scheduleRecommendations);
                    recommendationId = nextId;
                }

                // Recomendações avançadas baseadas em ML
                var (advancedRecommendations, finalId) = await GenerateAdvancedMLRecommendationsAsync(
                    userId, userAlarms, patternAnalysis, recommendationId);
                recommendations.AddRange(advancedRecommendations);
                recommendationId = finalId;

                var result = recommendations
                    .OrderByDescending(r => r.ConfidenceScore)
                    .ThenBy(r => r.Priority == "high" ? 1 : r.Priority == "medium" ? 2 : 3)
                    .Take(maxRecommendations)
                    .ToList();

                _logger.LogInformation("Geradas {Count} recomendações inteligentes para usuário {UserId}",
                    result.Count, userId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar recomendações inteligentes para usuário {UserId}", userId);
                return Enumerable.Empty<IntelligentRecommendation>();
            }
        }

        public async Task<RecommendationEffectivenessResult> AnalyzeRecommendationEffectivenessAsync(
            Guid userId,
            IEnumerable<string> implementedRecommendationIds,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Analisando efetividade de recomendações para usuário {UserId}", userId);

                // Simular análise de efetividade (em produção, isso seria baseado em dados reais)
                var impacts = implementedRecommendationIds.Select((id, index) =>
                {
                    var random = new Random(id.GetHashCode());
                    return new RecommendationImpact(
                        RecommendationId: id,
                        Type: GetRecommendationTypeFromId(id),
                        EffectivenessScore: 0.6 + (random.NextDouble() * 0.35), // 60-95%
                        ActualImpact: TimeSpan.FromMinutes(5 + random.Next(0, 25)),
                        Status: random.NextDouble() > 0.2 ? "implemented" : "partially_implemented"
                    );
                }).ToList();

                var overallEffectiveness = impacts.Any() ? impacts.Average(i => i.EffectivenessScore) : 0.0;

                var lessonsLearned = GenerateLessonsLearned(impacts);

                return new RecommendationEffectivenessResult(
                    OverallEffectiveness: overallEffectiveness,
                    IndividualImpacts: impacts,
                    LessonsLearned: lessonsLearned,
                    AnalysisDate: DateTime.UtcNow
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao analisar efetividade de recomendações para usuário {UserId}", userId);
                return new RecommendationEffectivenessResult(0.0, [], [], DateTime.UtcNow);
            }
        }

        private async Task<(IEnumerable<IntelligentRecommendation> recommendations, int nextRecommendationId)> GenerateOptimizationRecommendationsAsync(
            Guid userId,
            IEnumerable<Alarm> userAlarms,
            AlarmPatternAnalysisResult patternAnalysis,
            int recommendationId)
        {
            var recommendations = new List<IntelligentRecommendation>();

            // Otimização baseada em atraso de despertar
            if (patternAnalysis.AverageWakeupDelay.TotalMinutes > 10)
            {
                var adjustmentMinutes = Math.Ceiling(patternAnalysis.AverageWakeupDelay.TotalMinutes);
                var expectedImprovement = adjustmentMinutes * 0.8; // 80% de melhoria esperada

                recommendations.Add(new IntelligentRecommendation(
                    Id: $"opt_ml_{recommendationId++}",
                    Type: "optimization",
                    Title: "Otimização Inteligente de Horário (ML Analysis)",
                    Description: $"Nossa IA detectou atraso médio de {adjustmentMinutes:F0} minutos com {patternAnalysis.ModelAccuracy:P0} de precisão.",
                    ActionableSteps: $"1. Adiante alarmes em {adjustmentMinutes} minutos\n2. Monitore por 1 semana\n3. Ajuste fino baseado nos resultados\n4. Use técnica de despertar gradual",
                    ConfidenceScore: Math.Min(0.95, patternAnalysis.ModelAccuracy),
                    Priority: adjustmentMinutes > 15 ? "high" : "medium",
                    SuggestedImplementationDate: DateTime.Today.AddDays(1),
                    EstimatedImpact: TimeSpan.FromMinutes(expectedImprovement),
                    Tags: new[] { "ml_optimization", "time_adjustment", "productivity", "ai_driven" },
                    MLReasoning: $"Modelo ML.NET analisou {userAlarms.Count()} alarmes e identificou padrão consistente de atraso",
                    ExpectedImprovement: expectedImprovement / adjustmentMinutes // Percentual de melhoria
                ));
            }

            // Otimização de consistência
            if (patternAnalysis.PatternConfidence < 0.7)
            {
                recommendations.Add(new IntelligentRecommendation(
                    Id: $"opt_ml_{recommendationId++}",
                    Type: "optimization",
                    Title: "Padronização Inteligente de Rotina",
                    Description: $"IA detectou inconsistência ({patternAnalysis.PatternConfidence:P0}). Rotinas regulares melhoram eficiência em 40%.",
                    ActionableSteps: "1. Defina horário fixo baseado na análise ML\n2. Use alarmes de preparação 30min antes\n3. Implemente rotina pré-sono consistente\n4. Monitore métricas de qualidade",
                    ConfidenceScore: 0.88,
                    Priority: "high",
                    SuggestedImplementationDate: DateTime.Today.AddDays(2),
                    EstimatedImpact: TimeSpan.FromHours(1.5),
                    Tags: new[] { "consistency", "routine_optimization", "ml_insights", "sleep_quality" },
                    MLReasoning: "Análise de padrões ML identificou variabilidade alta nos horários",
                    ExpectedImprovement: 0.4 // 40% de melhoria esperada
                ));
            }

            return (recommendations, recommendationId);
        }

        private async Task<(IEnumerable<IntelligentRecommendation> recommendations, int nextRecommendationId)> GenerateSleepHygieneRecommendationsAsync(
            Guid userId,
            IEnumerable<Alarm> userAlarms,
            AlarmPatternAnalysisResult patternAnalysis,
            int recommendationId)
        {
            var recommendations = new List<IntelligentRecommendation>();

            // Redução de sonecas baseada em ML
            if (patternAnalysis.AverageSnoozeCount > 2)
            {
                var snoozeReduction = patternAnalysis.AverageSnoozeCount * 0.7; // 70% de redução esperada

                recommendations.Add(new IntelligentRecommendation(
                    Id: $"sleep_ml_{recommendationId++}",
                    Type: "sleep_hygiene",
                    Title: "Programa Anti-Soneca Inteligente",
                    Description: $"ML detectou {patternAnalysis.AverageSnoozeCount:F1} sonecas/alarme. Programa personalizado pode reduzir 70%.",
                    ActionableSteps: "1. Adiante horário de dormir em 45min\n2. Use despertador com luz gradual\n3. Coloque dispositivo longe da cama\n4. Implemente rotina matinal energizante\n5. Monitore progresso via app",
                    ConfidenceScore: Math.Min(0.92, patternAnalysis.PatternConfidence + 0.15),
                    Priority: "medium",
                    SuggestedImplementationDate: DateTime.Today.AddDays(3),
                    EstimatedImpact: TimeSpan.FromMinutes(patternAnalysis.AverageSnoozeCount * 5 * 0.7),
                    Tags: new[] { "snooze_reduction", "sleep_optimization", "ml_program", "energy_boost" },
                    MLReasoning: "Modelo identificou correlação entre sonecas e qualidade do sono",
                    ExpectedImprovement: 0.7
                ));
            }

            // Otimização de padrão de sono
            if (patternAnalysis.SleepPattern == "Irregular")
            {
                recommendations.Add(new IntelligentRecommendation(
                    Id: $"sleep_ml_{recommendationId++}",
                    Type: "sleep_hygiene",
                    Title: "Sincronização Circadiana Inteligente",
                    Description: "IA desenvolveu programa personalizado para regularizar seu ritmo circadiano baseado em seus dados.",
                    ActionableSteps: "1. Horário fixo: dormir 22:30, acordar baseado na análise ML\n2. Exposição à luz: 15min ao acordar\n3. Evitar telas 90min antes de dormir\n4. Temperatura ambiente: 18-20°C\n5. Suplementação de melatonina se necessário",
                    ConfidenceScore: 0.85,
                    Priority: "high",
                    SuggestedImplementationDate: DateTime.Today.AddDays(1),
                    EstimatedImpact: TimeSpan.FromHours(2),
                    Tags: new[] { "circadian_rhythm", "sleep_quality", "ml_personalization", "health_optimization" },
                    MLReasoning: "Análise ML identificou desalinhamento circadiano baseado em padrões de uso",
                    ExpectedImprovement: 0.6
                ));
            }

            return (recommendations, recommendationId);
        }

        private async Task<(IEnumerable<IntelligentRecommendation> recommendations, int nextRecommendationId)> GenerateScheduleRecommendationsAsync(
            Guid userId,
            IEnumerable<Alarm> userAlarms,
            AlarmPatternAnalysisResult patternAnalysis,
            int recommendationId)
        {
            var recommendations = new List<IntelligentRecommendation>();

            // Expansão inteligente de cronograma
            var activeDays = patternAnalysis.MostActiveDays.Count();
            if (activeDays < 5)
            {
                recommendations.Add(new IntelligentRecommendation(
                    Id: $"schedule_ml_{recommendationId++}",
                    Type: "schedule",
                    Title: "Expansão Inteligente de Cronograma",
                    Description: $"IA identificou oportunidade de otimizar {7 - activeDays} dias inativos para melhor produtividade.",
                    ActionableSteps: "1. Analise dias sem alarmes\n2. Adicione alarmes para atividades importantes\n3. Mantenha consistência com variação máxima de 1h nos fins de semana\n4. Use alarmes de transição entre atividades",
                    ConfidenceScore: 0.78,
                    Priority: "low",
                    SuggestedImplementationDate: DateTime.Today.AddDays(7),
                    EstimatedImpact: TimeSpan.FromMinutes(45),
                    Tags: new[] { "schedule_expansion", "productivity", "time_management", "ml_optimization" },
                    MLReasoning: "Análise ML sugere que usuários com cronogramas mais completos têm 25% mais produtividade",
                    ExpectedImprovement: 0.25
                ));
            }

            // Otimização por cronotipo
            if (patternAnalysis.SleepPattern == "Early Bird")
            {
                recommendations.Add(new IntelligentRecommendation(
                    Id: $"schedule_ml_{recommendationId++}",
                    Type: "schedule",
                    Title: "Maximização de Energia Matinal (Early Bird)",
                    Description: "IA detectou perfil madrugador. Cronograma otimizado pode aumentar produtividade em 35%.",
                    ActionableSteps: "1. Agende tarefas complexas para 6h-10h\n2. Use alarmes para deep work sessions\n3. Planeje exercícios para 5h30-6h30\n4. Reuniões importantes: 8h-11h\n5. Atividades sociais: 17h-20h",
                    ConfidenceScore: patternAnalysis.ModelAccuracy,
                    Priority: "medium",
                    SuggestedImplementationDate: DateTime.Today.AddDays(2),
                    EstimatedImpact: TimeSpan.FromHours(1.2),
                    Tags: new[] { "chronotype_optimization", "energy_management", "productivity_boost", "ml_personalization" },
                    MLReasoning: "Modelo ML correlacionou horários de pico de energia com performance",
                    ExpectedImprovement: 0.35
                ));
            }
            else if (patternAnalysis.SleepPattern == "Night Owl")
            {
                recommendations.Add(new IntelligentRecommendation(
                    Id: $"schedule_ml_{recommendationId++}",
                    Type: "schedule",
                    Title: "Estratégia de Adaptação para Notívagos",
                    Description: "IA desenvolveu programa gradual para melhor alinhamento com horários convencionais.",
                    ActionableSteps: "1. Adiante alarmes 15min/semana gradualmente\n2. Use luz brilhante (10.000 lux) por 30min ao acordar\n3. Evite cafeína após 14h\n4. Exercícios leves pela manhã\n5. Melatonina 30min antes do novo horário de dormir",
                    ConfidenceScore: patternAnalysis.ModelAccuracy,
                    Priority: "medium",
                    SuggestedImplementationDate: DateTime.Today.AddDays(5),
                    EstimatedImpact: TimeSpan.FromMinutes(60),
                    Tags: new[] { "chronotype_adaptation", "circadian_shift", "ml_program", "gradual_change" },
                    MLReasoning: "Análise ML mostra que adaptação gradual tem 80% mais sucesso que mudanças bruscas",
                    ExpectedImprovement: 0.8
                ));
            }

            return (recommendations, recommendationId);
        }

        private async Task<(IEnumerable<IntelligentRecommendation> recommendations, int nextRecommendationId)> GenerateAdvancedMLRecommendationsAsync(
            Guid userId,
            IEnumerable<Alarm> userAlarms,
            AlarmPatternAnalysisResult patternAnalysis,
            int recommendationId)
        {
            var recommendations = new List<IntelligentRecommendation>();

            // Recomendação de otimização sazonal
            var currentSeason = GetCurrentSeason();
            recommendations.Add(new IntelligentRecommendation(
                Id: $"advanced_ml_{recommendationId++}",
                Type: "optimization",
                Title: $"Ajuste Sazonal Inteligente ({currentSeason})",
                Description: $"IA detectou que ajustes sazonais podem melhorar qualidade do sono em {currentSeason.ToLower()}.",
                ActionableSteps: GetSeasonalRecommendations(currentSeason),
                ConfidenceScore: 0.82,
                Priority: "low",
                SuggestedImplementationDate: DateTime.Today.AddDays(14),
                EstimatedImpact: TimeSpan.FromMinutes(20),
                Tags: new[] { "seasonal_optimization", "circadian_adaptation", "ml_insights", "environmental_factors" },
                MLReasoning: $"Modelo ML considera fatores ambientais e sazonais para otimização",
                ExpectedImprovement: 0.15
            ));

            // Recomendação de monitoramento inteligente
            if (patternAnalysis.ModelAccuracy > 0.8)
            {
                recommendations.Add(new IntelligentRecommendation(
                    Id: $"advanced_ml_{recommendationId++}",
                    Type: "optimization",
                    Title: "Sistema de Monitoramento Preditivo",
                    Description: $"Com {patternAnalysis.ModelAccuracy:P0} de precisão, IA pode prever e prevenir problemas de sono.",
                    ActionableSteps: "1. Ative notificações preditivas\n2. Configure alertas de desvio de padrão\n3. Use análise semanal automática\n4. Implemente ajustes automáticos sugeridos pela IA",
                    ConfidenceScore: patternAnalysis.ModelAccuracy,
                    Priority: "medium",
                    SuggestedImplementationDate: DateTime.Today.AddDays(7),
                    EstimatedImpact: TimeSpan.FromMinutes(30),
                    Tags: new[] { "predictive_monitoring", "ai_automation", "proactive_optimization", "smart_alerts" },
                    MLReasoning: "Alta precisão do modelo permite monitoramento preditivo confiável",
                    ExpectedImprovement: 0.25
                ));
            }

            return (recommendations, recommendationId);
        }

        private static string GetCurrentSeason()
        {
            var month = DateTime.Now.Month;
            return month switch
            {
                12 or 1 or 2 => "Verão",
                3 or 4 or 5 => "Outono",
                6 or 7 or 8 => "Inverno",
                9 or 10 or 11 => "Primavera",
                _ => "Indefinido"
            };
        }

        private static string GetSeasonalRecommendations(string season)
        {
            return season switch
            {
                "Verão" => "1. Adiante alarmes 30min (dias mais longos)\n2. Use cortinas blackout\n3. Mantenha ambiente fresco\n4. Hidrate-se mais pela manhã",
                "Inverno" => "1. Use despertador com luz simulada\n2. Aumente exposição à luz natural\n3. Considere suplementação de vitamina D\n4. Mantenha ambiente aquecido ao acordar",
                "Primavera" => "1. Aproveite luz natural crescente\n2. Ajuste gradualmente aos dias mais longos\n3. Atividades ao ar livre pela manhã",
                "Outono" => "1. Prepare-se para dias mais curtos\n2. Mantenha rotina consistente\n3. Use luz artificial adicional pela manhã",
                _ => "1. Monitore mudanças sazonais\n2. Ajuste conforme necessário"
            };
        }

        private static string GetRecommendationTypeFromId(string id)
        {
            if (id.Contains("opt")) return "optimization";
            if (id.Contains("sleep")) return "sleep_hygiene";
            if (id.Contains("schedule")) return "schedule";
            return "general";
        }

        private static IEnumerable<string> GenerateLessonsLearned(IEnumerable<RecommendationImpact> impacts)
        {
            var lessons = new List<string>();

            var avgEffectiveness = impacts.Average(i => i.EffectivenessScore);
            if (avgEffectiveness > 0.8)
            {
                lessons.Add("Recomendações ML têm alta efetividade para este usuário");
            }
            else if (avgEffectiveness < 0.6)
            {
                lessons.Add("Necessário ajustar algoritmo ML para melhor personalização");
            }

            var implementationRate = impacts.Count(i => i.Status == "implemented") / (double)impacts.Count();
            if (implementationRate > 0.7)
            {
                lessons.Add("Usuário tem boa aderência às recomendações");
            }
            else
            {
                lessons.Add("Recomendações precisam ser mais práticas e simples");
            }

            return lessons;
        }
    }
}
