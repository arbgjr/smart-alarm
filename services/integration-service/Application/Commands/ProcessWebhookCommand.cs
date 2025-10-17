using MediatR;

namespace SmartAlarm.IntegrationService.Application.Commands
{
    /// <summary>
    /// Command para processar webhook recebido
    /// </summary>
    public record ProcessWebhookCommand(
        string WebhookId,
        string Payload,
        Dictionary<string, string> Headers
    ) : IRequest<ProcessWebhookResponse>;

    /// <summary>
    /// Response do processamento de webhook
    /// </summary>
    public record ProcessWebhookResponse(
        Guid WebhookId,
        bool Success,
        string Message,
        DateTime ProcessedAt,
        TimeSpan ProcessingDuration,
        IEnumerable<string> ActionsTriggered
    );
}
