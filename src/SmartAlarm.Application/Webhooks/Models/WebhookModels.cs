using System.Net;

namespace SmartAlarm.Application.Webhooks.Models
{
    /// <summary>
    /// Modelo de resposta de erro padronizado
    /// </summary>
    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Detail { get; set; }
        public IEnumerable<string>? Errors { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public ErrorResponse(HttpStatusCode statusCode, string message, string? detail = null, IEnumerable<string>? errors = null, string? correlationId = null)
        {
            StatusCode = (int)statusCode;
            Message = message;
            Detail = detail;
            Errors = errors;
            CorrelationId = correlationId ?? string.Empty;
        }

        public ErrorResponse(int statusCode, string message, string? detail = null, IEnumerable<string>? errors = null, string? correlationId = null)
        {
            StatusCode = statusCode;
            Message = message;
            Detail = detail;
            Errors = errors;
            CorrelationId = correlationId ?? string.Empty;
        }
    }

    /// <summary>
    /// Modelo de requisição para criação de webhook
    /// </summary>
    public class CreateWebhookRequest
    {
        public string Url { get; set; } = string.Empty;
        public string[] Events { get; set; } = Array.Empty<string>();
        public string? Description { get; set; }
    }

    /// <summary>
    /// Modelo de requisição para atualização de webhook
    /// </summary>
    public class UpdateWebhookRequest
    {
        public string? Url { get; set; }
        public string[]? Events { get; set; }
        public bool? IsActive { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>
    /// Modelo de resposta de webhook
    /// </summary>
    public class WebhookResponse
    {
        public Guid Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public string[] Events { get; set; } = Array.Empty<string>();
        public string? Description { get; set; }
        public string Secret { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid UserId { get; set; }
        public int FailureCount { get; set; }
        public DateTime? LastDeliveryAttempt { get; set; }
        public DateTime? LastSuccessfulDelivery { get; set; }
    }

    /// <summary>
    /// Modelo de resposta da lista de webhooks
    /// </summary>
    public class WebhookListResponse
    {
        public IEnumerable<WebhookResponse> Webhooks { get; set; } = Enumerable.Empty<WebhookResponse>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
    }
}
