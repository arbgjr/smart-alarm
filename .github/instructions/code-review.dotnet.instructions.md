---
applyTo: "**/*.{cs,csproj,sln,props,targets}"
---
# .NET Code Review Instructions

## 1. Architecture & Design Compliance

### Clean Architecture Validation
- **Layer Boundaries**: Verify Domain layer has no external dependencies, Application layer doesn't reference API layer
- **Dependency Direction**: Dependencies must point inward (API → Application → Domain, Infrastructure → Application/Domain)
- **SOLID Principles**: Check Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion
- **Domain Logic**: Business rules and invariants must be in Domain entities/services, not in Application or Infrastructure layers

### Design Patterns
- **CQRS with MediatR**: Commands for writes, Queries for reads, proper handler separation
- **Repository + Unit of Work**: Abstract data access, ensure proper transaction management
- **Domain Events**: Events published after successful persistence, not before
- **Dependency Injection**: Correct lifetimes (Scoped for most services, Singleton for stateless)

## 2. Code Quality & Performance

### Asynchronous Programming
- **Async All the Way**: `async/await` used throughout the call stack from controller to repository
- **CancellationToken**: Passed down through all async operations
- **ConfigureAwait**: Not needed in ASP.NET Core applications
- **Avoid Blocking**: No `.Wait()`, `.Result`, or `GetAwaiter().GetResult()`

### Entity Framework Best Practices
- **Query Optimization**: `AsNoTracking()` for read-only queries, proper `Include()` usage to prevent N+1
- **Projection**: Use `Select()` to return only needed data, avoid loading full entities unnecessarily
- **Batch Operations**: Use `AddRange()`, `UpdateRange()` for multiple entities
- **Connection Management**: DbContext properly scoped, no manual connection handling

### Memory & Performance
- **Object Allocation**: Minimize unnecessary object creation, use `StringBuilder` for string concatenation
- **Collection Usage**: Prefer `IEnumerable<T>` for parameters, `List<T>` for implementation when indexing needed
- **Caching**: Appropriate use of caching for expensive operations, proper cache invalidation

## 3. Security & Data Protection

### Authentication & Authorization
- **Endpoint Protection**: All controllers have `[Authorize]` or explicit `[AllowAnonymous]`
- **Permission Validation**: User permissions checked at service level, not just controller level
- **JWT Security**: Token validation includes expiration, signature verification, and blacklist checking

### Data Security
- **Input Validation**: All external inputs validated using FluentValidation
- **SQL Injection Prevention**: Use parameterized queries, EF Core prevents SQL injection by default
- **Sensitive Data**: No secrets, passwords, or PII in logs or error messages
- **HTTPS Only**: All endpoints require HTTPS in production

## 4. Observability & Monitoring

### Structured Logging
- **Correlation IDs**: All log entries include correlation context for request tracing
- **Log Levels**: Appropriate use of Debug, Information, Warning, Error levels
- **Structured Data**: Use structured logging with named parameters, not string interpolation
- **No Sensitive Data**: Never log passwords, tokens, or personally identifiable information

### Distributed Tracing
- **Activity Creation**: OpenTelemetry activities for all significant operations
- **Tag Setting**: Relevant tags set on activities (user.id, operation, etc.)
- **Error Handling**: Activities marked with error status when exceptions occur

### Metrics Collection
- **Custom Metrics**: Business-relevant counters, histograms, and gauges implemented
- **Performance Metrics**: Request duration, error rates, throughput measurements
- **Health Checks**: Proper health checks for dependencies (database, external services)

## 5. Error Handling & Resilience

### Exception Management
- **Custom Exceptions**: Domain-specific exceptions that provide meaningful error context
- **Global Error Handling**: Middleware handles unhandled exceptions consistently
- **Error Responses**: Consistent error response format following RFC 7807 Problem Details
- **Logging Context**: Exceptions logged with full context but no sensitive data

### Resilience Patterns
- **Retry Logic**: Polly policies for transient failures in external service calls
- **Circuit Breaker**: Protection against cascading failures
- **Timeout Handling**: Appropriate timeouts for all external operations
- **Graceful Degradation**: System continues to function when non-critical services fail

## 6. Testing Requirements

### Unit Test Quality
- **AAA Pattern**: Arrange, Act, Assert structure consistently followed
- **Test Coverage**: Critical business logic covered, aim for 80% code coverage
- **Mock Usage**: Dependencies properly mocked using Moq, avoid mocking concrete classes
- **Test Naming**: Descriptive names following `Should_ExpectedBehavior_When_StateUnderTest` pattern

### Integration Test Standards
- **Database Isolation**: Tests use separate databases or proper cleanup between tests
- **External Dependencies**: Use TestContainers or mocked services for external dependencies
- **End-to-End Flows**: Critical business workflows tested from API to database
- **Performance Testing**: Response time requirements validated

## 7. Configuration & Deployment

### Configuration Management
- **Environment Separation**: Different configurations for Development, Staging, Production
- **Secret Management**: All sensitive configuration values loaded from KeyVault
- **Connection Strings**: Database connections configured per environment
- **Feature Flags**: Configuration-driven feature toggles where appropriate

### Deployment Readiness
- **Health Endpoints**: `/health` and `/health/ready` endpoints implemented
- **Graceful Shutdown**: Application handles shutdown signals properly
- **Database Migrations**: EF Core migrations included and tested
- **Container Compatibility**: Application runs correctly in containerized environments

## 8. Documentation & Maintenance

### API Documentation
- **Swagger/OpenAPI**: Complete API documentation with request/response examples
- **XML Comments**: Public APIs documented with proper XML documentation
- **Endpoint Descriptions**: Clear descriptions of what each endpoint does
- **Status Codes**: All possible HTTP status codes documented

### Code Maintainability
- **Naming Conventions**: PascalCase for public members, camelCase for private members
- **Code Comments**: Complex business logic explained with inline comments
- **Method Size**: Methods focused on single responsibility, typically under 20-30 lines
- **Class Cohesion**: Related functionality grouped together, unrelated concerns separated

## Review Checklist

- [ ] Clean Architecture layers respected
- [ ] Proper async/await usage throughout
- [ ] Input validation implemented
- [ ] Observability (logging, tracing, metrics) included
- [ ] Error handling comprehensive
- [ ] Unit tests covering critical paths
- [ ] No hardcoded secrets or sensitive data
- [ ] Performance considerations addressed
- [ ] Security best practices followed
- [ ] Documentation updated where needed
