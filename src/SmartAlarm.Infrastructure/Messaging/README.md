# Messaging Layer

> Observação: Qualquer referência a mocks/stubs refere-se apenas ao ambiente de desenvolvimento/teste.
> Para produção, utilize sempre as integrações reais conforme documentação.

Esta pasta contém a abstração, mock e integrações reais para mensageria/eventos do Smart Alarm.

## Serviços Disponíveis

- `IMessagingService` / `MockMessagingService`: Interface e mock para publicação e subscrição de eventos.
- `RabbitMqMessagingService`: Integração real com RabbitMQ (dev/homologação).
- `OciStreamingMessagingService`: Stub para integração futura com OCI Streaming (produção).

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

Para produção, utilize `OciStreamingMessagingService`.
Para dev/homologação, utilize `RabbitMqMessagingService`.
Para testes, utilize `MockMessagingService`.
