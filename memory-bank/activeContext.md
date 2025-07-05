# Smart Alarm — Active Context

## Current Focus

- Serviços de domínio implementados e testados (AlarmDomainService, UserDomainService)
- Documentação e ADR dos serviços de domínio concluídos
- Estrutura de testes AAA e cobertura mínima de 80% para regras críticas

## Recent Changes

- Implementação e testes dos serviços de domínio
- Documentação Markdown e ADR-003 registrados
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
