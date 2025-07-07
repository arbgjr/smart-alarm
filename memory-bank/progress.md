# Smart Alarm — Progress

## Completed Features

- Initial folder and project structure
- Base documentation created
- Architecture standards defined
- Observability middleware (logging, tracing, metrics)
- Docker and docker-compose for API, Loki, Jaeger, Prometheus, Grafana
- Global logging (Serilog) and tracing (OpenTelemetry) configured in API
- All backend projects migrated to .NET 8.0
- AlarmRepository (Oracle DB) implemented na Infrastructure Layer
- NuGet restore e build padronizados para .NET 8.0
- Test automation script (run-tests.ps1) criado
- **AlarmService endpoints implementados (CRUD completo via AlarmController)**
- **Handlers para criação, atualização, exclusão, listagem e consulta de alarmes implementados**
- **Validação com FluentValidation aplicada nos comandos e DTOs**
- **Logging estruturado e métricas em todos os handlers principais**

## Pending Items / Next Steps

- Set up JWT/FIDO2 authentication
- Implement automated tests for handlers, repositories, middleware, and API (min. 80% coverage)
- Test the observability environment with `docker-compose up --build` e validar integração entre API, Loki, Jaeger, Prometheus, Grafana
- Documentar endpoints e arquitetura (Swagger/OpenAPI, docs técnicas)
- Set up CI/CD para build, testes, deploy e validação de observabilidade
- Planejar e priorizar features de negócio (alarmes, rotinas, integrações)
- Registrar decisões e próximos passos no Memory Bank
- Atualizar Oracle.ManagedDataAccess para versão oficial .NET 8.0 assim que disponível

## Current Status

- Endpoints principais do AlarmService implementados e handlers funcionais
- Projeto pronto para iniciar testes automatizados e integração de autenticação
- Infraestrutura de observabilidade e logging pronta para uso

## Known Issues

- Integração com OCI Functions ainda não testada em produção
- Definição dos contratos de integração externa pendente
- Oracle.ManagedDataAccess com warnings de compatibilidade (aguardando versão oficial)
- Nenhum bug crítico reportado até o momento
