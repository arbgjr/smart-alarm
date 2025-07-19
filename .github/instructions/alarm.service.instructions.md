---
applyTo: "services/alarm-service/**"
---
# Alarm Service Instructions

## 1. Core Responsibility

The **Alarm Service** is the authoritative source for all alarm lifecycle management - creation, scheduling, triggering, snoozing, dismissal, and business rule enforcement. It maintains alarm state consistency and publishes domain events for system-wide coordination.

## 2. Key Technologies

- **Framework**: .NET 8, ASP.NET Core
- **Persistence**: Entity Framework Core with multi-provider support (PostgreSQL for dev, Oracle for prod)
- **Background Processing**: Hangfire for alarm scheduling and recurring job management
- **Messaging**: RabbitMQ via `IMessagingService` for domain event publishing
- **Observability**: OpenTelemetry for distributed tracing, Serilog for structured logging, Prometheus metrics
- **Architecture**: Clean Architecture with strict layer separation

## 3. Architectural Patterns

- **Domain-Driven Design**: `Alarm` is the Aggregate Root containing all business logic and invariants
- **CQRS with MediatR**: Command handlers for state changes, Query handlers for data retrieval
- **Repository Pattern**: `IAlarmRepository` abstracts data access with EF Core implementations
- **Unit of Work**: `IUnitOfWork` ensures transaction consistency across operations
- **Event-Driven Architecture**: Publishes domain events (`AlarmTriggeredEvent`, `AlarmCreatedEvent`) after successful persistence
- **Background Jobs**: Hangfire integration for scheduled alarm execution and recurring tasks

## 4. Code Generation Rules

- **Aggregate Operations**: All alarm state changes must go through the `Alarm` aggregate root methods
- **Command Handlers**: Use Application layer handlers for all write operations (e.g., `CreateAlarmCommandHandler`, `SnoozeAlarmCommandHandler`)
- **Query Handlers**: Use separate handlers for read operations with `AsNoTracking()` for performance
- **Domain Events**: Publish events after successful `SaveChangesAsync()` but within the same transaction
- **Business Rules**: Implement all validation and business logic within the Domain layer entities
- **Observability**: Wrap all operations with activities, structured logging with correlation IDs
- **Error Handling**: Use domain-specific exceptions and global error handling middleware

## 5. Domain Event Publishing Pattern

```csharp
// Command Handler Example
public class TriggerAlarmCommandHandler : IRequestHandler<TriggerAlarmCommand>
{
    private readonly IAlarmRepository _alarmRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessagingService _messagingService;
    
    public async Task Handle(TriggerAlarmCommand request, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity("TriggerAlarm");
        
        var alarm = await _alarmRepository.GetByIdAsync(request.AlarmId);
        alarm.Trigger(); // Business logic in aggregate
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        // Publish domain event after successful persistence
        await _messagingService.PublishAsync(new AlarmTriggeredEvent
        {
            AlarmId = alarm.Id,
            UserId = alarm.UserId,
            TriggeredAt = DateTime.UtcNow
        });
    }
}
```

## 6. Hangfire Integration Patterns

```csharp
// Background Job Scheduling
public class ScheduleAlarmCommandHandler : IRequestHandler<ScheduleAlarmCommand>
{
    public async Task Handle(ScheduleAlarmCommand request, CancellationToken cancellationToken)
    {
        var alarm = await _alarmRepository.GetByIdAsync(request.AlarmId);
        
        // Schedule the job
        BackgroundJob.Schedule<IAlarmTriggerService>(
            service => service.TriggerAlarmAsync(alarm.Id),
            alarm.NextTriggerTime);
            
        // For recurring alarms
        if (alarm.IsRecurring)
        {
            RecurringJob.AddOrUpdate<IAlarmTriggerService>(
                $"alarm-{alarm.Id}",
                service => service.TriggerAlarmAsync(alarm.Id),
                alarm.CronExpression);
        }
    }
}
```

## 7. Quality Standards

- **Business Logic**: All alarm rules (time validation, user limits, trigger conditions) in Domain layer
- **Testing**: Unit tests for domain logic, integration tests for database operations and message publishing
- **Performance**: Use `AsNoTracking()` for read queries, implement pagination for user alarm lists
- **Consistency**: Use transactions for operations that span multiple aggregates or external services
- **Observability**: Log all state transitions with correlation IDs, emit custom metrics for alarm operations
- **Security**: Validate user permissions for alarm operations, sanitize all inputs
- **Resilience**: Implement retry policies for external dependencies, handle graceful degradation
