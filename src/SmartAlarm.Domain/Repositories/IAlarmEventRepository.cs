using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Domain.Repositories
{
    /// <summary>
    /// Interface para persistência e consulta de eventos de alarme
    /// </summary>
    public interface IAlarmEventRepository
    {
        /// <summary>
        /// Adiciona um novo evento de alarme
        /// </summary>
        Task AddAsync(AlarmEvent alarmEvent, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adiciona múltiplos eventos de alarme
        /// </summary>
        Task AddRangeAsync(IEnumerable<AlarmEvent> alarmEvents, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtém evento por ID
        /// </summary>
        Task<AlarmEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtém todos os eventos de um usuário
        /// </summary>
        Task<List<AlarmEvent>> GetUserEventsAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtém eventos de um usuário em um período
        /// </summary>
        Task<List<AlarmEvent>> GetUserEventsAsync(
            Guid userId, 
            DateTime startDate, 
            DateTime endDate, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtém eventos de um usuário por tipo
        /// </summary>
        Task<List<AlarmEvent>> GetUserEventsByTypeAsync(
            Guid userId, 
            AlarmEventType eventType, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtém eventos de um usuário por tipo em um período
        /// </summary>
        Task<List<AlarmEvent>> GetUserEventsByTypeAsync(
            Guid userId, 
            AlarmEventType eventType, 
            DateTime startDate, 
            DateTime endDate, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtém eventos de um alarme específico
        /// </summary>
        Task<List<AlarmEvent>> GetAlarmEventsAsync(Guid alarmId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtém eventos de um alarme em um período
        /// </summary>
        Task<List<AlarmEvent>> GetAlarmEventsAsync(
            Guid alarmId, 
            DateTime startDate, 
            DateTime endDate, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtém estatísticas de eventos de um usuário
        /// </summary>
        Task<Dictionary<AlarmEventType, int>> GetUserEventStatsAsync(
            Guid userId, 
            DateTime startDate, 
            DateTime endDate, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtém eventos recentes de um usuário (últimos N eventos)
        /// </summary>
        Task<List<AlarmEvent>> GetRecentUserEventsAsync(
            Guid userId, 
            int count = 100, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtém padrões de eventos por dia da semana
        /// </summary>
        Task<Dictionary<DayOfWeek, List<AlarmEvent>>> GetEventPatternsByDayOfWeekAsync(
            Guid userId, 
            DateTime startDate, 
            DateTime endDate, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Conta eventos consecutivos de um tipo
        /// </summary>
        Task<int> CountConsecutiveEventsAsync(
            Guid userId, 
            Guid alarmId, 
            AlarmEventType eventType, 
            DayOfWeek dayOfWeek, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove eventos antigos (limpeza automática)
        /// </summary>
        Task DeleteOldEventsAsync(DateTime olderThan, CancellationToken cancellationToken = default);

        /// <summary>
        /// Salva alterações
        /// </summary>
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}