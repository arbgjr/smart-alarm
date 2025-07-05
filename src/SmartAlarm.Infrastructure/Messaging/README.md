# Messaging Layer

Esta pasta contém a abstração e mock para mensageria/eventos do Smart Alarm.

## Serviços Disponíveis

- `IMessagingService` / `MockMessagingService`: Interface e mock para publicação e subscrição de eventos.

## Como usar

Os serviços são registrados automaticamente na DI via `DependencyInjection.cs`.

Exemplo de uso:

```csharp
public class MinhaClasse
{
    private readonly IMessagingService _messaging;
    public MinhaClasse(IMessagingService messaging) { _messaging = messaging; }
    public async Task DoWork() {
        await _messaging.PublishEventAsync("alarms", "Alarme disparado!");
    }
}
```

## Extensão

Para produção, implemente integrações reais (ex: Kafka, RabbitMQ, OCI Events) e registre na DI.
