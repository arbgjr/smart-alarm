# Smart Alarm — Active Context

## Current Focus

- Etapa 4 (Infraestrutura) concluída: multi-provider (PostgreSQL dev/testes, Oracle produção), mensageria real (RabbitMQ), storage real (MinIO), KeyVault real (Vault), observabilidade (tracing/métricas OpenTelemetry), docker-compose validado
- Todos os testes de integração e unidade passando em ambiente dockerizado
- Serviços de domínio implementados e testados (AlarmDomainService, UserDomainService)
- Documentação, ADRs e checklists atualizados
- Estrutura de testes AAA e cobertura mínima de 80% para regras críticas

## Recent Changes

- Infraestrutura multi-provider (PostgreSQL/Oracle) implementada e validada
- Integrações reais: RabbitMQ, MinIO, Vault, PostgreSQL, observabilidade (OpenTelemetry)
- Testes de integração e unidade validados em Docker
- Documentação Markdown, ADR-003, ADR-004, observability-patterns.md atualizados
- Checklist de governança seguido (owners, documentação, ADR)

## Next Steps

- Detalhar endpoints e fluxos de autenticação/autorização
- Implementar handlers e validação com FluentValidation
- Iniciar testes de integração e cobertura de Application Layer

## Active Decisions

- Uso exclusivo de .NET 8.0
- OCI Functions como padrão serverless
- Logging estruturado obrigatório
- Serviços de domínio centralizam regras de negócio e são ponto único de validação
- **Persistência multi-provider:** acesso a dados abstraído por interfaces, com implementações específicas para PostgreSQL (dev/testes) e Oracle (produção), selecionadas via DI/configuração. Decisão registrada em ADR-004.
- Integrações reais de mensageria, storage, keyvault e observabilidade implementadas e testadas
- Todos os testes de integração e unidade devem passar em ambiente dockerizado antes de concluir tarefas críticas.
