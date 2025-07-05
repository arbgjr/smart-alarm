# Testes Unitários e de Integração - SmartAlarm

Este projeto contém todos os testes automatizados (xUnit, Moq) para o backend do SmartAlarm, incluindo entidades, serviços, handlers, repositórios, controllers, integrações e infraestrutura.

## Critérios de Pronto

- Cobertura mínima de 80% para código crítico
- Testes unitários e de integração para todos os fluxos MVP
- AAA seguido em todos os testes
- Casos de sucesso, erro e edge cases cobertos
- Testes de integração para mensageria, storage, KeyVault, observabilidade e autenticação

## Como executar

1. Execute `dotnet restore` na raiz do projeto.
2. Execute `dotnet test --logger "console;verbosity=detailed"` para rodar todos os testes.
3. Para cobertura, utilize o ReportGenerator e acesse o relatório em `tests/TestCoverageReport/index.html`.

## Observações

- Os testes de integração requerem infraestrutura local (RabbitMQ, Vault, MinIO, PostgreSQL). Use `docker compose up -d --build`.
- Consulte `/docs/development/testes-integracao.md` para detalhes e troubleshooting.

---

Consulte a documentação em `/docs` para detalhes de arquitetura, critérios de pronto e governança.
