using MediatR;

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
}
