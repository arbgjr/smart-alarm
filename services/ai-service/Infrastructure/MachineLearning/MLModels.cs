using Microsoft.ML;
using Microsoft.ML.Data;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.AiService.Infrastructure.MachineLearning
{
    /// <summary>
    /// Dados de entrada para treinamento do modelo de análise de padrões de alarmes
    /// </summary>
    public class AlarmPatternData
    {
        [LoadColumn(0)]
        public float HourOfDay { get; set; }

        [LoadColumn(1)]
        public float DayOfWeek { get; set; }

        [LoadColumn(2)]
        public float WeekOfYear { get; set; }

        [LoadColumn(3)]
        public float IsEnabled { get; set; }

        [LoadColumn(4)]
        public float SnoozeCount { get; set; }

        [LoadColumn(5)]
        public float WakeupDelayMinutes { get; set; }

        [LoadColumn(6)]
        public float AlarmDuration { get; set; }
    }

    /// <summary>
    /// Predição do modelo de padrões de alarmes
    /// </summary>
    public class AlarmPatternPrediction
    {
        [ColumnName("PredictedLabel")]
        public float PredictedOptimalHour { get; set; }

        [ColumnName("Score")]
        public float[] Score { get; set; } = Array.Empty<float>();
    }

    /// <summary>
    /// Dados de entrada para predição de horários ótimos
    /// </summary>
    public class OptimalTimePredictionData
    {
        [LoadColumn(0)]
        public float CurrentHour { get; set; }

        [LoadColumn(1)]
        public float TargetDayOfWeek { get; set; }

        [LoadColumn(2)]
        public float HistoricalAverageHour { get; set; }

        [LoadColumn(3)]
        public float ContextScore { get; set; }

        [LoadColumn(4)]
        public float UserSleepPatternScore { get; set; }

        [LoadColumn(5)]
        public float SeasonalFactor { get; set; }
    }

    /// <summary>
    /// Predição de horário ótimo
    /// </summary>
    public class OptimalTimePrediction
    {
        [ColumnName("PredictedLabel")]
        public float PredictedOptimalHour { get; set; }

        [ColumnName("Score")]
        public float ConfidenceScore { get; set; }
    }
}
