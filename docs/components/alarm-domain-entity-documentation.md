---
title: Alarm Domain Entity - Technical Documentation
component_path: src/SmartAlarm.Domain/Entities/Alarm.cs
version: 1.0
date_created: 2025-07-19
last_updated: 2025-07-19
owner: Smart Alarm Development Team
tags: [domain, entity, core-business, ddd, clean-architecture]
---

# Alarm Domain Entity Documentation

The `Alarm` entity is the core domain object in the Smart Alarm system, representing user-configured alarms with associated routines, integrations, and schedules. It follows Domain-Driven Design (DDD) principles and encapsulates the essential business logic for alarm management.

## 1. Component Overview

### Purpose/Responsibility
- OVR-001: **Primary Responsibility** - Encapsulate all business logic and invariants related to alarm configuration, lifecycle management, and associated entities (routines, integrations, schedules)
- OVR-002: **Scope** - Handles alarm creation, modification, enabling/disabling, and management of child collections. Does NOT handle persistence, external notifications, or scheduling execution
- OVR-003: **System Context** - Core domain entity in Clean Architecture pattern, consumed by Application layer handlers and persisted through Infrastructure repositories

## 2. Architecture Section

- ARC-001: **Design Patterns Used**
  - **Domain-Driven Design (DDD)**: Rich domain model with encapsulated business rules
  - **Aggregate Root Pattern**: Manages consistency boundaries for related entities
  - **Value Object Pattern**: Uses `Name` value object for validated naming
  - **Collection Encapsulation**: Private backing fields with read-only public collections
  
- ARC-002: **Dependencies**
  - **Internal**: `SmartAlarm.Domain.ValueObjects.Name` - Validated name representation
  - **Internal**: `SmartAlarm.Domain.Entities.{Routine, Integration, Schedule}` - Child entities
  - **External**: System.Collections.Generic, System.Linq - Collection management
  - **External**: System (Guid, DateTime, ArgumentException) - Basic system types

- ARC-003: **Component Interactions**
  - **Parent-Child Relationships**: Manages collections of Routines, Integrations, and Schedules
  - **Application Layer**: Consumed by CQRS handlers for business operations
  - **Infrastructure Layer**: Persisted via Entity Framework Core repositories
  - **Domain Services**: May interact with domain services for complex business rules

- ARC-004: **Entity Framework Integration**
  - Private parameterless constructor for EF Core materialization
  - Public constructors for domain object creation
  - Read-only collections prevent external modification of child entities

### Component Structure and Dependencies Diagram

```mermaid
graph TD
    subgraph "Domain Layer"
        A[Alarm Entity] --> B[Name Value Object]
        A --> C[Routine Entity]
        A --> D[Integration Entity]
        A --> E[Schedule Entity]
        A --> F[User Entity Reference]
    end

    subgraph "Application Layer"
        G[CreateAlarmHandler] --> A
        H[UpdateAlarmHandler] --> A
        I[AlarmService] --> A
    end

    subgraph "Infrastructure Layer"
        J[AlarmRepository] --> A
        K[EF Core DbContext] --> A
    end

    subgraph "External Dependencies"
        L[System.Guid]
        M[System.DateTime]
        N[System.Collections.Generic]
    end

    A --> L
    A --> M
    A --> N

    classDiagram
        class Alarm {
            +Guid Id
            +Name Name
            +DateTime Time
            +bool Enabled
            +Guid UserId
            +DateTime CreatedAt
            +DateTime? LastTriggeredAt
            +IReadOnlyList~Routine~ Routines
            +IReadOnlyList~Integration~ Integrations
            +IReadOnlyList~Schedule~ Schedules
            +Enable(): void
            +Disable(): void
            +UpdateName(Name): void
            +UpdateTime(DateTime): void
            +AddRoutine(Routine): void
            +RemoveRoutine(Guid): void
            +AddIntegration(Integration): void
            +RemoveIntegration(Guid): void
            +AddSchedule(Schedule): void
            +RemoveSchedule(Guid): void
            +MarkAsTriggered(): void
        }
        
        class Name {
            <<value object>>
            +string Value
            +Name(string)
            +ToString(): string
            +Equals(object): bool
            +GetHashCode(): int
        }
        
        class Routine {
            <<entity>>
            +Guid Id
            +string Name
            +string Action
        }
        
        class Integration {
            <<entity>>
            +Guid Id
            +string Type
            +string Configuration
        }
        
        class Schedule {
            <<entity>>
            +Guid Id
            +DateTime StartDate
            +DateTime? EndDate
            +ScheduleRecurrence Recurrence
        }

        Alarm --> Name
        Alarm --> Routine
        Alarm --> Integration
        Alarm --> Schedule
```

## 3. Interface Documentation

### Public Properties

| Property | Purpose | Type | Access | Usage Notes |
|----------|---------|------|---------|-------------|
| Id | Unique identifier | Guid | Get | Auto-generated if not provided in constructor |
| Name | Validated alarm name | Name (Value Object) | Get | Encapsulates validation logic |
| Time | Scheduled trigger time | DateTime | Get | UTC time representation |
| Enabled | Activation status | bool | Get | Controls whether alarm should trigger |
| UserId | Owner reference | Guid | Get | Required foreign key relationship |
| CreatedAt | Creation timestamp | DateTime | Get | Auto-set to UTC now on creation |
| LastTriggeredAt | Last execution time | DateTime? | Get | Nullable, updated when alarm triggers |
| Routines | Associated routines | IReadOnlyList&lt;Routine&gt; | Get | Immutable collection view |
| Integrations | External integrations | IReadOnlyList&lt;Integration&gt; | Get | Immutable collection view |
| Schedules | Scheduling rules | IReadOnlyList&lt;Schedule&gt; | Get | Immutable collection view |

### Public Methods

| Method | Purpose | Parameters | Return Type | Usage Notes |
|--------|---------|------------|-------------|-------------|
| Enable() | Activate alarm | None | void | Sets Enabled to true |
| Disable() | Deactivate alarm | None | void | Sets Enabled to false |
| UpdateName(Name) | Change alarm name | newName: Name | void | Validates non-null name |
| UpdateTime(DateTime) | Change trigger time | newTime: DateTime | void | Should be UTC time |
| AddRoutine(Routine) | Add associated routine | routine: Routine | void | Prevents duplicates by ID |
| RemoveRoutine(Guid) | Remove routine by ID | routineId: Guid | void | Safe operation if ID not found |
| AddIntegration(Integration) | Add external integration | integration: Integration | void | Prevents duplicates by ID |
| RemoveIntegration(Guid) | Remove integration by ID | integrationId: Guid | void | Safe operation if ID not found |
| AddSchedule(Schedule) | Add scheduling rule | schedule: Schedule | void | Prevents duplicates by ID |
| RemoveSchedule(Guid) | Remove schedule by ID | scheduleId: Guid | void | Safe operation if ID not found |
| MarkAsTriggered() | Update last triggered time | None | void | Sets LastTriggeredAt to current UTC |

## 4. Implementation Details

- IMP-001: **Main Implementation Classes**
  - `Alarm`: Primary aggregate root with encapsulated business logic
  - `Name`: Value object for validated string representation
  - Private backing collections (`List<T>`) with public read-only access

- IMP-002: **Configuration and Initialization**
  - Constructor validation ensures UserId is not empty and Name is not null
  - Automatic ID generation if Guid.Empty provided
  - CreatedAt automatically set to UTC timestamp
  - EF Core constructor provided for materialization

- IMP-003: **Key Algorithms and Business Logic**
  - Collection management prevents duplicate entities by ID comparison
  - Business rule enforcement through method encapsulation
  - Value object pattern ensures name validation at domain level
  - Immutable collections prevent external state corruption

- IMP-004: **Performance Characteristics**
  - **Strengths**: In-memory operations, efficient LINQ queries for small collections
  - **Bottlenecks**: Linear search for duplicate detection in large collections
  - **Memory Usage**: Maintains three separate collections per instance
  - **Recommendations**: Consider indexed lookups for large child collections

## 5. Usage Examples

### Basic Usage

```csharp
// Create a new alarm
var alarmName = new Name("Morning Alarm");
var alarm = new Alarm(
    id: Guid.NewGuid(),
    name: alarmName,
    time: DateTime.UtcNow.AddHours(8), // 8 AM UTC
    enabled: true,
    userId: Guid.Parse("user-id-here")
);

// Basic operations
alarm.Enable();
alarm.UpdateTime(DateTime.UtcNow.AddHours(9)); // Change to 9 AM
```

### Advanced Usage

```csharp
// Create alarm with routines and integrations
var alarm = new Alarm(Guid.NewGuid(), "Work Alarm", 
    DateTime.Today.AddHours(7), true, userId);

// Add a morning routine
var morningRoutine = new Routine(Guid.NewGuid(), "Morning Coffee", "start_coffee_maker");
alarm.AddRoutine(morningRoutine);

// Add Google Calendar integration
var calendarIntegration = new Integration(Guid.NewGuid(), "GoogleCalendar", 
    JsonSerializer.Serialize(new { CalendarId = "primary" }));
alarm.AddIntegration(calendarIntegration);

// Add weekly schedule
var weeklySchedule = new Schedule(Guid.NewGuid(), 
    DateTime.Today, 
    null, // No end date
    ScheduleRecurrence.Weekly);
alarm.AddSchedule(weeklySchedule);

// Business operations
alarm.MarkAsTriggered(); // Update last triggered time
alarm.Disable(); // Temporarily disable
```

- USE-001: **Basic Usage Patterns**: Simple CRUD operations with validation
- USE-002: **Advanced Configuration**: Complex setups with multiple child entities and integrations
- USE-003: **Best Practices**: Always use UTC times, validate inputs, handle nullable LastTriggeredAt appropriately

## 6. Quality Attributes

- QUA-001: **Security**
  - Input validation through constructors and methods
  - UserId association prevents unauthorized access
  - No sensitive data exposure in public interface

- QUA-002: **Performance**
  - **Characteristics**: Fast in-memory operations, efficient for moderate-sized collections
  - **Scalability**: Linear search complexity O(n) for duplicate detection
  - **Resource Usage**: Three collections per instance, moderate memory footprint
  - **Optimization Opportunities**: Indexed lookups for large child collections

- QUA-003: **Reliability**
  - **Error Handling**: Comprehensive argument validation with descriptive exceptions
  - **Fault Tolerance**: Safe operations for non-existent IDs in remove methods
  - **Data Consistency**: Immutable collections prevent external state corruption
  - **Recovery**: No persistent state in entity itself, relies on repository pattern

- QUA-004: **Maintainability**
  - **Standards**: Follows Clean Architecture and DDD principles
  - **Testing**: Clear separation of concerns enables comprehensive unit testing
  - **Documentation**: Self-documenting code with XML comments
  - **Extensibility**: New properties and methods can be added without breaking changes

- QUA-005: **Extensibility**
  - **Extension Points**: Virtual methods for inheritance scenarios
  - **Customization**: Value objects can be replaced with custom implementations
  - **Plugin Architecture**: Integration and Routine entities support diverse configurations
  - **Future Growth**: Additional child entity collections can be added following existing patterns

## 7. Reference Information

- REF-001: **Dependencies and Versions**
  - .NET 8.0+ framework
  - SmartAlarm.Domain.ValueObjects (internal)
  - System.Collections.Generic
  - System.Linq

- REF-002: **Configuration Options**
  - No direct configuration - configured through constructor parameters
  - EF Core configuration handled in Infrastructure layer
  - Validation rules embedded in domain logic

- REF-003: **Testing Guidelines**
  ```csharp
  // Unit test example
  [Fact]
  public void AddRoutine_WithValidRoutine_ShouldAddToCollection()
  {
      // Arrange
      var alarm = new Alarm(Guid.NewGuid(), "Test", DateTime.UtcNow, true, Guid.NewGuid());
      var routine = new Routine(Guid.NewGuid(), "Test Routine", "test_action");
      
      // Act
      alarm.AddRoutine(routine);
      
      // Assert
      Assert.Single(alarm.Routines);
      Assert.Contains(routine, alarm.Routines);
  }
  ```

- REF-004: **Common Issues and Troubleshooting**
  - **Issue**: ArgumentNullException on Name update
    - **Solution**: Ensure Name value object is properly constructed
  - **Issue**: InvalidOperationException on duplicate addition
    - **Solution**: Check if entity ID already exists before adding
  - **Issue**: Time zone confusion
    - **Solution**: Always use UTC times, convert to local in presentation layer

- REF-005: **Related Documentation**
  - [Clean Architecture Overview](../architecture/clean-architecture.md)
  - [Domain-Driven Design Patterns](../architecture/ddd-patterns.md)
  - [Entity Framework Configuration](../infrastructure/ef-configuration.md)
  - [Value Objects Guide](../domain/value-objects.md)

- REF-006: **Change History**
  - **v1.0**: Initial implementation with core alarm functionality
  - **Current**: Stable API, no breaking changes planned
  - **Migration Notes**: N/A for initial version
