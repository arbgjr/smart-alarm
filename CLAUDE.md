# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Smart Alarm is a serverless-first backend platform for intelligent alarm and routine management, built with accessibility and neurodiversity support in mind. The system is designed for Oracle Cloud Infrastructure (OCI) Functions deployment and follows Clean Architecture principles.

## Architecture

This is a .NET 8 solution following Clean Architecture with strict layer separation:

- **Domain Layer** (`src/SmartAlarm.Domain/`): Business entities, value objects, and domain services
- **Application Layer** (`src/SmartAlarm.Application/`): CQRS handlers, DTOs, validators using MediatR
- **Infrastructure Layer** (`src/SmartAlarm.Infrastructure/`): Data access, external services, multi-provider implementations  
- **API Layer** (`src/SmartAlarm.Api/`): REST controllers, middleware, configuration
- **Microservices** (`services/`): AI Service, Alarm Service, Integration Service
- **Frontend** (`frontend/`): React 18 + TypeScript PWA application

### Key Architectural Patterns

- **CQRS with MediatR**: All business operations are handled through commands/queries
- **Multi-Provider Pattern**: Abstract infrastructure dependencies (Database, Storage, Secrets)
- **Repository + Unit of Work**: Data access abstraction with EF Core
- **Domain Events**: Loose coupling between bounded contexts
- **OpenTelemetry**: Comprehensive observability with tracing and metrics

## Essential Development Commands

### Backend (.NET)

```bash
# Restore and build solution
dotnet restore SmartAlarm.sln
dotnet build SmartAlarm.sln --no-restore

# Run API locally (requires Docker infrastructure)
docker compose up -d  # Start infrastructure services first
dotnet run --project src/SmartAlarm.Api

# Testing
dotnet test --logger "console;verbosity=detailed"                    # All tests
dotnet test --filter Category!=Integration                           # Unit tests only
dotnet test --filter Category=Integration --logger "console;verbosity=detailed"  # Integration tests
dotnet test --collect:"XPlat Code Coverage" --settings tests/coverlet.runsettings  # Coverage

# Integration tests require Docker services
docker compose up -d --build
```

### Frontend (React + TypeScript)

```bash
cd frontend

# Development
npm install
npm run dev          # Vite dev server
npm run build        # Production build
npm run preview      # Preview production build

# Testing
npm test            # Vitest unit tests
npm run test:e2e    # Playwright E2E tests
npm run test:e2e:responsive  # Responsive tests
npm run lint        # ESLint
```

## Multi-Provider Configuration

The system uses environment-based provider selection:

**Development**: PostgreSQL + MinIO + HashiCorp Vault + InMemory providers
**Production**: Oracle + OCI Object Storage + OCI Vault
**Testing**: InMemory providers + Testcontainers

Key environment variables:

- `DATABASE_PROVIDER`: `PostgreSQL` (dev/test) or `Oracle` (prod)
- `KEYVAULT_ENABLED`: `true/false`
- `STORAGE_PROVIDER`: Auto-detected based on environment
- `JWT_BLACKLIST_ENABLED`: `true` (uses Redis for token revocation)

## Essential Code Patterns

### MediatR Handler Structure

All application logic follows this pattern:

```csharp
public class CreateAlarmHandler : IRequestHandler<CreateAlarmCommand, AlarmResponse>
{
    private readonly IValidator<CreateAlarmCommand> _validator;
    private readonly IAlarmRepository _alarmRepository;
    private readonly SmartAlarmActivitySource _activitySource;
    
    public async Task<AlarmResponse> Handle(CreateAlarmCommand request, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity(nameof(CreateAlarmHandler));
        
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);
            
        // Business logic implementation
        // Always include OpenTelemetry tracing and structured logging
    }
}
```

### Repository Pattern

```csharp
// Domain repository interface
public interface IAlarmRepository : IRepository<Alarm>
{
    Task<IEnumerable<Alarm>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
}

// Infrastructure implementation with provider-specific logic
public class AlarmRepository : IAlarmRepository
{
    // EF Core implementation with change tracking and optimized queries
}
```

### FluentValidation Usage

All DTOs and commands must have corresponding validators:

```csharp
public class CreateAlarmCommandValidator : AbstractValidator<CreateAlarmCommand>
{
    public CreateAlarmCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TriggerTime).GreaterThan(DateTime.UtcNow);
    }
}
```

## Testing Requirements

### Test Structure

- **Unit Tests**: Business logic validation with 80%+ coverage target
- **Integration Tests**: Database, external APIs, service interactions (requires Docker)
- **E2E Tests**: User workflows using Playwright
- **Accessibility Tests**: Screen reader navigation, keyboard accessibility

### Test Categories

Use `[Trait("Category", "Integration")]` for tests requiring infrastructure.

Integration tests automatically start required Docker services via Testcontainers.

## Docker and Infrastructure

### Development Environment

```bash
# Start all infrastructure services (PostgreSQL, Redis, RabbitMQ, etc.)
docker compose up -d

# Full development environment with monitoring
docker compose -f docker-compose.dev.yml up -d

# Production infrastructure setup
docker compose -f docker-compose.infrastructure.yml up -d
```

### Key Services

- **PostgreSQL**: Primary development database
- **Redis**: JWT blacklist and distributed caching
- **RabbitMQ**: Message queue for microservices communication
- **HashiCorp Vault**: Development secrets management
- **Prometheus + Grafana**: Metrics and monitoring
- **Jaeger**: Distributed tracing

## Accessibility and Neurodiversity Focus

This project specifically serves neurodivergent users. Key principles:

### Frontend Development

- **Screen Reader Support**: All components must work with assistive technologies
- **Keyboard Navigation**: Complete keyboard accessibility with logical tab order
- **Reduced Cognitive Load**: Simple, predictable interfaces
- **Error Prevention**: Graceful error handling and user guidance

### API Design

- **Graceful Degradation**: APIs must handle partial failures
- **Clear Error Messages**: User-friendly error responses
- **Performance**: Fast response times critical for ADHD users
- **Privacy**: Protect sensitive behavioral and medical data

## Security Requirements

- **JWT Authentication**: With FIDO2 support and Redis blacklist
- **Input Validation**: FluentValidation for all inputs
- **Secrets Management**: Never hardcode secrets, use KeyVault providers
- **LGPD Compliance**: Privacy-by-design data handling
- **Encryption**: Sensitive data encrypted at rest and in transit

## Key Files and Locations

### Configuration

- `src/SmartAlarm.Api/appsettings.json`: Base configuration
- `tests/coverlet.runsettings`: Code coverage settings
- `docker-compose*.yml`: Infrastructure definitions

### Core Domain Models

- `src/SmartAlarm.Domain/Entities/Alarm.cs`: Core alarm entity
- `src/SmartAlarm.Domain/Entities/User.cs`: User management
- `src/SmartAlarm.Domain/ValueObjects/`: Email, Name, TimeConfiguration

### Critical Infrastructure

- `src/SmartAlarm.Infrastructure/KeyVault/`: Multi-provider secrets management
- `src/SmartAlarm.Infrastructure/Security/`: JWT and FIDO2 implementation
- `src/SmartAlarm.Infrastructure/Observability/`: OpenTelemetry setup

### Application Services

- `src/SmartAlarm.Application/Handlers/`: CQRS command/query handlers
- `src/SmartAlarm.Application/Validators/`: FluentValidation rules

## Common Development Tasks

1. **Adding a new API endpoint**: Create command/query → handler → validator → controller
2. **Database changes**: Add migration with `dotnet ef migrations add`
3. **New integration tests**: Use `[Trait("Category", "Integration")]` and Docker setup
4. **Frontend components**: Follow accessibility patterns in existing components
5. **Microservice changes**: Update corresponding service in `services/` directory

## Important Notes

- Always run integration tests with Docker infrastructure running
- Use structured logging with Serilog for all log entries
- Include OpenTelemetry tracing in all business operations
- Follow the established multi-provider pattern for new infrastructure dependencies
- Validate all inputs with FluentValidation
- Consider neurodivergent user experience in all UI/UX decisions
