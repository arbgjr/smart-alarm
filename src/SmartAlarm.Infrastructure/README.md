# SmartAlarm.Infrastructure

Esta camada implementa todas as preocupações técnicas e integrações externas do Smart Alarm, seguindo Clean Architecture.

## Decisão Arquitetural Importante (2025-07-05)

> **Etapa 4 (Infraestrutura) concluída:**
>
> - Multi-provider: repositórios e UnitOfWork desacoplados do domínio, com implementações específicas para PostgreSQL (dev/testes) e Oracle (produção), selecionados via DI/configuração ([ADR-004](../../docs/architecture/adr-004-repository-abstraction.md)).
> - Mensageria real: RabbitMQ (dev/homologação) implementado e testado; stub para OCI Streaming (produção).
> - Storage real: MinIO (dev/homologação) implementado e testado; stub para OCI Object Storage (produção).
> - KeyVault real: HashiCorp Vault (dev/homologação) implementado e testado; estrutura para OCI Vault/Azure/AWS pronta para extensão.
> - Observabilidade: OpenTelemetry (tracing/métricas), Prometheus, Jaeger, Loki, dashboards e health endpoints validados.
> - Docker compose atualizado e validado para todos os serviços de integração.
> - Todos os testes de integração e unidade passando em ambiente dockerizado.

## Responsabilidades

- Implementa repositórios concretos para interfaces de domínio (InMemory, EF, Dapper).
- Integração real com bancos, mensageria (RabbitMQ), storage (MinIO), keyvault (Vault), logging, tracing, métricas (OpenTelemetry/Prometheus).
- Configuração de DI para todos os serviços de infraestrutura.
- Infraestrutura desacoplada do domínio e aplicação, facilitando testes e substituição.

## Serviços de Infraestrutura

- **Repositórios:** InMemory, Entity Framework, Dapper (multi-provider: PostgreSQL/Oracle).
- **Mensageria:** RabbitMQ (dev/homologação), OCI Streaming (stub prod).
- **Storage:** MinIO (dev/homologação), OCI Object Storage (stub prod).
- **KeyVault:** HashiCorp Vault (dev/homologação), OCI Vault/Azure/AWS (stub prod).
- **Observabilidade:** OpenTelemetry (tracing/métricas), Prometheus, Jaeger, Loki, health endpoints.

## Como usar

Registrar a infraestrutura no Startup, escolhendo o provider conforme ambiente:

```csharp
// Para produção (Oracle)
services.AddSmartAlarmInfrastructure(configuration);

// Para testes/dev (PostgreSQL)
// services.AddSmartAlarmInfrastructurePostgres(configuration);
```

## Extensão

- Adicione novos serviços em `DependencyInjection.cs`.
- Implemente repositórios específicos para cada banco, seguindo as interfaces do domínio.
- Amplie integrações reais de mensageria, storage e keyvault conforme necessidade.
- Consulte os READMEs de cada subpasta para detalhes de uso e extensão.

---
