
# Integrations and Resilience Patterns

## External Integrations

- All integrations use HttpClientFactory for connection management
- Resilience implemented with Polly (retry, circuit breaker, timeout)
- Authentication via OAuth2/OpenID Connect for external APIs
- Centralized and secure configuration of endpoints and secrets

## Implementation Patterns

- Integration abstraction in the Infrastructure layer
- DTOs for integration input/output
- Automated integration tests
- Structured logging of external calls

## Usage Example (C#)

```csharp
public class ExternalApiService
{
    private readonly HttpClient _httpClient;
    public ExternalApiService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<string> GetDataAsync()
    {
        // Polly policy configured via DI
        return await _httpClient.GetStringAsync("/external-endpoint");
    }
}
```

---

**Status:** Integrations and resilience documented and standardized.
