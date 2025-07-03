# SmartAlarm.Infrastructure

This layer implements all technical concerns and external integrations for the Smart Alarm system, following Clean Architecture principles.

## Responsibilities
- Implements concrete repositories for domain interfaces (in-memory and, in the future, database-backed).
- Handles integration with external services: databases, messaging, storage, logging, tracing, and metrics.
- Provides dependency injection configuration for all infrastructure services.
- Ensures infrastructure is decoupled from domain and application, allowing easy replacement or mocking for tests.

## Example: Registering Infrastructure in Startup

```csharp
services.AddSmartAlarmInfrastructure();
```

## In-Memory Repositories
- `InMemoryAlarmRepository`: Implements `IAlarmRepository` for development/testing.
- `InMemoryUserRepository`: Implements `IUserRepository`.
- `InMemoryRoutineRepository`: Implements `IRoutineRepository`.
- `InMemoryIntegrationRepository`: Implements `IIntegrationRepository`.

> For production, replace in-memory implementations with database-backed repositories.

## Extending Infrastructure
- Add new services (e.g., logging, tracing, metrics) in `DependencyInjection.cs`.
- Keep all technical concerns isolated from business logic.

---
