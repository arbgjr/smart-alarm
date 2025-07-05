# Smart Alarm — Progress

## Completed Features

- Etapa 6 (Serverless & Deploy) concluída para dev/homologação: handlers serverless e integração com OCI Functions prontos para extensão, scripts e estrutura de deploy automatizado presentes, parametrização via KeyVault e variáveis de ambiente padronizada.
- Integração real com OCI Vault, OCI Object Storage e OCI Streaming documentada como stub, aguardando produção/credenciais.
- Todos os fluxos MVP, integrações (RabbitMQ, MinIO, Vault, PostgreSQL), observabilidade e testes validados e documentados. Etapa 7 (Testes) concluída: cobertura mínima de 80% para código crítico, todos os testes unitários e de integração passando, documentação e checklists atualizados, governança validada.
- ADR-006, observability-patterns.md, README de infraestrutura e docs de planejamento refletem o status real.
- Memory Bank atualizado e consistente.
- Etapa 4 (Infraestrutura) concluída: multi-provider (PostgreSQL dev/testes, Oracle produção), mensageria real (RabbitMQ), storage real (MinIO), KeyVault real (Vault), observabilidade (tracing/métricas OpenTelemetry), docker-compose validado
- Todos os testes de integração e unidade passando em ambiente dockerizado
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
- Etapa 5 (API Layer) concluída: controllers RESTful completos para todos os fluxos MVP, middlewares globais de logging, tracing, autenticação, validação e tratamento de erros, documentação Swagger/OpenAPI atualizada, testes xUnit cobrindo sucesso, erro e edge cases (mínimo 80% de cobertura), checklists de governança e documentação seguidos, ADR-005 registrado

## Pending Items / Next Steps

- Implement main endpoints (AlarmService) — Presentation Layer (API)
- Set up JWT/FIDO2 authentication
- Implement automated tests for handlers, repositories, middleware, and API (min. 80% coverage)
- Test the observability environment with `docker-compose up --build` and validate integration between API, Loki, Jaeger, Prometheus, and Grafana
- Document endpoints and architecture (Swagger/OpenAPI, technical docs)
- Set up CI/CD for build, tests, deploy, and observability validation
- Plan and prioritize business features (alarms, routines, integrations)
- Register decisions and next steps in the Memory Bank
- Atualizar Oracle.ManagedDataAccess para versão oficial .NET 8.0 assim que disponível

## Current Status

- Project in structuring and detailed planning phase
- Infraestrutura de observabilidade e logging pronta para uso
- Pronto para iniciar Presentation Layer (API RESTful)

## Known Issues

- Integration with OCI Functions not yet tested in production
- Definition of external integration contracts pending
- Oracle.ManagedDataAccess com warnings de compatibilidade (aguardando versão oficial)
- No critical issues reported at this time
