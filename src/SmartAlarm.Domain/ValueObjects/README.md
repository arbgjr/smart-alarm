
# Value Objects do Domínio

Este diretório contém os Value Objects (VOs) do domínio do Smart Alarm.

## Critérios de Pronto (Etapa 1 — Modelagem Completa do Domínio)

- Todos os VOs possuem propriedades, validações, invariantes e testes unitários cobrindo sucesso, erro e edge cases
- Documentação de cada VO (resumo, exemplos de uso, invariantes)
- Exemplos práticos disponíveis nos testes automatizados

**Status:** Todos os critérios da etapa 1 foram cumpridos, validados e revisados conforme checklist do projeto. Para detalhes completos, consulte também:

- `docs/domain-model.md` (documentação detalhada do modelo)
- `tests/SmartAlarm.Tests/Domain/ValueObjects/` (testes automatizados)
- `docs/planning/projectPlanning.md` (critérios de pronto e governança)

## Value Objects Implementados

- `Email`: Representa e valida o e-mail do usuário
- `Name`: Representa e valida o nome
- `TimeConfiguration`: Configuração de horário e timezone

## Exemplos de Uso

Consulte os testes em `../../../../tests/SmartAlarm.Tests/Domain/ValueObjects/` para exemplos práticos de uso e validação dos VOs.

---

## Checklist de Pronto (Etapa 1)

- [x] Todos os VOs possuem propriedades, validações, invariantes e testes unitários cobrindo sucesso, erro e edge cases
- [x] Documentação de cada VO (resumo, exemplos de uso, invariantes)
- [x] Exemplos práticos disponíveis nos testes automatizados
- [x] Critérios de pronto globais e específicos da etapa validados
- [x] Status revisado conforme governança do projeto
