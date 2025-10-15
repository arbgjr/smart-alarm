namespace SmartAlarm.IntegrationService.Infrastructure.Calendars
{
    /// <summary>
    /// Interface para serviços de integração com calendários
    /// </summary>
    public interface ICalendarIntegrationService
    {
        /// <summary>
        /// Provedor suportado por este serviço
        /// </summary>
        string Provider { get; }

        /// <summary>
        /// Sincroniza eventos do calendário externo
        /// </summary>
        Task<CalendarSyncResult> SyncEventsAsync(
            CalendarSyncRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Cria evento no calendário externo
        /// </summary>
        Task<CalendarEvent> CreateEventAsync(
            CreateCalendarEventRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Atualiza evento no calendário externo
        /// </summary>
        Task<CalendarEvent> UpdateEventAsync(
            UpdateCalendarEventRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove evento do calendário externo
        /// </summary>
        Task<bool> DeleteEventAsync(
            string eventId,
            string accessToken,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtém eventos em um período específico
        /// </summary>
        Task<IEnumerable<CalendarEvent>> GetEventsAsync(
            GetCalendarEventsRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Valida token de acesso
        /// </summary>
        Task<bool> ValidateAccessTokenAsync(
            string accessToken,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Solicitação de sincronização de calendário
    /// </summary>
    public record CalendarSyncRequest(
        Guid UserId,
        string AccessToken,
        DateTime? StartDate = null,
        DateTime? EndDate = null,
        bool ForceFullSync = false,
        string? CalendarId = null
    );

    /// <summary>
    /// Resultado da sincronização de calendário
    /// </summary>
    public record CalendarSyncResult(
        Guid UserId,
        string Provider,
        int EventsProcessed,
        int EventsCreated,
        int EventsUpdated,
        int EventsDeleted,
        IEnumerable<string> Errors,
        DateTime SyncedAt,
        TimeSpan Duration
    );

    /// <summary>
    /// Solicitação de criação de evento
    /// </summary>
    public record CreateCalendarEventRequest(
        string AccessToken,
        string Title,
        string? Description,
        DateTime StartTime,
        DateTime EndTime,
        string? Location = null,
        IEnumerable<string>? Attendees = null,
        string? CalendarId = null
    );

    /// <summary>
    /// Solicitação de atualização de evento
    /// </summary>
    public record UpdateCalendarEventRequest(
        string EventId,
        string AccessToken,
        string? Title = null,
        string? Description = null,
        DateTime? StartTime = null,
        DateTime? EndTime = null,
        string? Location = null,
        IEnumerable<string>? Attendees = null,
        string? CalendarId = null
    );

    /// <summary>
    /// Solicitação de obtenção de eventos
    /// </summary>
    public record GetCalendarEventsRequest(
        string AccessToken,
        DateTime StartDate,
        DateTime EndDate,
        string? CalendarId = null,
        int MaxResults = 100
    );

    /// <summary>
    /// Evento de calendário
    /// </summary>
    public record CalendarEvent(
        string Id,
        string Title,
        string? Description,
        DateTime StartTime,
        DateTime EndTime,
        string? Location,
        IEnumerable<string> Attendees,
        string CalendarId,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        Dictionary<string, object>? Metadata = null
    );
}
