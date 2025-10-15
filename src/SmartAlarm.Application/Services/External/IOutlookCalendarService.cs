using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SmartAlarm.Application.Services.External
{
    /// <summary>
    /// Interface para integração com Outlook Calendar (Microsoft Graph)
    /// </summary>
    public interface IOutlookCalendarService
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
        /// Verifica se o usuário tem autorização válida para o Outlook Calendar
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>True se a autorização é válida</returns>
        Task<bool> IsAuthorizedAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cria um evento no calendário do usuário
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <param name="calendarEvent">Evento a ser criado</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>ID do evento criado</returns>
        Task<string?> CreateEventAsync(Guid userId, CalendarEvent calendarEvent, CancellationToken cancellationToken = default);

        /// <summary>
        /// Atualiza um evento no calendário do usuário
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <param name="eventId">ID do evento</param>
        /// <param name="calendarEvent">Dados atualizados do evento</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>True se atualizado com sucesso</returns>
        Task<bool> UpdateEventAsync(Guid userId, string eventId, CalendarEvent calendarEvent, CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove um evento do calendário do usuário
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <param name="eventId">ID do evento</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>True se removido com sucesso</returns>
        Task<bool> DeleteEventAsync(Guid userId, string eventId, CancellationToken cancellationToken = default);
    }
}
