# Integration Service - Smart Alarm

The Integration Service is responsible for all external integrations of Smart Alarm, implemented exclusively in C#/.NET, following Clean Architecture and modern security and testability practices.

## Technologies and Patterns

- **C# (.NET 8+):** All integration logic
- **HttpClientFactory & Polly:** Resilient consumption of external APIs
- **OAuth2/OpenID Connect:** Secure authentication and authorization
- **Structured logging:** Serilog and Application Insights
- **Automated testing:** xUnit, Moq

## Notes

- All integrations are auditable, secure, and testable
