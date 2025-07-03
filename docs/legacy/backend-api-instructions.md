# Legacy Backend Instructions - Update to C#/.NET

> This document has been updated to reflect the new unified backend architecture in C#/.NET. All previous instructions related to Go, Python, Node.js, or multi-language have been removed.

## Backend Structure

- All backend services are implemented in C# (.NET 6+), organized as independent projects (AlarmService, AnalysisService, IntegrationService).
- Use of Clean Architecture, SOLID, automated testing, and native integration with Azure Functions.

## API Standards

- RESTful APIs documented via Swagger/OpenAPI.
- JWT/FIDO2 authentication, claims-based authorization, and RBAC.
- Strict input/output validation (FluentValidation).
- Structured logging (Serilog) and tracing (Application Insights).

## Integrations and AI

- External integrations (calendars, notifications, etc.) via HttpClientFactory, Polly, and OAuth2/OpenID Connect authentication.
- AI and behavioral analysis implemented with ML.NET. Integration with Python only via .NET libraries, never exposing sensitive data outside the C# environment.

## Testing and Security

- Unit and integration tests (xUnit, Moq), minimum 80% coverage for critical code.
- Automated CI/CD (GitHub Actions/Azure DevOps), infrastructure as code (Bicep/Terraform).
- Monitoring and alerts via Application Insights.
