# Smart Alarm - Copilot Instructions

## Project Overview

**Smart Alarm** is a backend and services platform for intelligent management of alarms, routines, and integrations, with a focus on accessibility and security. The entire backend architecture is exclusively based on **C# (.NET 6+)**, following Clean Architecture, SOLID, and modern practices for testability, observability, and integration. The default serverless pattern is **Oracle Cloud Infrastructure (OCI Functions)**, aiming for cost optimization and scalability.

### System Patterns
Follow the system patterns defined in [`systemPatterns.md`](/docs/architecture/systemPatterns.md).

### Code Standard
- Follow [`code-generation.instructions.md`](./instructions/code-generation.instructions.md)

### Key Features
- **RESTful APIs**: Alarm services, AI, and external integrations
- **Modular Architecture**: Clear separation of domain, application, infrastructure, and presentation
- **C#/.NET**: Single language for all backend, including AI (ML.NET)
- **Testing**: xUnit, Moq, and minimum 80% coverage for critical code
- **Security**: JWT/FIDO2 authentication, LGPD, structured logging (Serilog), Application Insights
- **Documentation**: Swagger/OpenAPI for endpoints, architecture, and compliance docs
- **Cloud**: OCI Functions (Oracle Cloud Infrastructure) as serverless standard, Autonomous DB, Object Storage

### Technology Stack
- **Backend:** C# (.NET 6+), Clean Architecture, OCI Functions, ML.NET
- **Frontend:** React, TypeScript, PWA, Atomic Design, integration via RESTful APIs
- **Cloud:** OCI (Functions, Autonomous DB, Object Storage, Vault, Application Performance Monitoring)
- **Integrations:** HttpClientFactory, Polly, OAuth2/OpenID Connect, external APIs

## Folder Structure

```
/AlarmService
  /Application
  /Domain
  /Infrastructure
  /Api
/AnalysisService
/IntegrationService
/docs
  /architecture
  /development
  /business
/tests
  /unit
  /integration
/infrastructure
  /docker
  /kubernetes
  /terraform
```

- Each service is an independent .NET project, preferably serverless (OCI Functions).
- Technical and business documentation in docs.
- Tests close to the code and organized by domain.

## Architecture and Code Standards

### Backend (C#/.NET)
- Clean Architecture and SOLID in all services
- Clear separation: Controllers, Application, Domain, Infrastructure
- DTOs for input/output, strict validation (FluentValidation)
- Structured logging (Serilog), tracing (Application Insights)
- Automated tests (xUnit, Moq), minimum 80% coverage
- Documentation via Swagger/OpenAPI
- JWT/FIDO2 authentication, RBAC, LGPD compliance
- External integrations via HttpClientFactory, Polly, OAuth2/OpenID Connect
- Never expose secrets in code or logs

### Frontend (React, TypeScript, PWA)
- Follow Atomic Design for component organization
- Use React, TypeScript, and hooks for UI logic
- Separate components by atomicity (atoms, molecules, organisms, pages)
- Use context API for global state and custom hooks for shared logic
- Implement accessibility (WCAG), responsiveness, and internationalization
- Use Service Workers for PWA and notifications
- Test components with Testing Library and simulate real interactions
- Document props and component contracts with TypeScript
- Never expose tokens or secrets in code or bundle

### Integrations and Cloud
- OCI Functions as serverless standard (Oracle Functions, Autonomous DB, Object Storage)
- Infrastructure as code (prefer Terraform, Bicep optional)
- Abstraction for cloud provider, facilitating future migration
- Centralized monitoring and alerts (OCI Application Performance Monitoring, Logging)
- On the frontend, consume APIs via HttpClient/Fetch, handle errors and loading states

### Naming and Organization
- PascalCase for classes, public methods, files, and React components
- camelCase for variables, private methods, and functions
- UPPER_SNAKE_CASE for global constants
- Descriptive names, no abbreviations
- On the frontend, organize components in folders by atomicity

### Testing
- xUnit for unit/integration tests (backend)
- Mocks with Moq (backend)
- AAA (Arrange, Act, Assert)
- Test for success, error, and edge cases
- Tests close to the implemented code
- On the frontend, use Testing Library for React components, cover interactions, accessibility, and visual states

### Security and Compliance
- OWASP Top 10, LGPD, granular consent
- AES-256-GCM for data at rest, TLS 1.3 in transit
- FIDO2/WebAuthn for passwordless authentication
- BYOK (Bring Your Own Key) for sensitive data
- On the frontend, never expose tokens or secrets in code or bundle
- Implement authentication and authorization when consuming APIs
- Follow accessibility (WCAG) and privacy (LGPD) practices in the interface

## Development Flow

- Install dependencies: `dotnet restore`
- Build: `dotnet build`
- Run tests: `dotnet test`
- Deploy: OCI Functions via CI/CD pipelines
- Document endpoints and decisions in architecture

## Pull Requests & Code Review

- Follow the conventional commit format
- Clearly describe what changed and why
- Include context, changes, tests performed, and pending items
- Use technical and business reviewers

## Examples

### C# Handler (AlarmService)

```csharp
public class CreateAlarmHandler : IRequestHandler<CreateAlarmCommand, AlarmResponse>
{
    private readonly IAlarmRepository _alarmRepository;
    private readonly IValidator<CreateAlarmCommand> _validator;
    private readonly ILogger<CreateAlarmHandler> _logger;

    public async Task<AlarmResponse> Handle(CreateAlarmCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Validation failed: {@Errors}", validationResult.Errors);
            throw new ValidationException(validationResult.Errors);
        }

        var alarm = new Alarm { /* ... */ };
        await _alarmRepository.AddAsync(alarm);
        _logger.LogInformation("Alarm created: {AlarmId}", alarm.Id);
        return new AlarmResponse(alarm);
    }
}
```

### C# Unit Test

```csharp
[Fact]
public async Task Should_ThrowValidationException_When_CommandIsInvalid()
{
    var handler = new CreateAlarmHandler(...);
    var invalidCommand = new CreateAlarmCommand { /* ... */ };
    await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(invalidCommand, CancellationToken.None));
}
```

---
