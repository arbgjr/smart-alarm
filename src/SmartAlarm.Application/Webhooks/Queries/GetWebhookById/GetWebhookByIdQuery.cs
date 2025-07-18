using MediatR;
using SmartAlarm.Application.Webhooks.Models;

namespace SmartAlarm.Application.Webhooks.Queries.GetWebhookById
{
    /// <summary>
    /// Query para buscar webhook por ID
    /// </summary>
    public class GetWebhookByIdQuery : IRequest<WebhookResponse?>
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        public GetWebhookByIdQuery(Guid id, Guid userId)
        {
            Id = id;
            UserId = userId;
        }
    }
}
