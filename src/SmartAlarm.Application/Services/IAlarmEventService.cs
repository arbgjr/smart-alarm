using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Application.Services
{
    /// <summary>
    /// Serviço para gerenciamento de eventos de alarme
    /// </summary>
    public interface IAlarmEventService
    {
        /// <summary>
        /// Registra um evento de alarme
        /// </summary>
        Task RecordEventAsync(
            Guid alarmId,
            Guid userId,
            AlarmEventType eventType,
            int? snoozeMinutes = null,
            string? metadata = null,
            string? location = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Registra evento de alarme criado
        /// </summary>
        Task RecordAlarmCreatedAsync(
            Guid alarmId,
            Guid userId,
            string? metadata = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Registra evento de alarme disparado
        /// </summary>
        Task RecordAlarmTriggeredAsync(
            Guid alarmId,
            Guid userId,
            string? location = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Registra evento de soneca
        /// </summary>
        Task RecordAlarmSnoozedAsync(
            Guid alarmId,
            Guid userId,
            int snoozeMinutes,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Registra evento de alarme desativado
        /// </summary>
        Task RecordAlarmDisabledAsync(
            Guid alarmId,
            Guid userId,
            string? reason = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Registra evento de alarme ignorado/descartado
        /// </summary>
        Task RecordAlarmDismissedAsync(
            Guid alarmId,
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Registra evento de modificação de alarme
        /// </summary>
        Task RecordAlarmModifiedAsync(
            Guid alarmId,
            Guid userId,
            string? changes = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Registra evento de escalação de alarme
        /// </summary>
        Task RecordAlarmEscalatedAsync(
            Guid alarmId,
            Guid userId,
            int escalationLevel,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtém histórico de eventos de um usuário
        /// </summary>
        Task<List<AlarmEvent>> GetUserEventHistoryAsync(
            Guid userId,
            int days = 30,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtém estatísticas de eventos de um usuário
        /// </summary>
        Task<Dictionary<AlarmEventType, int>> GetUserEventStatsAsync(
            Guid userId,
            int days = 30,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtém padrões comportamentais do usuário
        /// </summary>
        Task<UserBehaviorPattern> GetUserBehaviorPatternAsync(
            Guid userId,
            int days = 30,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Representa um padrão de comportamento do usuário baseado em eventos
    /// </summary>
    public class UserBehaviorPattern
    {
        public Guid UserId { get; set; }
        public DateTime AnalysisPeriodStart { get; set; }
        public DateTime AnalysisPeriodEnd { get; set; }
        public int TotalEvents { get; set; }
        public Dictionary<DayOfWeek, int> EventsByDayOfWeek { get; set; } = new();
        public Dictionary<AlarmEventType, int> EventsByType { get; set; } = new();
        public Dictionary<int, int> EventsByHour { get; set; } = new();
        public List<BehaviorInsight> Insights { get; set; } = new();
        public double ConsistencyScore { get; set; } // 0-1, onde 1 é muito consistente
        public List<DayOfWeek> MostActiveDays { get; set; } = new();
        public List<int> MostActiveHours { get; set; } = new();
        public int AverageSnoozeMinutes { get; set; }
        public double DismissalRate { get; set; } // Porcentagem de alarmes ignorados vs disparados
    }

    /// <summary>
    /// Representa um insight de comportamento
    /// </summary>
    public class BehaviorInsight
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Confidence { get; set; } // 0-1
        public Dictionary<string, object> Data { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }
}
