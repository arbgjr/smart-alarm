---
title: CQRS Command Handlers - Technical Documentation
component_path: src/SmartAlarm.Application/Handlers/
version: 1.0
date_created: 2025-07-19
last_updated: 2025-07-19
owner: Smart Alarm Development Team
tags: [application, cqrs, mediatr, handlers, clean-architecture]
---

# CQRS Command Handlers Documentation

The Smart Alarm CQRS Command Handlers implement the Command Query Responsibility Segregation pattern using MediatR, providing a clean separation between command processing and query handling. These handlers serve as the primary entry points for business operations in the application layer, orchestrating domain logic, observability, and cross-cutting concerns.

## 1. Component Overview

### Purpose/Responsibility
- OVR-001: **Primary Responsibility** - Execute business commands through clean, testable handlers that orchestrate domain operations, validation, logging, metrics, and tracing
- OVR-002: **Scope** - Handles command execution, validation orchestration, observability integration, and result transformation. Does NOT contain business logic (delegated to domain) or direct infrastructure concerns
- OVR-003: **System Context** - Application layer coordinators in Clean Architecture, bridging controllers/APIs with domain services and infrastructure through dependency injection

## 2. Architecture Section

- ARC-001: **Design Patterns Used**
  - **Command Query Responsibility Segregation (CQRS)**: Separate command and query handlers
  - **Mediator Pattern**: MediatR for decoupled handler registration and execution
  - **Request-Response Pattern**: Strongly-typed commands with corresponding DTOs
  - **Decorator Pattern**: Validation, logging, and metrics behaviors as pipeline decorations
  - **Unit of Work Pattern**: Transaction boundary management through repositories
  
- ARC-002: **Dependencies**
  - **Core**: MediatR.IRequestHandler for command processing contracts
  - **Domain**: Domain entities, repositories, and domain services
  - **Infrastructure**: Repository implementations, external service interfaces
  - **Observability**: Logging (ILogger), metrics (SmartAlarmMeter), tracing (SmartAlarmActivitySource)
  - **Cross-Cutting**: Correlation context, business metrics, validation services

- ARC-003: **Component Interactions**
  - **Web Controllers**: Receive commands from HTTP endpoints
  - **Domain Layer**: Execute business logic through domain services and entities
  - **Repository Layer**: Persist changes through repository pattern
  - **Validation Pipeline**: FluentValidation integration for request validation
  - **Observability Stack**: Structured logging, metrics collection, distributed tracing

- ARC-004: **Handler Pipeline Flow**
  1. Command validation through FluentValidation behaviors
  2. Correlation context establishment and activity creation
  3. Business logic execution through domain services
  4. Repository operations with transaction management
  5. Metrics collection and performance tracking
  6. Result transformation to response DTOs

### Component Structure and Dependencies Diagram

```mermaid
graph TD
    subgraph "CQRS Handler Architecture"
        A[Web Controller] --> B[MediatR Request]
        B --> C[Validation Behavior]
        C --> D[Command Handler]
        D --> E[Domain Service]
        D --> F[Repository]
        D --> G[External Services]
    end

    subgraph "Handler Components"
        H[CreateAlarmHandler]
        I[UpdateAlarmHandler]
        J[DeleteAlarmHandler]
        K[GetAlarmByIdHandler]
        L[ListAlarmsHandler]
    end

    subgraph "Cross-Cutting Concerns"
        M[ILogger]
        N[SmartAlarmMeter]
        O[SmartAlarmActivitySource]
        P[ICorrelationContext]
        Q[BusinessMetrics]
    end

    subgraph "Domain & Infrastructure"
        R[Domain Entities]
        S[Repository Interfaces]
        T[Repository Implementations]
        U[External Integrations]
    end

    D --> H
    D --> I
    D --> J
    D --> K
    D --> L

    D --> M
    D --> N
    D --> O
    D --> P
    D --> Q

    E --> R
    F --> S
    S --> T
    G --> U

    classDiagram
        class IRequestHandler~TRequest,TResponse~ {
            <<interface>>
            +Handle(TRequest, CancellationToken): Task~TResponse~
        }
        
        class CreateAlarmHandler {
            -IAlarmRepository _repository
            -ILogger~CreateAlarmHandler~ _logger
            -SmartAlarmMeter _meter
            -SmartAlarmActivitySource _activitySource
            -ICorrelationContext _correlationContext
            +Handle(CreateAlarmCommand, CancellationToken): Task~AlarmResponseDto~
        }
        
        class CreateAlarmCommand {
            +CreateAlarmDto Alarm
            +Guid UserId
        }
        
        class AlarmResponseDto {
            +Guid Id
            +string Name
            +DateTime Time
            +bool Enabled
            +DateTime CreatedAt
        }
        
        class ValidationBehavior~TRequest,TResponse~ {
            -IEnumerable~IValidator~TRequest~~ _validators
            +Handle(TRequest, RequestHandlerDelegate~TResponse~, CancellationToken): Task~TResponse~
        }

        IRequestHandler --> CreateAlarmHandler
        CreateAlarmHandler --> CreateAlarmCommand
        CreateAlarmHandler --> AlarmResponseDto
        ValidationBehavior --> IRequestHandler
```

## 3. Interface Documentation

### Handler Types and Responsibilities

| Handler | Command/Query | Purpose | Response Type | Usage Notes |
|---------|---------------|---------|---------------|-------------|
| CreateAlarmHandler | Command | Create new alarm | AlarmResponseDto | Validates business rules, creates domain entity |
| UpdateAlarmHandler | Command | Modify existing alarm | AlarmResponseDto | Updates specific fields, preserves audit trail |
| DeleteAlarmHandler | Command | Remove alarm | bool | Soft delete with validation |
| GetAlarmByIdHandler | Query | Retrieve single alarm | AlarmResponseDto? | Returns null if not found or unauthorized |
| ListAlarmsHandler | Query | Retrieve alarm collection | PaginatedResult&lt;AlarmResponseDto&gt; | Supports filtering, sorting, pagination |

### Standard Handler Structure

| Component | Purpose | Type | Implementation Notes |
|-----------|---------|------|---------------------|
| Constructor | Dependency injection | DI Container | Repository, logger, metrics, tracing services |
| Handle Method | Main execution logic | async Task&lt;TResponse&gt; | Primary handler implementation with observability |
| Activity Creation | Distributed tracing | using statement | Creates scoped activity with standardized tags |
| Error Handling | Exception management | try/catch blocks | Logs errors, updates activity status, preserves context |
| Metrics Recording | Performance tracking | Method calls | Records duration, success/failure rates, business metrics |

### Common Handler Dependencies

| Dependency | Interface | Purpose | Usage Pattern |
|------------|-----------|---------|---------------|
| Repository | I{Entity}Repository | Data access | Injected via constructor, async operations |
| Logger | ILogger&lt;T&gt; | Structured logging | Template-based logging with correlation IDs |
| Meter | SmartAlarmMeter | Technical metrics | Duration, count, success rate measurements |
| Activity Source | SmartAlarmActivitySource | Distributed tracing | Scoped activities with standardized tags |
| Correlation Context | ICorrelationContext | Request correlation | Cross-service tracking and audit trails |
| Business Metrics | BusinessMetrics | Domain metrics | Business-specific measurements and KPIs |

## 4. Implementation Details

- IMP-001: **Handler Structure Pattern**
  - Constructor injection for all dependencies
  - Single Handle method implementing IRequestHandler&lt;TRequest, TResponse&gt;
  - Activity creation with using statement for proper disposal
  - Structured logging with correlation IDs and operation context
  - Exception handling with activity status updates

- IMP-002: **Observability Integration**
  - **Logging**: Template-based structured logging with LogTemplates class
  - **Metrics**: Technical metrics (duration, counts) and business metrics (KPIs)
  - **Tracing**: Activity creation with standardized tags for correlation
  - **Correlation**: Request correlation across service boundaries

- IMP-003: **Validation and Error Handling**
  - FluentValidation pipeline behavior for request validation
  - Domain-specific validation through domain services
  - Structured exception handling with appropriate HTTP status codes
  - Activity status tracking (Ok, Error) for tracing correlation

- IMP-004: **Performance Characteristics**
  - **Response Time**: Typically 50-200ms for simple operations
  - **Throughput**: Horizontal scaling through stateless design
  - **Resource Usage**: Memory efficient with proper disposal patterns
  - **Database Optimization**: Repository pattern enables query optimization

## 5. Usage Examples

### Basic Handler Implementation

```csharp
public class CreateAlarmHandler : IRequestHandler<CreateAlarmCommand, AlarmResponseDto>
{
    private readonly IAlarmRepository _alarmRepository;
    private readonly ILogger<CreateAlarmHandler> _logger;
    private readonly SmartAlarmMeter _meter;
    private readonly SmartAlarmActivitySource _activitySource;
    private readonly ICorrelationContext _correlationContext;

    public CreateAlarmHandler(
        IAlarmRepository alarmRepository,
        ILogger<CreateAlarmHandler> logger,
        SmartAlarmMeter meter,
        SmartAlarmActivitySource activitySource,
        ICorrelationContext correlationContext)
    {
        _alarmRepository = alarmRepository;
        _logger = logger;
        _meter = meter;
        _activitySource = activitySource;
        _correlationContext = correlationContext;
    }

    public async Task<AlarmResponseDto> Handle(CreateAlarmCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = _correlationContext.CorrelationId;
        
        _logger.LogInformation(LogTemplates.CommandStarted, 
            nameof(CreateAlarmCommand), correlationId, request.UserId);

        using var activity = _activitySource.StartHandlerActivity("CreateAlarmHandler", "command");
        activity?.SetTag("user.id", request.UserId.ToString());
        activity?.SetTag("alarm.name", request.Alarm.Name);
        activity?.SetTag("correlation.id", correlationId);

        try
        {
            // Domain logic
            var alarm = new Alarm(Guid.NewGuid(), request.Alarm.Name, 
                request.Alarm.Time.Value, true, request.UserId);
            
            await _alarmRepository.AddAsync(alarm);
            
            // Success metrics and logging
            activity?.SetStatus(ActivityStatusCode.Ok);
            _meter.IncrementAlarmCount("created", request.UserId.ToString());
            _meter.RecordAlarmCreationDuration(stopwatch.ElapsedMilliseconds, "standard", true);
            
            _logger.LogInformation(LogTemplates.CommandCompleted, 
                nameof(CreateAlarmCommand), correlationId, stopwatch.ElapsedMilliseconds);

            return new AlarmResponseDto
            {
                Id = alarm.Id,
                Name = alarm.Name.Value,
                Time = alarm.Time,
                Enabled = alarm.Enabled,
                CreatedAt = alarm.CreatedAt
            };
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _meter.IncrementAlarmCount("creation_failed", request.UserId.ToString());
            _meter.RecordAlarmCreationDuration(stopwatch.ElapsedMilliseconds, "standard", false);
            
            _logger.LogError(ex, LogTemplates.CommandFailed, 
                nameof(CreateAlarmCommand), correlationId, ex.Message);
            throw;
        }
    }
}
```

### Advanced Handler with Business Logic

```csharp
public class UpdateAlarmHandler : IRequestHandler<UpdateAlarmCommand, AlarmResponseDto>
{
    private readonly IAlarmRepository _alarmRepository;
    private readonly IAlarmDomainService _domainService;
    private readonly ILogger<UpdateAlarmHandler> _logger;
    private readonly SmartAlarmActivitySource _activitySource;

    public async Task<AlarmResponseDto> Handle(UpdateAlarmCommand request, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartHandlerActivity("UpdateAlarmHandler", "command");
        activity?.SetTag("alarm.id", request.AlarmId.ToString());
        activity?.SetTag("user.id", request.UserId.ToString());

        try
        {
            // Retrieve existing alarm
            var alarm = await _alarmRepository.GetByIdAsync(request.AlarmId);
            if (alarm == null || alarm.UserId != request.UserId)
            {
                throw new AlarmNotFoundException($"Alarm {request.AlarmId} not found");
            }

            // Apply domain business rules
            await _domainService.ValidateAlarmUpdateAsync(alarm, request.Updates);

            // Update alarm properties
            if (!string.IsNullOrEmpty(request.Updates.Name))
                alarm.UpdateName(new Name(request.Updates.Name));
            
            if (request.Updates.Time.HasValue)
                alarm.UpdateTime(request.Updates.Time.Value);

            if (request.Updates.Enabled.HasValue)
            {
                if (request.Updates.Enabled.Value)
                    alarm.Enable();
                else
                    alarm.Disable();
            }

            // Persist changes
            await _alarmRepository.UpdateAsync(alarm);
            
            activity?.SetStatus(ActivityStatusCode.Ok);
            _logger.LogInformation("Alarm {AlarmId} updated successfully", request.AlarmId);

            return MapToResponseDto(alarm);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Failed to update alarm {AlarmId}: {Error}", request.AlarmId, ex.Message);
            throw;
        }
    }
}
```

- USE-001: **Standard Pattern**: Consistent handler structure with observability integration
- USE-002: **Complex Operations**: Multi-step operations with domain service integration
- USE-003: **Error Handling**: Comprehensive exception management with context preservation

## 6. Quality Attributes

- QUA-001: **Security**
  - **Authorization**: User context validation for all operations
  - **Input Validation**: FluentValidation pipeline prevents malicious input
  - **Audit Logging**: Comprehensive operation tracking with correlation IDs
  - **Data Protection**: No sensitive data in logs or trace contexts

- QUA-002: **Performance**
  - **Response Time**: Optimized for 95th percentile under 200ms
  - **Throughput**: Stateless handlers enable horizontal scaling
  - **Resource Efficiency**: Proper disposal patterns prevent memory leaks
  - **Caching Strategy**: Repository-level caching for frequently accessed data

- QUA-003: **Reliability**
  - **Error Handling**: Comprehensive exception handling with context preservation
  - **Transaction Safety**: Repository pattern ensures data consistency
  - **Fault Tolerance**: Graceful handling of downstream service failures
  - **Recovery**: Detailed logging enables rapid incident resolution

- QUA-004: **Maintainability**
  - **Consistent Structure**: Standardized handler patterns across all operations
  - **Clear Separation**: Clean Architecture principles with explicit dependencies
  - **Testability**: Mockable interfaces enable comprehensive unit testing
  - **Documentation**: Self-documenting code with clear naming conventions

- QUA-005: **Extensibility**
  - **New Handlers**: Simple addition following established patterns
  - **Pipeline Behaviors**: Easy integration of cross-cutting concerns
  - **Custom Validation**: Extensible validation pipeline
  - **Metrics Integration**: Pluggable metrics and observability components

## 7. Reference Information

- REF-001: **Dependencies and Versions**
  - MediatR 12.0+ for command/query handling
  - FluentValidation 11.0+ for input validation
  - Microsoft.Extensions.Logging for structured logging
  - OpenTelemetry.Api for distributed tracing

- REF-002: **Handler Registration Configuration**
  ```csharp
  // Program.cs or Startup.cs
  services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateAlarmHandler).Assembly));
  services.AddValidatorsFromAssembly(typeof(CreateAlarmCommandValidator).Assembly);
  services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
  services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
  ```

- REF-003: **Testing Guidelines**
  ```csharp
  [Fact]
  public async Task Handle_WithValidCommand_CreatesAlarm()
  {
      // Arrange
      var mockRepository = new Mock<IAlarmRepository>();
      var mockLogger = new Mock<ILogger<CreateAlarmHandler>>();
      var mockMeter = new Mock<SmartAlarmMeter>();
      var mockActivitySource = new Mock<SmartAlarmActivitySource>();
      var mockCorrelationContext = new Mock<ICorrelationContext>();
      
      var handler = new CreateAlarmHandler(
          mockRepository.Object,
          mockLogger.Object,
          mockMeter.Object,
          mockActivitySource.Object,
          mockCorrelationContext.Object);
      
      var command = new CreateAlarmCommand
      {
          Alarm = new CreateAlarmDto { Name = "Test Alarm", Time = DateTime.UtcNow },
          UserId = Guid.NewGuid()
      };
      
      // Act
      var result = await handler.Handle(command, CancellationToken.None);
      
      // Assert
      Assert.NotNull(result);
      Assert.Equal("Test Alarm", result.Name);
      mockRepository.Verify(r => r.AddAsync(It.IsAny<Alarm>()), Times.Once);
  }
  ```

- REF-004: **Common Issues and Troubleshooting**
  - **Issue**: Handler not found exception
    - **Solution**: Verify MediatR registration and handler assembly scanning
  - **Issue**: Validation failures not handled
    - **Solution**: Check ValidationBehavior registration and validator implementations
  - **Issue**: Correlation ID missing in logs
    - **Solution**: Ensure ICorrelationContext is properly registered and middleware configured
  - **Issue**: Performance degradation
    - **Solution**: Review repository queries, check for N+1 problems, optimize database access

- REF-005: **Related Documentation**
  - [Clean Architecture Overview](../architecture/clean-architecture.md)
  - [CQRS Pattern Implementation](../patterns/cqrs-implementation.md)
  - [Validation Pipeline Setup](../validation/fluent-validation.md)
  - [Observability Integration](../observability/handler-instrumentation.md)

- REF-006: **Change History and Migration Notes**
  - **v1.0**: Initial CQRS handler implementation with MediatR
  - **Current**: Stable API with comprehensive observability integration
  - **Future Enhancements**: Background processing integration, caching behaviors
  - **Migration Notes**: No breaking changes expected, handlers follow additive pattern
