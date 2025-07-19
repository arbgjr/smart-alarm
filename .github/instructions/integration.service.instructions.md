---
applyTo: "services/integration-service/**"
---
# Integration Service Instructions

## 1. Core Responsibility

The **Integration Service** acts as the Anti-Corruption Layer (ACL) for all external system integrations. It manages calendar synchronization, third-party API communication, webhook processing, and data transformation between external formats and Smart Alarm domain models.

## 2. Key Technologies

- **Framework**: .NET 8, ASP.NET Core
- **HTTP Communication**: `IHttpClientFactory` with Polly resilience policies for all external API calls
- **Authentication**: OAuth 2.0, OpenID Connect, API key management via `IKeyVaultService`
- **External APIs**: Google Calendar API, Microsoft Graph API, CalDAV, Apple CloudKit
- **Message Processing**: RabbitMQ for asynchronous integration events
- **Data Transformation**: AutoMapper for DTO-to-Domain model mapping
- **Persistence**: Entity Framework Core for integration metadata and sync status

## 3. Architectural Patterns

- **Anti-Corruption Layer**: Translates external data models to internal domain models, preventing external schemas from leaking into the system
- **Facade Pattern**: Each external service has its own client facade (e.g., `GoogleCalendarClient`, `MicrosoftGraphClient`)
- **Circuit Breaker**: Polly policies protect against cascading failures when external services are unavailable
- **Retry with Exponential Backoff**: Handle transient failures gracefully with intelligent retry logic
- **Provider Pattern**: Pluggable calendar providers (`ICalendarProvider`) for easy extensibility

## 4. Code Generation Rules

- **Client Implementation**: Create dedicated client classes in Infrastructure layer implementing interfaces from Application layer
- **Resilience Policies**: All HTTP clients must have Polly policies for timeout, retry, and circuit breaker
- **Data Mapping**: Use dedicated mapper classes to convert external DTOs to internal domain models
- **Configuration Management**: Load all API endpoints, keys, and settings from `IConfiguration`, never hardcode
- **Error Handling**: Use custom exceptions for integration-specific errors (`ExternalApiException`, `AuthenticationException`)
- **Observability**: Wrap all external calls with activities, log request/response metadata (sanitized)

## 5. HTTP Client Configuration Pattern

```csharp
// Dependency Injection Setup
public static IServiceCollection AddExternalIntegrations(this IServiceCollection services, IConfiguration configuration)
{
    // Resilience policies
    var retryPolicy = HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) => 
            {
                // Log retry attempts
            });

    var circuitBreakerPolicy = HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            exceptionsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30));

    // Google Calendar Client
    services.AddHttpClient<IGoogleCalendarClient, GoogleCalendarClient>(client =>
    {
        client.BaseAddress = new Uri(configuration["GoogleCalendar:ApiBaseUrl"]);
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddPolicyHandler(retryPolicy)
    .AddPolicyHandler(circuitBreakerPolicy);

    return services;
}
```

## 6. External Data Transformation

```csharp
// Anti-Corruption Layer Example
public class GoogleCalendarEventMapper
{
    public CalendarEvent MapToDomain(GoogleCalendarEventDto externalEvent)
    {
        return new CalendarEvent
        {
            ExternalId = externalEvent.Id,
            Title = externalEvent.Summary ?? string.Empty,
            Description = externalEvent.Description,
            StartTime = ParseGoogleDateTime(externalEvent.Start),
            EndTime = ParseGoogleDateTime(externalEvent.End),
            Location = externalEvent.Location,
            Source = CalendarSource.Google,
            LastSyncAt = DateTime.UtcNow
        };
    }

    private DateTime ParseGoogleDateTime(GoogleDateTimeDto dateTime)
    {
        // Handle Google's complex date/time format
        return dateTime.DateTime?.ToUniversalTime() ?? 
               DateTime.ParseExact(dateTime.Date, "yyyy-MM-dd", null);
    }
}
```

## 7. Webhook Processing

```csharp
// Webhook Handler Pattern
[ApiController]
[Route("api/v1/webhooks")]
public class WebhookController : ControllerBase
{
    [HttpPost("calendar/{provider}")]
    public async Task<IActionResult> ProcessCalendarWebhook(
        string provider,
        [FromBody] object payload,
        CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity("ProcessCalendarWebhook");
        activity?.SetTag("provider", provider);

        var webhook = new WebhookEvent
        {
            Provider = provider,
            Payload = JsonSerializer.Serialize(payload),
            ReceivedAt = DateTime.UtcNow,
            ProcessingStatus = WebhookStatus.Received
        };

        await _webhookRepository.AddAsync(webhook);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Queue for background processing
        await _messagingService.PublishAsync(new WebhookReceivedEvent(webhook.Id));

        return Ok();
    }
}
```

## 8. OAuth 2.0 Authentication Flow

```csharp
public class GoogleCalendarClient : IGoogleCalendarClient
{
    private readonly HttpClient _httpClient;
    private readonly IKeyVaultService _keyVaultService;

    public async Task<string> GetAccessTokenAsync(string userId)
    {
        var refreshToken = await _keyVaultService.GetSecretAsync($"google-refresh-token-{userId}");
        
        var tokenRequest = new
        {
            grant_type = "refresh_token",
            refresh_token = refreshToken,
            client_id = await _keyVaultService.GetSecretAsync("google-client-id"),
            client_secret = await _keyVaultService.GetSecretAsync("google-client-secret")
        };

        var response = await _httpClient.PostAsJsonAsync("oauth2/v4/token", tokenRequest);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
        return tokenResponse.AccessToken;
    }
}
```

## 9. Quality Standards

- **Rate Limiting**: Implement proper rate limiting for external API calls to avoid quota exhaustion
- **Caching**: Cache external data appropriately with TTL based on data freshness requirements
- **Data Privacy**: Never log sensitive user data, sanitize all external responses before logging
- **Testing**: Mock external services for unit tests, use integration tests with real APIs in staging
- **Error Recovery**: Implement graceful degradation when external services are unavailable
- **Monitoring**: Track integration health, success rates, and performance metrics
- **Documentation**: Document all external API integrations, authentication flows, and data mappings

## 10. Sync Strategy Patterns

```csharp
// Incremental Sync with Delta Tokens
public class CalendarSyncService
{
    public async Task SyncUserCalendarAsync(Guid userId, CancellationToken cancellationToken)
    {
        var integration = await _integrationRepository.GetByUserIdAsync(userId);
        var lastSyncToken = integration.LastSyncToken;

        var changes = lastSyncToken != null
            ? await _calendarClient.GetIncrementalChangesAsync(userId, lastSyncToken)
            : await _calendarClient.GetFullCalendarAsync(userId);

        foreach (var change in changes.Items)
        {
            await ProcessCalendarChangeAsync(change, userId);
        }

        integration.UpdateLastSync(changes.NextSyncToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```
