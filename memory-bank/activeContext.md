# Smart Alarm — Active Context

## Current Focus

- Infraestrutura multi-provider (PostgreSQL dev/testes, Oracle produção) implementada e validada
- Todos os testes de integração e unidade passando em ambiente dockerizado
- Serviços de domínio implementados e testados (AlarmDomainService, UserDomainService)
- Documentação e ADR dos serviços de domínio concluídos
- Estrutura de testes AAA e cobertura mínima de 80% para regras críticas

## Recent Changes

- Implementação e validação da infraestrutura multi-provider (PostgreSQL/Oracle)
- Todos os testes de integração (RabbitMQ, MinIO, Vault, PostgreSQL) validados em Docker
- Documentação Markdown, ADR-003 e ADR-004 registrados
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
- Todos os testes de integração e unidade devem passar em ambiente dockerizado antes de concluir tarefas críticas.
