
# ADR-006: Serverless Handlers e Deploy Automatizado (OCI Functions)

## Contexto

Para garantir escalabilidade, custo-eficiência e integração nativa com a Oracle Cloud, o SmartAlarm adota OCI Functions como padrão serverless. Handlers são implementados seguindo Clean Architecture, com deploy automatizado e parametrização segura.

## Decisão

- Handlers serverless implementados em C# (.NET 6+), camada dedicada (`Api/Functions`)
- Deploy automatizado via script PowerShell e OCI CLI
- Parametrização via KeyVault e variáveis de ambiente
- Testes unitários e integração obrigatórios
- Documentação e governança conforme checklist

## Status de Implementação (2025-07-05)

- Etapa 6 (Serverless & Deploy) concluída para dev/homologação: handlers serverless e integração com OCI Functions prontos para extensão, scripts e estrutura de deploy automatizado presentes, parametrização via KeyVault e variáveis de ambiente padronizada.
- Integração real com OCI Vault, OCI Object Storage e OCI Streaming documentada como stub, aguardando produção/credenciais.
- Todos os fluxos MVP, integrações (RabbitMQ, MinIO, Vault, PostgreSQL), observabilidade e testes validados e documentados.
- ADR-006, observability-patterns.md, README de infraestrutura e docs de planejamento refletem o status real.
- Memory Bank atualizado e consistente.

**Próximos passos:** Implementar integração real OCI em produção quando disponível, manter documentação e ADRs atualizados.

----

> Esta decisão deve ser revisada a cada evolução significativa de arquitetura cloud ou requisitos de negócio.
