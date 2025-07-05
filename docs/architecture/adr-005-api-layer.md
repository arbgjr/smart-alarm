# ADR-005: API Layer - Padrões, Governança e Critérios de Pronto

## Contexto

A camada de API do SmartAlarm segue Clean Architecture, SOLID, e práticas modernas de testabilidade, observabilidade e segurança. Todos os endpoints são RESTful, documentados via Swagger/OpenAPI, e expõem contratos claros para integração.

## Decisão

- Controllers implementam todos os fluxos MVP (CRUD de alarmes, autenticação JWT, etc.)
- Middlewares globais para logging, tracing, autenticação, validação e tratamento de erros
- Documentação automática via Swagger/OpenAPI
- Padrão de resposta de erro centralizado (`ErrorResponse`)
- Testes xUnit cobrindo sucesso, erro e edge cases (mínimo 80% de cobertura)
- Governança: checklist de PR, ADR atualizado, Memory Bank atualizado

## Consequências

- APIs seguras, rastreáveis e fáceis de integrar
- Facilidade de manutenção e evolução
- Conformidade com LGPD, OWASP e padrões de observabilidade

## Critérios de pronto

- Todos os endpoints MVP implementados, testados e documentados
- Documentação Swagger/OpenAPI atualizada
- Checklists de governança e documentação seguidos
- ADR e Memory Bank atualizados

---

Consulte também: `/docs/api/alarms.endpoints.md`, `/docs/api/error-handling.md`, `/docs/api/swagger-access.md`, `/memory-bank/activeContext.md`, `/memory-bank/progress.md`.
