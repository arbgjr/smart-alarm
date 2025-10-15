using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SmartAlarm.Application.Services.External
{
    /// <summary>
    /// Interface para integração com Google Calendar
    /// </summary>
    public interface IGoogleCalendarService
    {
        /// <summary>
        /// Obtém eventos do calendário do usuário em um período
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <param name="startDate">Data inicial</param>
        /// <param name="endDate">Data final</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Lista de eventos do calendário</returns>
        Task<List<CalendarEvent>> GetEventsAsync(Guid userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifica se o usuário tem eventos de férias/folga em uma data
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <param name="date">Data para verificar</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>True se tiver eventos de férias/folga</returns>
        Task<bool> HasVacationOrDayOffAsync(Guid userId, DateTime date, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtém o próximo evento do usuário
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Próximo evento ou null</returns>
        Task<CalendarEvent?> GetNextEventAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sincroniza calendário do usuário
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <param name="accessToken">Token de acesso OAuth</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Quantidade de eventos sincronizados</returns>
        Task<int> SyncCalendarAsync(Guid userId, string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifica se o usuário tem autorização válida para o Google Calendar
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>True se a autorização é válida</returns>
        Task<bool> IsAuthorizedAsync(Guid userId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Representa um evento do calendário
    /// </summary>
    public class CalendarEvent
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsAllDay { get; set; }
        public string? Location { get; set; }
        public CalendarEventType Type { get; set; }
        public List<string> Attendees { get; set; } = new();
        public bool IsRecurring { get; set; }
        public string? RecurrenceRule { get; set; }
    }

    /// <summary>
    /// Tipo de evento do calendário
    /// </summary>
    public enum CalendarEventType
    {
        Regular,
        Vacation,
        DayOff,
        Holiday,
        Meeting,
        Personal,
        Work,
        Other
    }
}