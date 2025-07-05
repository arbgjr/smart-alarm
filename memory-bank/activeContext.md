# Smart Alarm — Active Context

## Current Focus

- Etapa 7 (Testes) concluída: cobertura mínima de 80% para código crítico, todos os testes unitários e de integração passando, documentação e checklists atualizados, governança validada. Todos os fluxos MVP, integrações (RabbitMQ, MinIO, Vault, PostgreSQL), observabilidade e testes validados e documentados. ADRs, READMEs e docs refletem o status real. Memory Bank atualizado e consistente.

## Etapa 8 (Observabilidade e Segurança) concluída em 05/07/2025

Todos os requisitos de observabilidade e segurança implementados, testados e validados:

- Métricas customizadas expostas via Prometheus
- Tracing distribuído ativo (OpenTelemetry)
- Logs estruturados (Serilog) com rastreabilidade
- Autenticação JWT/FIDO2, RBAC, LGPD (consentimento granular, logs de acesso)
- Proteção de dados (AES-256-GCM, TLS 1.3, BYOK, KeyVault)
- Testes unitários e de integração cobrindo todos os fluxos críticos
- Documentação, ADRs, Memory Bank e checklists atualizados
- Validação final via semantic search

Critérios de pronto globais e específicos atendidos. Documentação e governança completas.

- Próximos passos: Implementar integração real OCI em produção quando disponível, manter documentação e ADRs atualizados.

## Recent Changes

- Infraestrutura multi-provider (PostgreSQL/Oracle) implementada e validada
- Integrações reais: RabbitMQ, MinIO, Vault, PostgreSQL, observabilidade (OpenTelemetry)
- Testes de integração e unidade validados em Docker, cobertura mínima de 80% atingida, documentação e governança revisadas.
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
