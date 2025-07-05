# SmartAlarm.Infrastructure

Esta camada implementa todas as preocupações técnicas e integrações externas do Smart Alarm, seguindo Clean Architecture.

## Decisão Arquitetural Importante (2025-07-05)

> **Abstração Multi-Provider:**
> A infraestrutura implementa repositórios e UnitOfWork desacoplados do domínio, com implementações específicas para cada banco (PostgreSQL para dev/testes, Oracle para produção). A seleção do provider é feita via DI e configuração, conforme definido na [ADR-004](../../docs/architecture/adr-004-repository-abstraction.md).

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

Registrar a infraestrutura no Startup, escolhendo o provider conforme ambiente:

```csharp
// Para produção (Oracle)
services.AddSmartAlarmInfrastructure(configuration);

// Para testes/dev (PostgreSQL)
// (implementar AddSmartAlarmInfrastructurePostgres se necessário)
// services.AddSmartAlarmInfrastructurePostgres(configuration);
```

## Extensão

- Adicione novos serviços em `DependencyInjection.cs`.
- Implemente repositórios específicos para cada banco, seguindo as interfaces do domínio.
- Consulte os READMEs de cada subpasta para detalhes de uso e extensão.

---
