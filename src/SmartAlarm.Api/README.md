# SmartAlarm.Api

Projeto Web API principal do Smart Alarm.

## Observabilidade
O middleware de observabilidade é registrado no pipeline para logging estruturado, tracing e métricas.

## Como usar o middleware
No arquivo `Program.cs`, adicione:

```csharp
app.UseObservability();
```

Consulte a documentação em `SmartAlarm.Observability` para detalhes.
