using MediatR;

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
}
