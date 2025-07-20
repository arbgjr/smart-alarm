---
title: Smart Alarm Activity Source - Technical Documentation
component_path: src/SmartAlarm.Observability/Tracing/SmartAlarmActivitySource.cs
version: 1.0
date_created: 2025-07-19
last_updated: 2025-07-19
owner: Smart Alarm Development Team
tags: [observability, tracing, telemetry, monitoring, distributed-tracing]
---

# Smart Alarm Activity Source Documentation

The `SmartAlarmActivitySource` provides a centralized, strongly-typed tracing infrastructure for the Smart Alarm system using .NET's native Activity API and OpenTelemetry standards. It enables distributed tracing across microservices with domain-specific activity types and standardized tagging conventions.

## 1. Component Overview

### Purpose/Responsibility
- OVR-001: **Primary Responsibility** - Provide unified distributed tracing infrastructure with domain-specific activity creation, standardized tagging, and OpenTelemetry integration
- OVR-002: **Scope** - Handles activity creation, lifecycle management, and standardized tagging. Does NOT handle trace export, sampling, or storage configuration
- OVR-003: **System Context** - Core observability component consumed across all application layers for performance monitoring, debugging, and operational insights

## 2. Architecture Section

- ARC-001: **Design Patterns Used**
  - **Factory Pattern**: Centralized activity creation with consistent naming and configuration
  - **Builder Pattern**: Fluent interfaces for activity configuration and tag assignment
  - **Template Method Pattern**: Standardized activity types with customizable parameters
  - **Singleton Pattern**: Single ActivitySource instance per application domain
  
- ARC-002: **Dependencies**
  - **External**: System.Diagnostics.Activity - .NET native tracing infrastructure
  - **External**: System.Diagnostics.ActivitySource - Activity creation and management
  - **External**: OpenTelemetry.Api - Distributed tracing standards
  - **Framework**: .NET 8.0+ ActivitySource enhancements

- ARC-003: **Component Interactions**
  - **Application Handlers**: CQRS handlers instrument business operations
  - **Repository Layer**: Data access operations with performance tracking
  - **External Integrations**: Cross-service correlation and dependency tracking
  - **Web Controllers**: HTTP request/response lifecycle tracing
  - **Background Services**: Long-running operations and job processing

- ARC-004: **OpenTelemetry Integration**
  - Compatible with OpenTelemetry exporters (Jaeger, Zipkin, Azure Monitor)
  - Follows OpenTelemetry semantic conventions for consistency
  - Supports distributed context propagation across service boundaries
  - Integrates with OpenTelemetry metrics and logging correlation

### Component Structure and Dependencies Diagram

```mermaid
graph TD
    subgraph "Smart Alarm Tracing Architecture"
        A[SmartAlarmActivitySource] --> B[ActivitySource Instance]
        A --> C[Activity Factory Methods]
        A --> D[Domain-Specific Activities]
        A --> E[Standardized Tags]
    end

    subgraph "Activity Types"
        F[Domain Activities] --> D
        G[Repository Activities] --> D
        H[Integration Activities] --> D
        I[Handler Activities] --> D
        J[Background Activities] --> D
    end

    subgraph "OpenTelemetry Stack"
        K[Activity Listeners]
        L[Trace Exporters]
        M[Sampling Processors]
        N[Context Propagation]
    end

    subgraph "Consuming Components"
        O[CQRS Handlers]
        P[Repository Classes]
        Q[External Integrations]
        R[Web Controllers]
        S[Background Services]
    end

    A --> K
    K --> L
    K --> M
    B --> N
    
    O --> A
    P --> A
    Q --> A
    R --> A
    S --> A

    classDiagram
        class SmartAlarmActivitySource {
            +const string Name
            -ActivitySource _activitySource
            +StartActivity(string, ActivityKind, ActivityContext): Activity?
            +StartDomainActivity(string, string, string?): Activity?
            +StartRepositoryActivity(string, string, string?): Activity?
            +StartIntegrationActivity(string, string, string?): Activity?
            +StartHandlerActivity(string, string): Activity?
            +StartBackgroundActivity(string, string?): Activity?
            +Dispose(): void
        }
        
        class ActivitySource {
            <<system>>
            +Name: string
            +Version: string
            +StartActivity(string, ActivityKind): Activity?
            +HasListeners(): bool
            +Dispose(): void
        }
        
        class Activity {
            <<system>>
            +Id: string
            +DisplayName: string
            +Kind: ActivityKind
            +Status: ActivityStatusCode
            +SetTag(string, string): Activity
            +SetStatus(ActivityStatusCode, string?): Activity
            +Dispose(): void
        }
        
        enum ActivityKind {
            Internal
            Server
            Client
            Producer
            Consumer
        }

        SmartAlarmActivitySource --> ActivitySource
        ActivitySource --> Activity
```

## 3. Interface Documentation

### Constants

| Constant | Value | Purpose | Usage Notes |
|----------|-------|---------|-------------|
| Name | "SmartAlarm" | Activity source identifier | Used for OpenTelemetry configuration and filtering |

### Public Methods

| Method | Purpose | Parameters | Return Type | Usage Notes |
|--------|---------|------------|-------------|-------------|
| StartActivity() | Create generic activity | name: string, kind: ActivityKind, parentContext: ActivityContext | Activity? | Basic activity creation, returns null if no listeners |
| StartDomainActivity() | Create domain operation activity | operationName: string, entityType: string, entityId: string? | Activity? | Standardized domain operation tracing |
| StartRepositoryActivity() | Create data access activity | operationName: string, entityType: string, entityId: string? | Activity? | Database and storage operation tracing |
| StartIntegrationActivity() | Create external integration activity | operationName: string, serviceName: string, endpoint: string? | Activity? | Third-party service interaction tracing |
| StartHandlerActivity() | Create CQRS handler activity | handlerName: string, operationType: string | Activity? | Command and query handler tracing |
| StartBackgroundActivity() | Create background job activity | jobName: string, jobId: string? | Activity? | Long-running task and scheduled job tracing |
| Dispose() | Cleanup resources | None | void | Disposes underlying ActivitySource |

### Standard Tags Applied

| Tag Category | Tag Keys | Purpose | Example Values |
|--------------|----------|---------|----------------|
| Operation | operation, handler, operation.type | Identify operation type | CreateAlarm, GetAlarmById, command |
| Entity | entity.type, entity.id | Domain entity context | Alarm, User, Integration |
| Integration | service.name, service.endpoint | External service details | GoogleCalendar, /api/events |
| Correlation | correlation.id, user.id | Request correlation | uuid, user-guid |
| Performance | database.query, cache.hit | Performance indicators | SELECT * FROM, true/false |

## 4. Implementation Details

- IMP-001: **Main Implementation Classes**
  - `SmartAlarmActivitySource`: Primary factory with domain-specific methods
  - `ActivitySource`: .NET native tracing infrastructure wrapper
  - Activity lifecycle management with proper disposal patterns
  - Tag standardization with consistent naming conventions

- IMP-002: **Configuration and Initialization**
  - Singleton ActivitySource instance with application lifetime
  - Version tracking for compatibility and debugging
  - Integration with DI container for automatic disposal
  - OpenTelemetry listener registration and configuration

- IMP-003: **Key Algorithms and Business Logic**
  - **Activity Creation**: Conditional activity creation based on listener availability
  - **Context Propagation**: Parent-child activity relationships for distributed tracing
  - **Tag Standardization**: Consistent tagging patterns across all activity types
  - **Performance Optimization**: Null activity handling to minimize overhead when tracing disabled

- IMP-004: **Performance Characteristics**
  - **Overhead**: Minimal when no listeners registered (~1-5ns per call)
  - **Memory Usage**: Activity objects created only when listeners present
  - **Throughput**: High-performance operations, optimized for production workloads
  - **Sampling**: Respects OpenTelemetry sampling configuration

## 5. Usage Examples

### Basic Usage

```csharp
public class CreateAlarmHandler : IRequestHandler<CreateAlarmCommand, AlarmResponseDto>
{
    private readonly SmartAlarmActivitySource _activitySource;
    
    public async Task<AlarmResponseDto> Handle(CreateAlarmCommand request, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartHandlerActivity("CreateAlarmHandler", "command");
        activity?.SetTag("user.id", request.Alarm.UserId.ToString());
        activity?.SetTag("alarm.name", request.Alarm.Name);
        
        try
        {
            // Business logic here
            var result = await ProcessAlarm(request);
            
            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
```

### Advanced Usage

```csharp
public class AlarmRepository : IAlarmRepository
{
    private readonly SmartAlarmActivitySource _activitySource;
    private readonly DbContext _context;
    
    public async Task<Alarm?> GetByIdAsync(Guid id)
    {
        using var activity = _activitySource.StartRepositoryActivity("GetById", "Alarm", id.ToString());
        activity?.SetTag("database.operation", "SELECT");
        activity?.SetTag("database.table", "Alarms");
        
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await _context.Alarms.FindAsync(id);
            
            activity?.SetTag("database.result", result != null ? "found" : "not_found");
            activity?.SetTag("database.duration_ms", stopwatch.ElapsedMilliseconds.ToString());
            activity?.SetStatus(ActivityStatusCode.Ok);
            
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("database.error", ex.GetType().Name);
            throw;
        }
    }
}

// Integration service example
public class GoogleCalendarIntegration
{
    private readonly SmartAlarmActivitySource _activitySource;
    
    public async Task<CalendarEvent> CreateEventAsync(CreateEventRequest request)
    {
        using var activity = _activitySource.StartIntegrationActivity("CreateEvent", "GoogleCalendar", "/calendar/v3/events");
        activity?.SetTag("integration.calendar_id", request.CalendarId);
        activity?.SetTag("integration.event_title", request.Title);
        activity?.SetTag("http.method", "POST");
        
        // HTTP call with tracing context propagation
        var response = await httpClient.PostAsync(endpoint, content);
        
        activity?.SetTag("http.status_code", ((int)response.StatusCode).ToString());
        activity?.SetStatus(response.IsSuccessStatusCode ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
        
        return await ProcessResponse(response);
    }
}
```

- USE-001: **Basic Usage**: Simple activity creation with error handling
- USE-002: **Advanced Patterns**: Complex scenarios with performance metrics and external integrations
- USE-003: **Best Practices**: Consistent tagging, proper disposal, status management

## 6. Quality Attributes

- QUA-001: **Security**
  - **Data Protection**: No sensitive data in activity names or tags
  - **Audit Trail**: Comprehensive operation tracking for security analysis
  - **Privacy Compliance**: LGPD/GDPR-aware tagging without personal data
  - **Access Control**: Activity data access controlled by OpenTelemetry configuration

- QUA-002: **Performance**
  - **Low Overhead**: Optimized for high-throughput production environments
  - **Conditional Execution**: Activities created only when listeners present
  - **Memory Efficiency**: Automatic cleanup with using statements
  - **Sampling Support**: Configurable sampling rates for cost control

- QUA-003: **Reliability**
  - **Fault Tolerance**: Graceful handling of tracing failures
  - **Non-Blocking**: Tracing failures don't affect business operations
  - **Context Preservation**: Reliable parent-child activity relationships
  - **Cleanup**: Proper resource disposal prevents memory leaks

- QUA-004: **Maintainability**
  - **Consistent API**: Standardized methods for different operation types
  - **Documentation**: Self-documenting code with clear naming conventions
  - **Testing**: Mockable interface for unit testing scenarios
  - **Configuration**: External configuration for sampling and export settings

- QUA-005: **Extensibility**
  - **Custom Activities**: Easy addition of new domain-specific activity types
  - **Tag Extension**: Flexible tagging system for new requirements
  - **Exporter Support**: Compatible with multiple OpenTelemetry exporters
  - **Integration Points**: Standard interfaces for custom trace processing

## 7. Reference Information

- REF-001: **Dependencies and Versions**
  - .NET 8.0+ System.Diagnostics.Activity
  - OpenTelemetry.Api 1.6+
  - Compatible exporters: Jaeger, Zipkin, Azure Monitor, AWS X-Ray

- REF-002: **Configuration Options**
  ```json
  {
    "OpenTelemetry": {
      "Tracing": {
        "Sources": ["SmartAlarm"],
        "Sampling": {
          "Type": "ParentBased",
          "Probability": 0.1
        },
        "Exporters": {
          "Jaeger": {
            "Endpoint": "http://jaeger:14268/api/traces"
          }
        }
      }
    }
  }
  ```

- REF-003: **Testing Guidelines**
  ```csharp
  [Fact]
  public void StartDomainActivity_WithValidParameters_CreatesActivityWithTags()
  {
      // Arrange
      using var activityListener = new ActivityListener
      {
          ShouldListenTo = _ => true,
          Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
      };
      ActivitySource.AddActivityListener(activityListener);
      
      var source = new SmartAlarmActivitySource();
      
      // Act
      using var activity = source.StartDomainActivity("CreateAlarm", "Alarm", "test-id");
      
      // Assert
      Assert.NotNull(activity);
      Assert.Equal("CreateAlarm", activity.DisplayName);
      Assert.Contains(activity.Tags, t => t.Key == "entity.type" && t.Value == "Alarm");
  }
  ```

- REF-004: **Common Issues and Troubleshooting**
  - **Issue**: Activities not appearing in traces
    - **Solution**: Verify ActivityListener registration and sampling configuration
  - **Issue**: Performance degradation
    - **Solution**: Check sampling rates and disable unnecessary tags
  - **Issue**: Missing correlation between services
    - **Solution**: Ensure proper context propagation in HTTP headers
  - **Issue**: Memory leaks
    - **Solution**: Verify proper disposal of activities with using statements

- REF-005: **Related Documentation**
  - [OpenTelemetry Configuration](../observability/opentelemetry-setup.md)
  - [Distributed Tracing Best Practices](../observability/tracing-guidelines.md)
  - [Monitoring and Alerting](../monitoring/alerting-setup.md)
  - [Performance Optimization](../performance/observability-overhead.md)

- REF-006: **Change History and Migration Notes**
  - **v1.0**: Initial implementation with standard activity types
  - **Future Enhancements**: Custom sampling strategies, additional integrations
  - **Migration Notes**: No breaking changes expected, additive API evolution
