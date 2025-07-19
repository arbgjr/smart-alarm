---
mode: "agent"
description: "Develop REST API endpoints following Clean Architecture, JWT authentication, and comprehensive observability"
---

# API Development Prompt

You are an expert .NET 8 API developer working on the Smart Alarm project. Your task is to implement a REST API endpoint following our established patterns and architecture.

## Context & Architecture

**Project**: Smart Alarm - Intelligent alarm and routine management platform
**Stack**: .NET 8, Clean Architecture, Entity Framework Core, MediatR, OpenTelemetry, JWT Authentication
**Deployment**: Serverless-first (OCI Functions), multi-environment (PostgreSQL/Oracle, MinIO/OCI Storage)

## Task Instructions

Create a REST API endpoint with the following requirements:

### 1. Clean Architecture Implementation
- **Domain Layer**: Entities, value objects, domain services, repository interfaces
- **Application Layer**: Command/Query handlers (MediatR), DTOs, validation (FluentValidation)
- **Infrastructure Layer**: EF Core repositories, external integrations, messaging
- **API Layer**: Controllers (routing only), middleware, dependency injection

### 2. Security & Authentication
- Implement JWT authentication with user context
- Add authorization attributes for role-based access
- Include input sanitization and validation
- Follow OWASP security guidelines

### 3. Observability & Monitoring
```csharp
// Required observability pattern
using var activity = _activitySource.StartActivity("OperationName");
activity?.SetTag("user.id", userId.ToString());

_logger.LogInformation("Operation started - CorrelationId: {CorrelationId}",
    _correlationContext.CorrelationId);

_metrics.IncrementCounter("operation_total", "endpoint", "operation_name");
```

### 4. Error Handling & Validation
- Use FluentValidation for input validation
- Implement global exception handling
- Return consistent API responses
- Log errors with correlation IDs

### 5. Documentation & Testing
- Generate OpenAPI/Swagger documentation
- Include XML documentation comments
- Create comprehensive unit tests (xUnit + Moq)
- Add integration tests with TestContainers
- Provide `.http` files for manual testing

## Expected Deliverables

1. **Controller** (API Layer) - Routing and HTTP concerns only
2. **Command/Query** (Application Layer) - Business operation contracts
3. **Handler** (Application Layer) - Business logic implementation
4. **Validator** (Application Layer) - Input validation rules
5. **Tests** - Unit and integration test coverage
6. **Documentation** - API documentation and usage examples

## Example Request Format

```csharp
// Command/Query pattern example
public record CreateAlarmCommand(
    string Name,
    DateTime TriggerTime,
    string? Description
) : IRequest<AlarmResponse>;

// Handler with observability
public class CreateAlarmCommandHandler : IRequestHandler<CreateAlarmCommand, AlarmResponse>
{
    // Implementation with logging, validation, and error handling
}
```

## Quality Standards

- **Test Coverage**: Minimum 90% for business logic
- **Response Time**: < 200ms for standard operations
- **Security**: No exposed sensitive data in logs
- **Maintainability**: Follow SOLID principles
- **Documentation**: Complete OpenAPI specification

Please implement the endpoint following these guidelines and include all necessary files with proper organization.
