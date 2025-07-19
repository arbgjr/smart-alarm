# Observability Examples – SmartAlarm API

## Tracing e Métricas em Handlers

### Exemplo: Handler com Tracing, Métricas e Logging

```csharp
public class CreateAlarmHandler : IRequestHandler<CreateAlarmCommand, AlarmResponseDto>
{
    private readonly IAlarmRepository _alarmRepository;
    private readonly ILogger<CreateAlarmHandler> _logger;

    public CreateAlarmHandler(IAlarmRepository alarmRepository, ILogger<CreateAlarmHandler> logger)
    {
        _alarmRepository = alarmRepository;
        _logger = logger;
    }

    public async Task<AlarmResponseDto> Handle(CreateAlarmCommand request, CancellationToken cancellationToken)
    {
        using var activity = SmartAlarmTracing.ActivitySource.StartActivity("CreateAlarmHandler.Handle");
        activity?.SetTag("user.id", request.Alarm.UserId.ToString());
        activity?.SetTag("alarm.name", request.Alarm.Name);
        activity?.SetTag("operation.type", "create");

        // Validação e lógica de negócio...
        var alarm = new Alarm(Guid.NewGuid(), request.Alarm.Name!, request.Alarm.Time!.Value, true, request.Alarm.UserId);
        await _alarmRepository.AddAsync(alarm);
        _logger.LogInformation("Alarme criado: {AlarmId}", alarm.Id);
        activity?.SetTag("alarm.id", alarm.Id.ToString());
        activity?.SetStatus(ActivityStatusCode.Ok);
        SmartAlarmMetrics.AlarmsCreatedCounter.Add(1);
        return new AlarmResponseDto { /* ... */ };
    }
}
```

## Expondo Métricas para Prometheus

- O endpoint `/metrics` já está disponível e configurado via OpenTelemetry/Prometheus.
- Consulte o dashboard Prometheus para visualizar contadores, histogramas e métricas customizadas.

## Logging Estruturado

- Todos os logs utilizam Serilog com contexto (ex: AlarmId, UserId, Operation).
- Nunca logar dados sensíveis (senha, token, dados pessoais).

## Referências

- [docs/architecture/observability-patterns.md](../../docs/architecture/observability-patterns.md)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
- [Serilog](https://serilog.net/)
