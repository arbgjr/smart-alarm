
# Observability – Logging, Tracing, and Metrics

## Structured Logging

- Serilog configured in all services
- Logs with context (userId, entityId, traceId)
- Levels: Information, Warning, Error

## Distributed Tracing

- OpenTelemetry with ActivitySource in Handlers and Middlewares
- TraceId propagated in all requests and responses
- Export to Application Insights and Jaeger

## Metrics

- Counters, histograms, and gauges via OpenTelemetry
- Export to Prometheus
- Dashboards in Grafana

## Code Examples

```csharp
// Logging
_logger.LogInformation("Alarm created: {AlarmId}", alarm.Id);

// Tracing
using var activity = SmartAlarmTracing.ActivitySource.StartActivity("CreateAlarmHandler.Handle");
activity?.SetTag("entity.id", alarm.Id);
activity?.SetStatus(ActivityStatusCode.Ok);
```

---

**Status:** Observability documented and implemented according to standards.
