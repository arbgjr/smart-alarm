# Smart Alarm — Active Context

## Current Focus

- Etapa 6 (Serverless & Deploy) concluída para dev/homologação: handlers serverless e integração com OCI Functions prontos para extensão, scripts e estrutura de deploy automatizado presentes, parametrização via KeyVault e variáveis de ambiente padronizada.
- Integração real com OCI Vault, OCI Object Storage e OCI Streaming documentada como stub, aguardando produção/credenciais.
- Todos os fluxos MVP, integrações (RabbitMQ, MinIO, Vault, PostgreSQL), observabilidade e testes validados e documentados.
- ADR-006, observability-patterns.md, README de infraestrutura e docs de planejamento refletem o status real.
- Memory Bank atualizado e consistente.

- Próximos passos: Implementar integração real OCI em produção quando disponível, manter documentação e ADRs atualizados.

## Recent Changes

- Infraestrutura multi-provider (PostgreSQL/Oracle) implementada e validada
- Integrações reais: RabbitMQ, MinIO, Vault, PostgreSQL, observabilidade (OpenTelemetry)
- Testes de integração e unidade validados em Docker
- Documentação Markdown, ADR-003, ADR-004, observability-patterns.md atualizados
- Checklist de governança seguido (owners, documentação, ADR)

## Next Steps

- Iniciar etapa 6 (Serverless & Deploy)

## Active Decisions

- Uso exclusivo de .NET 8.0
- OCI Functions como padrão serverless
- Logging estruturado obrigatório
- Serviços de domínio centralizam regras de negócio e são ponto único de validação
- **Persistência multi-provider:** acesso a dados abstraído por interfaces, com implementações específicas para PostgreSQL (dev/testes) e Oracle (produção), selecionadas via DI/configuração. Decisão registrada em ADR-004.
- Integrações reais de mensageria, storage, keyvault e observabilidade implementadas e testadas
- Todos os testes de integração e unidade devem passar em ambiente dockerizado antes de concluir tarefas críticas.
