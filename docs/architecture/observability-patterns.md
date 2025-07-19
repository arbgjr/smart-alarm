# Observability Patterns and Guidelines

**Status:** Todos os padrões, exemplos e integrações de observabilidade (tracing, métricas, logs estruturados) estão implementados, testados e validados conforme etapa 4 do planejamento.

## Overview

This document provides comprehensive guidelines and examples for implementing observability in Smart Alarm services, following distributed tracing, structured logging, and metrics collection patterns.

## Architecture

Smart Alarm uses a layered observability approach:

- **Distributed Tracing**: OpenTelemetry with ActivitySource for request correlation
- **Structured Logging**: Serilog with contextual information
- **Metrics**: Custom counters, histograms, and gauges via OpenTelemetry
- **Monitoring**: Application Insights/OTLP for telemetry export

## Distributed Tracing

### Activity Source Setup

Each service/layer should define its own ActivitySource:

```csharp
// SmartAlarm.Application/SmartAlarmTracing.cs
public static class SmartAlarmTracing
{
    public static readonly ActivitySource ActivitySource = new("SmartAlarm.Application");
}
```

### Handler Tracing Pattern

**✅ REQUIRED**: All handlers must implement distributed tracing:

```csharp
public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken)
{
    using var activity = SmartAlarmTracing.ActivitySource.StartActivity("HandlerName.Handle");
    
    // Add relevant tags
    activity?.SetTag("entity.id", request.Id.ToString());
    activity?.SetTag("user.id", request.UserId.ToString());
    
    try
    {
        // Business logic here
        var result = await DoSomethingAsync(request);
        
        // Add result tags
        activity?.SetTag("result.count", result.Count);
        activity?.SetStatus(ActivityStatusCode.Ok);
        
        // Update metrics
        SmartAlarmMetrics.SuccessCounter.Add(1);
        
        return result;
    }
    catch (ValidationException ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, "Validation failed");
        SmartAlarmMetrics.ValidationErrorsCounter.Add(1);
        throw;
    }
    catch (NotFoundException ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, "Entity not found");
        SmartAlarmMetrics.NotFoundErrorsCounter.Add(1);
        throw;
    }
}
```

### Tagging Standards

**Entity Operations**:

- `entity.id`: The primary identifier of the entity being operated on
- `entity.type`: Type of entity (alarm, user, routine, etc.)
- `operation.type`: create, read, update, delete, list

**User Context**:

- `user.id`: Current user identifier
- `user.role`: User role if applicable

**Request Context**:

- `request.size`: Size of the request (e.g., number of items in batch operations)
- `response.size`: Size of the response

**Business Context**:

- `alarm.name`: Alarm name for alarm operations
- `alarm.enabled`: Whether alarm is enabled
- `validation.errors`: Number of validation errors

## Structured Logging

### Logging Patterns

**✅ REQUIRED**: Use structured logging with meaningful context:

```csharp
// Success scenarios
_logger.LogInformation("Alarm created: {AlarmId} for user {UserId}", alarm.Id, alarm.UserId);
_logger.LogInformation("Retrieved {Count} alarms for user {UserId}", alarms.Count, userId);

// Warning scenarios
_logger.LogWarning("Alarm not found: {AlarmId}", alarmId);
_logger.LogWarning("Validation failed for alarm creation: {@Errors}", validationResult.Errors);

// Error scenarios
_logger.LogError(ex, "Failed to create alarm for user {UserId}", request.UserId);
_logger.LogError(ex, "Database operation failed for alarm {AlarmId}", alarmId);
```

### Log Message Standards

- Use **past tense** for completed actions: "Alarm created", "User authenticated"
- Use **present tense** for ongoing actions: "Creating alarm", "Validating request"
- Include **relevant identifiers**: AlarmId, UserId, RequestId
- Use **structured logging parameters** instead of string interpolation
- **Never log sensitive data**: passwords, tokens, personal information

## Metrics Collection

### Metrics Definition

```csharp
// SmartAlarm.Application/SmartAlarmMetrics.cs
public static class SmartAlarmMetrics
{
    public static readonly Meter Meter = new("SmartAlarm.Application");
    
    // Operation counters
    public static readonly Counter<long> AlarmsCreatedCounter = 
        Meter.CreateCounter<long>("alarms_created", "count", "Total number of alarms created");
    
    // Error counters
    public static readonly Counter<long> ValidationErrorsCounter = 
        Meter.CreateCounter<long>("validation_errors", "count", "Total number of validation errors");
    
    // Performance histograms
    public static readonly Histogram<double> HandlerDuration = 
        Meter.CreateHistogram<double>("handler_duration", "ms", "Duration of handler execution");
}
```

### Metrics Usage Patterns

**✅ REQUIRED**: Update metrics in all handlers:

```csharp
// Success metrics
SmartAlarmMetrics.AlarmsCreatedCounter.Add(1);
SmartAlarmMetrics.AlarmsRetrievedCounter.Add(1);

// Error metrics
SmartAlarmMetrics.ValidationErrorsCounter.Add(1);
SmartAlarmMetrics.NotFoundErrorsCounter.Add(1);

// Performance metrics (optional but recommended)
using var timer = SmartAlarmMetrics.HandlerDuration.CreateTimer();
// Operation happens here automatically
```

## Implementation Examples

### Complete Handler Example

```csharp
public class CreateAlarmHandler : IRequestHandler<CreateAlarmCommand, AlarmResponseDto>
{
    private readonly IAlarmRepository _alarmRepository;
    private readonly IValidator<CreateAlarmDto> _validator;
    private readonly ILogger<CreateAlarmHandler> _logger;

    public CreateAlarmHandler(
        IAlarmRepository alarmRepository, 
        IValidator<CreateAlarmDto> validator, 
        ILogger<CreateAlarmHandler> logger)
    {
        _alarmRepository = alarmRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<AlarmResponseDto> Handle(CreateAlarmCommand request, CancellationToken cancellationToken)
    {
        using var activity = SmartAlarmTracing.ActivitySource.StartActivity("CreateAlarmHandler.Handle");
        activity?.SetTag("user.id", request.Alarm.UserId.ToString());
        activity?.SetTag("alarm.name", request.Alarm.Name);
        activity?.SetTag("operation.type", "create");

        var validationResult = await _validator.ValidateAsync(request.Alarm, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Validation failed for alarm creation: {@Errors}", validationResult.Errors);
            activity?.SetStatus(ActivityStatusCode.Error, "Validation failed");
            SmartAlarmMetrics.ValidationErrorsCounter.Add(1);
            throw new ValidationException(validationResult.Errors.ToString());
        }

        var alarm = new Alarm(Guid.NewGuid(), request.Alarm.Name, request.Alarm.Time, true, request.Alarm.UserId);
        await _alarmRepository.AddAsync(alarm);
        
        _logger.LogInformation("Alarm created: {AlarmId} for user {UserId}", alarm.Id, alarm.UserId);
        activity?.SetTag("alarm.id", alarm.Id.ToString());
        activity?.SetStatus(ActivityStatusCode.Ok);
        SmartAlarmMetrics.AlarmsCreatedCounter.Add(1);

        return new AlarmResponseDto
        {
            Id = alarm.Id,
            Name = alarm.Name,
            Time = alarm.Time,
            Enabled = alarm.Enabled,
            UserId = alarm.UserId
        };
    }
}
```

### Query Handler Example

```csharp
public class GetAlarmByIdHandler : IRequestHandler<GetAlarmByIdQuery, AlarmResponseDto?>
{
    private readonly IAlarmRepository _alarmRepository;
    private readonly ILogger<GetAlarmByIdHandler> _logger;

    public GetAlarmByIdHandler(IAlarmRepository alarmRepository, ILogger<GetAlarmByIdHandler> logger)
    {
        _alarmRepository = alarmRepository;
        _logger = logger;
    }

    public async Task<AlarmResponseDto?> Handle(GetAlarmByIdQuery request, CancellationToken cancellationToken)
    {
        using var activity = SmartAlarmTracing.ActivitySource.StartActivity("GetAlarmByIdHandler.Handle");
        activity?.SetTag("alarm.id", request.Id.ToString());
        activity?.SetTag("operation.type", "read");
        
        var alarm = await _alarmRepository.GetByIdAsync(request.Id);
        if (alarm == null)
        {
            _logger.LogWarning("Alarm not found: {AlarmId}", request.Id);
            activity?.SetStatus(ActivityStatusCode.Error, "Alarm not found");
            SmartAlarmMetrics.NotFoundErrorsCounter.Add(1);
            return null;
        }
        
        _logger.LogInformation("Alarm retrieved: {AlarmId}", alarm.Id);
        activity?.SetTag("alarm.name", alarm.Name);
        activity?.SetTag("alarm.user_id", alarm.UserId.ToString());
        activity?.SetStatus(ActivityStatusCode.Ok);
        SmartAlarmMetrics.AlarmsRetrievedCounter.Add(1);
        
        return new AlarmResponseDto(alarm);
    }
}
```

## Code Review Checklist

### ✅ Distributed Tracing

- [ ] Handler creates an activity with descriptive name
- [ ] Activity includes relevant tags (entity.id, user.id, operation.type)
- [ ] Activity status is set appropriately (Ok for success, Error for failures)
- [ ] Activity tags include business-relevant information
- [ ] No sensitive information in activity tags

### ✅ Structured Logging

- [ ] All operations log start/completion with relevant context
- [ ] Error scenarios are logged with appropriate level (Warning/Error)
- [ ] Log messages use structured parameters instead of string interpolation
- [ ] No sensitive information in log messages
- [ ] Log messages are descriptive and actionable

### ✅ Metrics Collection

- [ ] Success operations increment appropriate counters
- [ ] Error scenarios increment error-specific counters
- [ ] Metrics have descriptive names and units
- [ ] Performance-critical operations use histograms/timers
- [ ] Metrics align with business requirements

### ✅ Error Handling

- [ ] All exceptions are caught and properly logged
- [ ] Activity status reflects the actual outcome
- [ ] Error metrics are updated for each error type
- [ ] Error context is preserved in logs and traces

## Testing Observability

### Unit Test Example

```csharp
[Fact]
public async Task CreateAlarm_ShouldEmitCorrectTelemetry()
{
    // Arrange
    var testActivityListener = new TestActivityListener();
    var handler = new CreateAlarmHandler(_repository, _validator, _logger);
    var command = new CreateAlarmCommand { /* ... */ };

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    var activity = testActivityListener.Activities.Single();
    activity.OperationName.Should().Be("CreateAlarmHandler.Handle");
    activity.GetTagValue("operation.type").Should().Be("create");
    activity.Status.Should().Be(ActivityStatusCode.Ok);
    
    // Verify metrics were updated
    // Verify logs were written
}
```

## Performance Considerations

- **Activity overhead**: Minimal, but avoid excessive tag creation in hot paths
- **Logging overhead**: Use appropriate log levels and structured parameters
- **Metrics overhead**: Counters are lightweight, histograms have more overhead
- **Sampling**: Configure appropriate sampling rates for high-volume operations

## Configuration

### OpenTelemetry Setup

```csharp
// Program.cs
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("SmartAlarm.Api"))
        .AddSource("SmartAlarm.Application")
        .AddSource("SmartAlarm.Observability")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("SmartAlarm.Api"))
        .AddMeter("SmartAlarm.Application")
        .AddAspNetCoreInstrumentation()
        .AddPrometheusExporter());
```

### Serilog Configuration

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "ApplicationInsights",
        "Args": {
          "connectionString": "..."
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithThreadId", "WithMachineName"]
  }
}
```

## Troubleshooting

### Common Issues

1. **Missing Activities**: Ensure ActivitySource is properly registered in DI
2. **No Metrics Data**: Verify meter registration and export configuration
3. **Poor Performance**: Check sampling configuration and reduce excessive tagging
4. **Missing Context**: Ensure proper activity propagation across async boundaries

### Debugging Tips

```csharp
// Add debug listener for development
ActivitySource.AddActivityListener(new ActivityListener
{
    ShouldListenTo = _ => true,
    Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
    ActivityStarted = activity => Console.WriteLine($"Started: {activity.DisplayName}"),
    ActivityStopped = activity => Console.WriteLine($"Stopped: {activity.DisplayName}")
});
```

## Migration Guide

For existing handlers without observability:

1. Add `using var activity = SmartAlarmTracing.ActivitySource.StartActivity("HandlerName.Handle");`
2. Add relevant tags for the operation
3. Set activity status in success/error paths
4. Add appropriate metric increments
5. Ensure structured logging with context
6. Update unit tests to verify observability

This ensures consistent and comprehensive observability across all Smart Alarm services.
