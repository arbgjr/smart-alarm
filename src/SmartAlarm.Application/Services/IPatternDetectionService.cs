using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Application.Services
{
    /// <summary>
    /// Serviço para detecção de padrões comportamentais dos usuários
    /// </summary>
    public interface IPatternDetectionService
    {
        /// <summary>
        /// Detecta padrões de rotina de um usuário
        /// </summary>
        Task<List<RoutinePattern>> DetectUserRoutinePatternsAsync(
            Guid userId, 
            int analysisWindowDays = 30, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Detecta padrões de desativação de alarmes
        /// </summary>
        Task<List<DisablePattern>> DetectDisablePatternsAsync(
            Guid userId, 
            int analysisWindowDays = 30, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Detecta padrões de soneca
        /// </summary>
        Task<List<SnoozePattern>> DetectSnoozePatternsAsync(
            Guid userId, 
            int analysisWindowDays = 30, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Detecta anomalias no comportamento do usuário
        /// </summary>
        Task<List<BehaviorAnomaly>> DetectBehaviorAnomaliesAsync(
            Guid userId, 
            int analysisWindowDays = 30, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Calcula score de consistência do usuário
        /// </summary>
        Task<double> CalculateConsistencyScoreAsync(
            Guid userId, 
            int analysisWindowDays = 30, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Identifica horários ótimos para alarmes baseado nos padrões
        /// </summary>
        Task<List<OptimalTimeSlot>> SuggestOptimalTimeSlotsAsync(
            Guid userId, 
            DayOfWeek dayOfWeek, 
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Representa um padrão de rotina detectado
    /// </summary>
    public class RoutinePattern
    {
        public Guid UserId { get; set; }
        public PatternType Type { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public TimeOnly AverageTime { get; set; }
        public TimeSpan TimeVariation { get; set; }
        public int Frequency { get; set; }
        public double Confidence { get; set; }
        public DateTime FirstObserved { get; set; }
        public DateTime LastObserved { get; set; }
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Representa um padrão de desativação de alarmes
    /// </summary>
    public class DisablePattern
    {
        public Guid UserId { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public TimeOnly TimeRange { get; set; }
        public int ConsecutiveDisables { get; set; }
        public double DisableRate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public DateTime DetectedAt { get; set; }
    }

    /// <summary>
    /// Representa um padrão de soneca
    /// </summary>
    public class SnoozePattern
    {
        public Guid UserId { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public int AverageSnoozeMinutes { get; set; }
        public int AverageSnoozeCount { get; set; }
        public double SnoozeRate { get; set; }
        public List<int> CommonSnoozeDurations { get; set; } = new();
        public double Confidence { get; set; }
        public DateTime DetectedAt { get; set; }
    }

    /// <summary>
    /// Representa uma anomalia comportamental
    /// </summary>
    public class BehaviorAnomaly
    {
        public Guid UserId { get; set; }
        public AnomalyType Type { get; set; }
        public DateTime Timestamp { get; set; }
        public string Description { get; set; } = string.Empty;
        public double Severity { get; set; } // 0-1, onde 1 é muito severo
        public Dictionary<string, object> Context { get; set; } = new();
        public DateTime DetectedAt { get; set; }
    }

    /// <summary>
    /// Representa um horário ótimo sugerido para alarmes
    /// </summary>
    public class OptimalTimeSlot
    {
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public double OptimalityScore { get; set; } // 0-1, onde 1 é ótimo
        public string Reasoning { get; set; } = string.Empty;
        public List<string> SupportingEvidence { get; set; } = new();
    }

    /// <summary>
    /// Tipos de padrão detectados
    /// </summary>
    public enum PatternType
    {
        WakeUp,
        Sleep,
        Work,
        Exercise,
        Meal,
        Commute,
        Recreation,
        Other
    }

    /// <summary>
    /// Tipos de anomalia comportamental
    /// </summary>
    public enum AnomalyType
    {
        UnusualWakeTime,
        ConsecutiveDisables,
        ExcessiveSnoozing,
        PatternBreak,
        InactivityPeriod,
        FrequencyChange,
        Other
    }
}