# Observability Layer (Tracing & Metrics)

> Observação: Qualquer referência a mocks/stubs refere-se apenas ao ambiente de desenvolvimento/teste.
> Para produção, utilize sempre as integrações reais conforme documentação.

Esta pasta contém as abstrações, mocks e orientações para tracing (rastreamento distribuído) e métricas customizadas do Smart Alarm.

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

Para produção, implemente integrações reais (ex: OpenTelemetry, Application Insights) e registre na DI do projeto Api.

## Observabilidade no Smart Alarm

- Métricas e tracing reais são expostos via OpenTelemetry e Prometheus no projeto Api.
- Use as interfaces para instrumentar código de infraestrutura e domínio.
- Consulte docs/architecture/observability-patterns.md para padrões e exemplos.
