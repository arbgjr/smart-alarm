# System Patterns - Smart Alarm Project

## Current System Architecture (Updated 19/07/2025)

### **Production-Ready Multi-Service Architecture**

The Smart Alarm system has evolved into a mature, enterprise-ready platform with **three specialized microservices**, all implemented in **C# .NET 8** following Clean Architecture principles:

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   AI Service    │    │  Alarm Service  │    │Integration Svc  │
│                 │    │                 │    │                 │  
│ • ML.NET        │    │ • Hangfire      │    │ • External APIs │
│ • Behavioral    │    │ • Background    │    │ • Calendar      │
│ • Predictions   │    │   Jobs          │    │ • Notifications │
│ • Analysis      │    │ • CRUD          │    │ • Webhooks      │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         └───────────────────────┼───────────────────────┘
                                 │
         ┌─────────────────────────────────────────────────┐
         │           Shared Infrastructure                 │
         │                                                 │
         │ • Clean Architecture (Domain/App/Infra/API)    │
         │ • OpenTelemetry (Tracing + Metrics)           │
         │ • Serilog (Structured Logging)                │
         │ • Multi-Provider Storage & KeyVault           │
         │ • JWT + FIDO2 Security                        │
         │ • PostgreSQL (Dev) / Oracle (Prod)            │
         │ • RabbitMQ (Dev) / OCI Streaming (Prod)       │
         └─────────────────────────────────────────────────┘
```

### **Environment-Based Configuration Pattern**

Each service automatically configures itself based on the deployment environment:

- **Development**: PostgreSQL + MinIO + HashiCorp Vault + InMemory caches
- **Staging**: PostgreSQL + MinIO + Azure KeyVault + Redis
- **Production**: Oracle + OCI Storage + OCI Vault + Redis + SSL/TLS

## Core Architectural Patterns

### **Clean Architecture Implementation**

All services strictly follow the 4-layer Clean Architecture:

```
┌─────────────┐ ←── Controllers, Middleware, DTOs
│   API       │
├─────────────┤ ←── CQRS Handlers, Services, Validation  
│ Application │
├─────────────┤ ←── Entities, Value Objects, Domain Services
│   Domain    │  
├─────────────┤ ←── Repositories, External Services, DB Context
│Infrastructure│
└─────────────┘
```

**Key Principles**:
- **Domain** layer has zero external dependencies
- **Application** layer contains business workflows (MediatR + CQRS)  
- **Infrastructure** layer implements interfaces defined in Domain
- **API** layer is thin - only routing, validation, and serialization

### **CQRS + MediatR Pattern**

All business operations use Command/Query Responsibility Segregation:

```csharp
// Commands for write operations
public class CreateAlarmCommand : IRequest<AlarmResponse>
public class CreateAlarmCommandHandler : IRequestHandler<CreateAlarmCommand, AlarmResponse>

// Queries for read operations  
public class GetAlarmByIdQuery : IRequest<AlarmResponse>
public class GetAlarmByIdQueryHandler : IRequestHandler<GetAlarmByIdQuery, AlarmResponse>
```

### **Multi-Provider Repository Pattern**

Database and external service access uses provider abstraction:

```csharp
// Interface defined in Domain
public interface IAlarmRepository 
{
    Task<Alarm> GetByIdAsync(Guid id);
    Task AddAsync(Alarm alarm);
}

// Implementations in Infrastructure
public class EfAlarmRepository : IAlarmRepository // PostgreSQL/Oracle
public class InMemoryAlarmRepository : IAlarmRepository // Testing
```

**Environment Selection**:
- Development/Testing: In-memory or PostgreSQL
- Staging: PostgreSQL with cloud services
- Production: Oracle with OCI services

### **Comprehensive Observability Pattern**

Every service includes complete observability instrumentation:

```csharp
public class AlarmCommandHandler 
{
    private readonly ILogger<AlarmCommandHandler> _logger;
    private readonly SmartAlarmActivitySource _activitySource;
    private readonly IMeter _meter;

    public async Task<AlarmResponse> Handle(CreateAlarmCommand request)
    {
        using var activity = _activitySource.StartActivity("CreateAlarm");
        activity?.SetTag("user.id", request.UserId.ToString());
        
        _logger.LogInformation("Creating alarm for user {UserId}", request.UserId);
        _meter.IncrementCounter("alarms_created_total");
        
        // Business logic...
    }
}
```

**Stack**: OpenTelemetry + Serilog + Prometheus + Jaeger + Grafana

## Security Architecture Patterns

### **Multi-Provider KeyVault Pattern**

Secrets management adapts to deployment environment:

```csharp
public interface IKeyVaultService
{
    Task<string> GetSecretAsync(string secretName);
    Task SetSecretAsync(string secretName, string value);
}

// Production: OciVaultProvider, AzureKeyVaultProvider
// Development: HashiCorpVaultProvider  
// Testing: InMemoryKeyVaultProvider
```

### **JWT + Redis Blacklist Pattern**

Token revocation system for security:

```csharp
public interface IJwtBlocklistService
{
    Task<bool> IsTokenBlockedAsync(string tokenId);
    Task BlockTokenAsync(string tokenId, TimeSpan expiry);
}

// Production/Staging: RedisJwtBlocklistService
// Development: InMemoryJwtBlocklistService
```

## Integration & Resilience Patterns

### **Circuit Breaker + Retry Pattern**

All external service calls use Polly for resilience:

```csharp
services.AddHttpClient<GoogleCalendarService>()
    .AddPolicyHandler(Policy
        .Handle<HttpRequestException>()
        .CircuitBreakerAsync(3, TimeSpan.FromSeconds(30)));
```

### **Smart Storage Fallback Pattern**

Storage service automatically detects and falls back:

```csharp
public class SmartStorageService : IStorageService
{
    // Tries MinIO first, falls back to MockStorage if unavailable
    public async Task<string> UploadFileAsync(Stream fileStream, string fileName)
    {
        try 
        {
            return await _minioService.UploadFileAsync(fileStream, fileName);
        }
        catch (MinioException)
        {
            _logger.LogWarning("MinIO unavailable, falling back to mock storage");
            return await _mockStorage.UploadFileAsync(fileStream, fileName);
        }
    }
}
```

## Testing Patterns

### **AAA Testing Pattern** 
All tests follow Arrange-Act-Assert structure:

```csharp
[Fact]
public async Task Handle_ValidCommand_ShouldCreateAlarm()
{
    // Arrange
    var command = new CreateAlarmCommand { Name = "Test Alarm" };
    var mockRepo = new Mock<IAlarmRepository>();
    var handler = new CreateAlarmHandler(mockRepo.Object, /* other deps */);

    // Act  
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    mockRepo.Verify(x => x.AddAsync(It.IsAny<Alarm>()), Times.Once);
}
```

### **Integration Test Categorization**

Tests are organized by category for selective execution:

```csharp
[Trait("Category", "Integration")]
[Trait("Group", "Storage")]
public class MinioStorageTests { }

[Trait("Category", "Integration")]  
[Trait("Group", "Database")]
public class EfRepositoryTests { }
```

**Execution**: `dotnet test --filter "Category=Integration&Group=Storage"`

## Code Organization Patterns

### **Domain-Driven Structure**
```
src/
├── SmartAlarm.Domain/          # Core business entities
├── SmartAlarm.Application/     # Use cases and handlers  
├── SmartAlarm.Infrastructure/  # External service implementations
├── SmartAlarm.Api/            # REST API controllers
├── SmartAlarm.AiService/      # AI/ML microservice
├── SmartAlarm.AlarmService/   # Alarm management microservice
└── SmartAlarm.IntegrationService/ # External integrations microservice
```

### **Naming Conventions**
- **Classes**: `PascalCase` (AlarmService, CreateAlarmHandler)
- **Methods**: `PascalCase` (GetByIdAsync, HandleAsync)  
- **Variables**: `camelCase` (alarmId, createdAt)
- **Constants**: `UPPER_SNAKE_CASE` (MAX_RETRY_ATTEMPTS)
- **Private Fields**: `_camelCase` (_logger, _repository)

### **Error Handling Patterns**

**Domain Exceptions**: Custom exceptions for business rule violations
```csharp
public class AlarmNotFoundException : DomainException
{
    public AlarmNotFoundException(Guid alarmId) 
        : base($"Alarm with ID {alarmId} was not found") { }
}
```

**Global Exception Handling**: Middleware catches and formats all errors
```csharp
public class GlobalExceptionMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try { await next(context); }
        catch (DomainException ex) 
        {
            await HandleDomainExceptionAsync(context, ex);
        }
    }
}
```

## Quality Assurance Patterns

### **Build Pipeline Standards**
- ✅ All projects compile without errors
- ✅ Unit test coverage > 80% for business logic
- ✅ Integration tests for external dependencies
- ✅ Static code analysis (SonarQube/CodeQL)
- ✅ Security scanning (SAST/DAST)
- ✅ Performance benchmarks for critical paths

### **Documentation Standards**
- XML documentation for all public APIs
- Architecture Decision Records (ADRs) for major decisions
- OpenAPI/Swagger for REST endpoints
- README files for each service
- Runbooks for operational procedures

---

**Last Updated**: July 19, 2025  
**Architecture Status**: Production Ready ✅  
**Technical Debt**: Zero critical items ✅
**Next Phase**: MVP Implementation - Frontend & User Experience ✅

## Documentation Organization (Updated 19/07/2025)

### **Implementation Plans Structure**
All implementation plans consolidated in `/docs/plan/` with standardized format:

- **[Index & Overview](../docs/plan/README.md)**: Central hub for all implementation plans
- **[MVP Roadmap Implementation](../docs/plan/feature-mvp-roadmap-implementation-1.md)**: Current active 12-week plan
- **[Frontend Implementation](../docs/plan/feature-frontend-implementation-1.md)**: React TypeScript PWA detailed plan
- **[Historical Evolution](../docs/plan/project-evolution-historical-1.md)**: Project evolution context

### **Future Frontend Patterns (To Be Established)**
As the frontend implementation progresses, the following patterns will be added:

#### **React Architecture Patterns** (Phase 2-3)
- Atomic Design component hierarchy
- Custom hooks for business logic
- Context API for state management  
- React Query for server state
- Error boundary implementation

#### **PWA Implementation Patterns** (Phase 2)
- Service Worker caching strategies
- Offline-first architecture
- IndexedDB with Dexie.js
- Push notification integration

#### **Accessibility Patterns** (All Phases)
- WCAG 2.1 AAA compliance
- Screen reader compatibility
- Keyboard navigation
- Focus management
- ARIA implementation

#### **Testing Patterns** (Phase 4)
- React Testing Library for components
- Playwright for E2E testing
- Axe for accessibility testing
- Cross-browser compatibility

### **Quality Standards Evolution**
The existing backend quality standards will be extended to include:
- Frontend test coverage > 80%
- Accessibility compliance validation
- Performance budgets (Core Web Vitals)
- Cross-platform PWA functionality

### Naming

- camelCase for variables, functions, and methods.
- PascalCase for classes, types, React components, and files.
- UPPER_SNAKE_CASE for global constants.
- Descriptive and clear names, no abbreviations.

### JavaScript/TypeScript (Frontend)

- Use double quotes for user-visible strings and single quotes for internal strings.
- Always use a semicolon at the end of statements.
- Prefer const for immutable variables and let for mutable ones. Avoid var.
- Use arrow functions `=>` and only add parentheses to parameters when necessary.
- Always use braces in conditionals and loops, with the opening brace on the same line.
- Use JSDoc to document public functions, classes, and interfaces (backend) and TypeScript types/interfaces for props and states (frontend).
- Do not export types or functions unnecessarily.
- On the frontend, use React.FC for functional components and prefer hooks for reusable logic.

### Tests

- Write unit tests for all business logic (Vitest or Jest).
- Include success, failure, and edge cases.
- Use mocks for external dependencies.
- Name tests descriptively (e.g., "should return error if...").
- Follow the AAA pattern (Arrange, Act, Assert).
- On the frontend, use Testing Library for React components, cover interactions, accessibility, and visual states.
- Implement integration tests for external services using HTTP health checks to verify availability.
- For tests involving MinIO, Vault, and other services, use HttpClient to simplify availability checks.
- Organize integration tests by category to allow selective execution (e.g., MinIO, Vault, Database).

### Integration Tests

- **Categorization**: Use xUnit's `[Trait]` or `[Category]` attributes to organize tests.
  - `[Trait("Category", "Integration")]`
  - `[Trait("Group", "Essential")]` (e.g., Database, Storage)
  - `[Trait("Group", "Observability")]` (e.g., Logging, Tracing)
- **Execution**: Run specific categories using the `--filter` option.
  - `dotnet test --filter "Category=Integration"`
  - `dotnet test --filter "Category=Integration&Group=Essential"`
- **Health Checks**: Prefer using service health endpoints for availability checks in tests.
  - MinIO: `/minio/health/live`
  - Vault: `/v1/sys/health`
  - PostgreSQL: Use `pg_isready` command.
  - RabbitMQ: Use `rabbitmqctl status`.
- **Orchestration**: Use Docker Compose (`docker-compose.yml`) to manage the test environment's infrastructure.
- **Isolation**: Ensure tests are isolated and can run in parallel without interfering with each other. Use dedicated databases or schemas for testing.

### Error Handling

- Use try/catch to capture exceptions.
- Prefer throwing specific errors.
- Always log errors with relevant context.
- Validate all user inputs and external data.
- On the frontend, handle API errors and display user-friendly messages.

### Security

- Never expose credentials or secrets in code.
- Use environment variables for sensitive data.
- Validate and sanitize user inputs.
- Do not log sensitive information.
- On the frontend, never expose tokens or secrets in code or the bundle.
- Implement authentication and authorization when consuming APIs.
- Follow accessibility (WCAG) and privacy (LGPD) practices in the interface.

### Backend (APIs and Services)

- Follow Clean Architecture and SOLID principles for all backend logic.
- Separate controllers, services, repositories, and entities.
- Use DTOs for data input and output.
- Implement authentication and authorization as needed by the domain.
- Always validate and sanitize data received in endpoints.
- Use structured logs to track requests and errors, without exposing sensitive data.
- Implement unit and integration tests for critical endpoints and services.
- Document endpoints and API contracts (e.g., Swagger/OpenAPI).
- Prefer middlewares for error handling and authentication.
- Never expose secrets or sensitive variables in code or logs.

### Frontend (React/PWA)

- Follow Atomic Design for component organization.
- Use React, TypeScript, and hooks for UI logic.
- Separate components by atomicity (atoms, molecules, organisms, pages).
- Use context API for global state and custom hooks for shared logic.
- Implement accessibility (WCAG), responsiveness, and internationalization.
- Use Service Workers for PWA and notifications.
- Test components with Testing Library and simulate real interactions.
- Document props and component contracts with TypeScript.

### Integrations and APIs

- Use tools and best practices for OCI, OpenAI, GitHub, and other integrations.
- Always consult the specific instructions for each service (e.g., OCI Functions, SWA, etc).
- On the frontend, consume APIs via HttpClient/Fetch, handle errors and loading states.

### Development Flow

- Install dependencies with `dotnet restore`.
- Compile with `dotnet build`.
- Run tests with `dotnet test --logger "console;verbosity=detailed"`.
- Use simulation and integration scripts to validate complete scenarios (e.g., `tests/SmartAlarm-test.sh`).
- On the frontend, use linters (ESLint), formatters (Prettier), and check accessibility (axe, Lighthouse).

### Review and Pull Requests

- Follow the conventional commit format.
- Clearly describe what changed and why.
- Include context, changes, tests performed, and pending items in the PR description.
- On the frontend, review accessibility, responsiveness, and visual impact of changes.

### Observabilidade: Tracing e Métricas

**✅ IMPLEMENTADO**: Todos os handlers e pontos críticos da Application Layer implementam tracing distribuído (OpenTelemetry, Application Insights) e coleta de métricas customizadas.

**Padrões obrigatórios para novos handlers, comandos e queries:**

#### Distributed Tracing (OBRIGATÓRIO)

```csharp
public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken)
{
    using var activity = SmartAlarmTracing.ActivitySource.StartActivity("HandlerName.Handle");
    activity?.SetTag("entity.id", request.Id.ToString());
    activity?.SetTag("operation.type", "create"); // create, read, update, delete, list
    
    try
    {
        // Business logic
        var result = await DoWork(request);
        activity?.SetStatus(ActivityStatusCode.Ok);
        SmartAlarmMetrics.SuccessCounter.Add(1);
        return result;
    }
    catch (Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        SmartAlarmMetrics.ErrorCounter.Add(1);
        throw;
    }
}
```

#### Structured Logging (OBRIGATÓRIO)

```csharp
// Sucesso
_logger.LogInformation("Entity created: {EntityId} for user {UserId}", entity.Id, userId);

// Avisos
_logger.LogWarning("Entity not found: {EntityId}", entityId);

// Erros
_logger.LogError(ex, "Failed to process request for user {UserId}", userId);
```

#### Métricas Customizadas (OBRIGATÓRIO)

```csharp
// Contadores de operações
SmartAlarmMetrics.EntityCreatedCounter.Add(1);
SmartAlarmMetrics.ValidationErrorsCounter.Add(1);

// Histogramas de performance (opcional)
using var timer = SmartAlarmMetrics.HandlerDuration.CreateTimer();
```

#### Code Review Checklist

- [ ] Handler cria activity com nome descritivo
- [ ] Activity inclui tags relevantes (entity.id, user.id, operation.type)
- [ ] Activity status é definido corretamente (Ok/Error)
- [ ] Logs estruturados usam parâmetros ao invés de interpolação
- [ ] Métricas são atualizadas para sucessos e erros
- [ ] Nenhuma informação sensível em logs/traces

**📚 Documentação completa**: Consulte `docs/architecture/observability-patterns.md` para exemplos detalhados e padrões de implementação.

## API Layer - Padrões e Critérios

- Controllers RESTful para todos os recursos MVP
- Middlewares globais: logging (Serilog), tracing (OpenTelemetry), autenticação (JWT), validação (FluentValidation), tratamento de erros (ExceptionHandlingMiddleware)
- Documentação automática via Swagger/OpenAPI
- Resposta de erro padronizada (ErrorResponse)
- Testes xUnit cobrindo sucesso, erro e edge cases (mínimo 80% de cobertura)
- Governança: checklist de PR, ADR atualizado, Memory Bank atualizado

Consulte ADR-005 para detalhes e justificativas.

### Good Practice Examples

#### Asynchronous Function (Backend)

```csharp
public async Task<User> GetUserByIdAsync(Guid id)
{
    if (id == Guid.Empty)
        throw new ArgumentException("ID is required");
    var user = await _userRepository.GetByIdAsync(id);
    if (user == null)
        throw new NotFoundException("User not found");
    return user;
}
```

#### Unit Test (Backend)

```csharp
[Fact]
public async Task Should_ThrowArgumentException_When_IdIsEmpty()
{
    var service = new UserService(...);
    await Assert.ThrowsAsync<ArgumentException>(() => service.GetUserByIdAsync(Guid.Empty));
}
```

#### React Component (Frontend)

```tsx
import React from "react";

type ButtonProps = {
    label: string;
    onClick: () => void;
};

export const Button: React.FC<ButtonProps> = ({ label, onClick }) => (
    <button onClick={onClick} aria-label={label}>
        {label}
    </button>
);
```

#### Component Test (Frontend)

```typescript
import { render, screen, fireEvent } from "@testing-library/react";
import { Button } from "./Button";

test("should call onClick when clicked", () => {
    const onClick = vi.fn();
    render(<Button label="Save" onClick={onClick} />);
    fireEvent.click(screen.getByRole("button"));
    expect(onClick).toHaveBeenCalled();
});
```
