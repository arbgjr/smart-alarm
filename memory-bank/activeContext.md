# Smart Alarm — Active Context

## Current Focus

- Etapa 5 (API Layer) concluída: controllers RESTful completos para todos os fluxos MVP, middlewares globais de logging, tracing, autenticação, validação e tratamento de erros, documentação Swagger/OpenAPI atualizada, testes xUnit cobrindo sucesso, erro e edge cases (mínimo 80% de cobertura), checklists de governança e documentação seguidos, ADR-005 registrado
- Detalhar endpoints e fluxos de autenticação/autorização
- Implementar handlers e validação com FluentValidation
- Iniciar testes de integração e cobertura de Application Layer

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
