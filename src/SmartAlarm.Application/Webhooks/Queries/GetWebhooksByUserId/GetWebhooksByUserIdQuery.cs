using MediatR;
using SmartAlarm.Application.Webhooks.Models;

namespace SmartAlarm.Application.Webhooks.Queries.GetWebhooksByUserId
{
    /// <summary>
    /// Query para buscar webhooks de um usuário
    /// </summary>
    public class GetWebhooksByUserIdQuery : IRequest<WebhookListResponse>
    {
        public Guid UserId { get; set; }
        public bool IncludeInactive { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public GetWebhooksByUserIdQuery(Guid userId, bool includeInactive = false, int page = 1, int pageSize = 10)
        {
            UserId = userId;
            IncludeInactive = includeInactive;
            Page = Math.Max(1, page);
            PageSize = Math.Clamp(pageSize, 1, 100);
        }
    }
}
