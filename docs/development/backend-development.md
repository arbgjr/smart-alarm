# Backend Development Guide - Unified C# Architecture

This guide covers backend development for Smart Alarm using exclusively C# (.NET), following Clean Architecture, SOLID principles, testability, security, and native integration with Azure Functions.

## 🏗️ Architecture Philosophy

The backend architecture is based on specialized services, all implemented in C#/.NET, organized as independent projects (AlarmService, AnalysisService, IntegrationService), preferably serverless (Azure Functions). All services follow Clean Architecture, with clear separation of layers (presentation, application, domain, infrastructure), facilitating testing, maintenance, and evolution.

## 🚀 AlarmService: Alarm CRUD Operations

AlarmService is responsible for all CRUD operations for alarms, business rules, notifications, and specific validations for neurodivergent users. It uses Entity Framework Core for persistence, FluentValidation for validation, and structured logging (Serilog).

```csharp
// Application/Handlers/CreateAlarmHandler.cs
public class CreateAlarmHandler : IRequestHandler<CreateAlarmCommand, AlarmResponse>
{
    private readonly IAlarmRepository _alarmRepository;
    private readonly IValidator<CreateAlarmCommand> _validator;
    private readonly ILogger<CreateAlarmHandler> _logger;

    public CreateAlarmHandler(IAlarmRepository alarmRepository, IValidator<CreateAlarmCommand> validator, ILogger<CreateAlarmHandler> logger)
    {
        _alarmRepository = alarmRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<AlarmResponse> Handle(CreateAlarmCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Validation failed: {@Errors}", validationResult.Errors);
            throw new ValidationException(validationResult.Errors);
        }

        var alarm = new Alarm
        {
            // ...assign fields from command...
        };

        await _alarmRepository.AddAsync(alarm);
        _logger.LogInformation("Alarm created: {AlarmId}", alarm.Id);
        return new AlarmResponse(alarm);
    }
}
```

## 🤖 AnalysisService: AI and Behavioral Analysis

All AI and behavioral analysis logic is implemented in C# using ML.NET. When necessary, integrations with TensorFlow or PyTorch can be made via .NET libraries, always keeping the main logic and sensitive data under C# backend control.

- Recommendation models, pattern analysis, and contextual suggestions are trained and served via ML.NET.
- Unit and integration tests ensure model robustness.

## 🔗 IntegrationService: External Integrations

All integrations with external APIs (calendars, notifications, holidays, etc.) are done via .NET libraries, with OAuth2/OpenID Connect authentication, standardized logging, and error handling (Polly, HttpClientFactory).

## 🛡️ Security, Testability, and Observability

- JWT/FIDO2 authentication, claims-based authorization, and RBAC.
- Structured logging (Serilog), distributed tracing (Application Insights), monitoring, and alerts.
- Automated tests (xUnit, Moq), minimum 80% coverage for critical code.
- Documentation via Swagger/OpenAPI.

## 🧩 Patterns and Best Practices

- Clean Architecture and SOLID in all services.
- Strict input/output validation.
- Centralized error handling and user-friendly responses.
- Automated CI/CD (GitHub Actions/Azure DevOps), infrastructure as code (Bicep/Terraform).

## Example Project Structure

```
/AlarmService
  /Application
  /Domain
  /Infrastructure
  /Api
/AnalysisService
/IntegrationService
```

## Final Notes

- The entire backend is C#/.NET, with no dependencies on Go, Python, or Node.js.
- Any integration with Python for AI is encapsulated and never exposes sensitive data outside the .NET environment.
- The focus is always on testability, security, accessibility, and long-term maintainability.