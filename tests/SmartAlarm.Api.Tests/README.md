# Testes da API - SmartAlarm

Este projeto contém testes automatizados (xUnit) para os controllers da API do SmartAlarm, seguindo o padrão AAA (Arrange, Act, Assert) e cobrindo casos de sucesso, erro e edge cases.

## Como executar

1. Execute `dotnet restore` na raiz do projeto.
2. Execute `dotnet test` na pasta `tests/SmartAlarm.Api.Tests`.

## Cobertura

- AlarmController: autenticação, validação, fluxos principais
- AuthController: login válido e inválido

## Critérios de pronto

- Cobertura mínima de 80% para código crítico
- Testes de integração e unidade para todos os endpoints MVP
- AAA seguido em todos os testes

---

Consulte a documentação em `/docs` para detalhes de arquitetura, endpoints e critérios de pronto.
