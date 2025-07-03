# AlarmRepository (Oracle Autonomous DB)

This repository implements `IAlarmRepository` using Oracle Autonomous DB, Dapper, and structured logging (Serilog/Microsoft.Extensions.Logging). It is production-ready and follows Clean Architecture and SOLID principles.

## Dependencies
- Dapper
- Oracle.ManagedDataAccess
- Microsoft.Extensions.Logging.Abstractions

## Configuration
- Add your Oracle DB connection string to `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "OracleDb": "User Id=...;Password=...;Data Source=..."
  }
}
```

- Register infrastructure in your DI setup:

```csharp
services.AddSmartAlarmInfrastructure();
```

## Usage
- The repository is injected via `IAlarmRepository`.
- All operations are async and log errors with context.

## Security
- Never commit secrets. Use environment variables or secret managers.

## Observability
- All errors are logged via ILogger.
- Add tracing/metrics as needed (OpenTelemetry).

## Extensibility
- Extend for other aggregates (User, Routine, Integration) as needed.

---

For more details, see the main project documentation.
