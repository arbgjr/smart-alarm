---
applyTo: "**"
---
# Code Generation Instructions

## 1. Core Principles

- **Architecture**: Strictly adhere to **Clean Architecture** and **SOLID** principles
- **Testability**: All code must be unit-testable with comprehensive test coverage (minimum 80%)
- **Security**: Implement security by design - no hardcoded secrets, proper input validation, structured logging without sensitive data
- **Observability**: Include OpenTelemetry tracing, Serilog structured logging, and Prometheus metrics in all services
- **Consistency**: Follow established patterns in `memory-bank/systemPatterns.md` and existing codebase

## 2. .NET 8 Backend Standards

### Layer Architecture
- **Domain**: Pure business logic, entities, value objects, domain services, repository interfaces - no external dependencies
- **Application**: Use cases, command/query handlers (MediatR), DTOs, application services, validation (FluentValidation)
- **Infrastructure**: EF Core repositories, external service integrations, messaging (RabbitMQ), storage (MinIO/OCI), KeyVault providers
- **API**: Controllers, middleware, dependency injection configuration, API documentation (Swagger)

### Code Patterns
- **CQRS with MediatR**: Commands for write operations, Queries for read operations
- **Repository + Unit of Work**: Abstract data access with `IRepository<T>` and `IUnitOfWork`
- **Domain Events**: Publish events after successful persistence for inter-service communication
- **Dependency Injection**: Use appropriate lifetimes (Scoped for most services, Singleton for stateless services)
- **Background Jobs**: Use Hangfire for scheduled tasks and recurring operations

### Validation & Error Handling
- **FluentValidation**: For all command and query input validation
- **Custom Exceptions**: Domain-specific exceptions (e.g., `AlarmNotFoundException`, `InvalidScheduleException`)
- **Global Exception Handling**: Use middleware for consistent API error responses
- **Result Pattern**: Consider using Result<T> for operations that can fail gracefully

## 3. Observability Integration

### Required Observability
```csharp
// Every service method should include:
using var activity = _activitySource.StartActivity("MethodName");
activity?.SetTag("user.id", userId.ToString());

_logger.LogInformation("Operation started for user {UserId} - CorrelationId: {CorrelationId}",
    userId, _correlationContext.CorrelationId);

_meter.IncrementCounter("operation_started", "service", "alarm");
```

### Error Logging
```csharp
catch (Exception ex)
{
    activity?.SetTag("error", true);
    activity?.SetTag("error.message", ex.Message);
    
    _logger.LogError(ex, "Operation failed for user {UserId} - CorrelationId: {CorrelationId}",
        userId, _correlationContext.CorrelationId);
    
    _meter.IncrementErrorCount("service", "operation", "exception");
    throw;
}
```

## 4. Testing Standards

### Unit Tests (xUnit)
- **AAA Pattern**: Arrange, Act, Assert
- **Coverage**: Test success paths, error cases, edge cases, and business rule validation
- **Mocking**: Use Moq for dependencies, avoid mocking value types or concrete classes
- **Naming**: `Should_ExpectedBehavior_When_StateUnderTest`

### Integration Tests
- **Environment**: Use `WebApplicationFactory<T>` for API tests
- **Database**: Use TestContainers or in-memory databases for isolated testing
- **Categories**: Mark with `[Trait("Category", "Integration")]` for selective execution
- **Cleanup**: Ensure tests are isolated and can run in parallel

## 5. Multi-Provider Infrastructure

### Database Configuration
```csharp
// Support PostgreSQL (dev) and Oracle (prod)
var dbProvider = configuration.GetValue<string>("Database:Provider");
if (string.Equals(dbProvider, "PostgreSQL", StringComparison.OrdinalIgnoreCase))
{
    services.AddDbContext<SmartAlarmDbContext>(options => options.UseNpgsql(connectionString));
    services.AddScoped<IAlarmRepository, EfAlarmRepositoryPostgres>();
}
else
{
    services.AddDbContext<SmartAlarmDbContext>(options => options.UseOracle(connectionString));
    services.AddScoped<IAlarmRepository, EfAlarmRepository>();
}
```

### Environment-Based Services
```csharp
// Register services based on environment
services.AddScoped<IStorageService>(provider =>
{
    var environment = provider.GetRequiredService<IConfiguration>()["ASPNETCORE_ENVIRONMENT"];
    return environment switch
    {
        "Production" => new OciObjectStorageService(...),
        _ => new SmartStorageService(...) // Auto-detects MinIO availability
    };
});
```

## 6. Security Requirements

- **Authentication**: JWT with FIDO2 support, token revocation via Redis blacklist
- **Authorization**: RBAC implementation, validate user permissions at service level
- **Secrets Management**: Use `IKeyVaultService` with multi-provider support (Azure, OCI, Vault)
- **Input Validation**: Sanitize and validate all external inputs, prevent injection attacks
- **Logging**: Never log sensitive data (passwords, tokens, PII), use structured logging with correlation IDs

## 7. Performance Optimization

- **Database**: Use `AsNoTracking()` for read-only queries, implement proper indexing
- **Caching**: Use Redis for distributed caching and JWT blacklisting
- **Async/Await**: Use throughout the entire call stack with proper `CancellationToken` support
- **Connection Pooling**: Use `IHttpClientFactory` for external API calls with Polly resilience policies
- **Background Processing**: Use Hangfire for long-running operations to avoid request timeouts

## 8. Command Execution

### .NET Commands
```bash
# Build with error tolerance for CI
dotnet build || true

# Test with detailed logging
dotnet test --logger "console;verbosity=detailed" || true

# Run with environment configuration
dotnet run --environment Development
```

### Docker Commands
```bash
# Start development environment
docker compose -f docker-compose.dev.yml up -d

# Run integration tests
docker compose -f docker-compose.yml up -d --build
dotnet test --filter "Category=Integration"
```

## 9. Documentation Requirements

- **API Documentation**: Auto-generated Swagger/OpenAPI for all controllers
- **Architecture Decision Records**: Document significant decisions in `docs/architecture/`
- **Code Comments**: Use XML documentation for public APIs, inline comments for complex business logic
- **README**: Service-specific README with setup, configuration, and deployment instructions
