# Smart Alarm — Active Context

## Current Focus
- Implementação dos endpoints principais do AlarmService (CRUD)
- Handlers e validação com FluentValidation
- Estruturação de logging e métricas nos fluxos críticos
- Preparação para testes automatizados e integração de autenticação JWT/FIDO2

## Recent Changes
- AlarmController implementado com endpoints RESTful (Create, List, GetById, Update, Delete)
- Handlers para criação, atualização, exclusão, listagem e consulta de alarmes
- Validação com FluentValidation aplicada nos comandos e DTOs
- Logging estruturado e métricas em todos os handlers principais

## Next Steps
- Implementar autenticação JWT/FIDO2
- Iniciar testes automatizados (xUnit, Moq, cobertura mínima 80%)
- Documentar endpoints e arquitetura (Swagger/OpenAPI)
- Validar integração de observabilidade (Loki, Jaeger, Prometheus, Grafana)

## Active Decisions
- Exclusivo uso de .NET 8.0
- OCI Functions como padrão serverless
- Logging estruturado obrigatório
- Validação obrigatória com FluentValidation em todos os comandos/DTOs
- Cobertura mínima de testes para código crítico
