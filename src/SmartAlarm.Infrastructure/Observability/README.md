# Observability Layer (Tracing & Metrics)

Esta pasta contém as abstrações e mocks para tracing (rastreamento distribuído) e métricas customizadas do Smart Alarm.

## Serviços Disponíveis

- `ITracingService` / `MockTracingService`: Interface e mock para rastreamento de operações.
- `IMetricsService` / `MockMetricsService`: Interface e mock para coleta de métricas customizadas.

## Como usar

Os serviços são registrados automaticamente na DI via `DependencyInjection.cs`.

Exemplo de uso:

```csharp
public class MinhaClasse
{
    private readonly ITracingService _tracing;
    public MinhaClasse(ITracingService tracing) { _tracing = tracing; }
    public async Task DoWork() {
        await _tracing.TraceAsync("MinhaOperacao", "Iniciando trabalho...");
    }
}
```

## Extensão

Para produção, implemente integrações reais (ex: OpenTelemetry, Application Insights) e registre na DI.
