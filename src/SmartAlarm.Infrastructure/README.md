# SmartAlarm.Infrastructure

Esta camada implementa todas as preocupações técnicas e integrações externas do Smart Alarm, seguindo Clean Architecture.

## Responsabilidades

- Implementa repositórios concretos para interfaces de domínio (InMemory, EF, Dapper).
- Integração com serviços externos: bancos, mensageria, storage, logging, tracing, métricas, keyvault.
- Configuração de DI para todos os serviços de infraestrutura.
- Infraestrutura desacoplada do domínio e aplicação, facilitando testes e substituição.

## Serviços de Infraestrutura

- **Repositórios:** InMemory, Entity Framework, Dapper.
- **Mensageria:** Mock, RabbitMQ (dev), OCI Streaming (stub prod).
- **Storage:** Mock, MinIO (dev), OCI Object Storage (stub prod).
- **KeyVault:** Mock, HashiCorp Vault (dev), OCI Vault/Azure/AWS (stub prod).
- **Observabilidade:** Mock, real via OpenTelemetry no projeto Api.

## Como usar

Registrar infraestrutura no Startup:

```csharp
services.AddSmartAlarmInfrastructure(configuration);
```

## Extensão

- Adicione novos serviços em `DependencyInjection.cs`.
- Consulte os READMEs de cada subpasta para detalhes de uso e extensão.

---
