using MediatR;
using SmartAlarm.IntegrationService.Infrastructure.Webhooks;

namespace SmartAlarm.IntegrationService.Application.Commands
{
    /// <summary>
    /// Command para registrar webhook
    /// </summary>
    public record RegisterWebhookCommand(
        Guid UserId,
        string Provider,
        string EventType,
        string CallbackUrl,
        Dictionary<string, string>? Configuration = null,
        TimeSpan? ExpirationTime = null
    ) : IRequest<RegisterWebhookResponse>;

    /// <summary>
    /// Response do registro de webhook
    /// </summary>
    public record RegisterWebhookResponse(
        Guid WebhookId,
        Guid UserId,
        string Provider,
        string EventType,
        string CallbackUrl,
        WebhookStatus Status,
        DateTime CreatedAt,
        DateTime? ExpiresAt,
        string SecretKey
    );
}
