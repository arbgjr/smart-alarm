using MediatR;
using SmartAlarm.IntegrationService.Infrastructure.Calendars;

namespace SmartAlarm.IntegrationService.Application.Commands
{
    /// <summary>
    /// Command para sincronizar calendário externo
    /// </summary>
    public record SyncExternalCalendarCommand(
        Guid UserId,
        string Provider,
        string AccessToken,
        DateTime? StartDate = null,
        DateTime? EndDate = null,
        bool ForceFullSync = false
    ) : IRequest<SyncExternalCalendarResponse>;

    /// <summary>
    /// Response da sincronização de calendário externo
    /// </summary>
    public record SyncExternalCalendarResponse(
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
}
