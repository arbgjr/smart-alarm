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
