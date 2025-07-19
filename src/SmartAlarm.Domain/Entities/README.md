
# Entidades do Domínio

Este diretório contém as entidades principais do domínio do Smart Alarm.

## Critérios de Pronto (Etapa 1 — Modelagem Completa do Domínio)

- Todas as entidades possuem propriedades, métodos, validações e testes unitários cobrindo sucesso, erro e edge cases
- Documentação de cada entidade (resumo, exemplos de uso, invariantes)
- Exemplos práticos disponíveis nos testes automatizados

**Status:** Todos os critérios da etapa 1 foram cumpridos, validados e revisados conforme checklist do projeto. Para detalhes completos, consulte também:

- `docs/domain-model.md` (documentação detalhada do modelo)
- `tests/SmartAlarm.Tests/Domain/Entities/` (testes automatizados)
- `docs/planning/projectPlanning.md` (critérios de pronto e governança)

## Entidades Implementadas

- `Alarm`: Representa um alarme configurado pelo usuário
- `User`: Representa um usuário do sistema
- `Schedule`: Configuração de horário e recorrência de um alarme
- `Routine`: Rotina associada a um alarme
- `Integration`: Integração externa configurada para um alarme
- `DaysOfWeek`: Enum de dias da semana
- `ScheduleRecurrence`: Enum de recorrência

## Exemplos de Uso

Consulte os testes em `../../../../tests/SmartAlarm.Tests/Domain/Entities/` para exemplos práticos de uso e validação das entidades.

---

## Checklist de Pronto (Etapa 1)

- [x] Todas as entidades possuem propriedades, métodos, validações e testes unitários cobrindo sucesso, erro e edge cases
- [x] Documentação de cada entidade (resumo, exemplos de uso, invariantes)
- [x] Exemplos práticos disponíveis nos testes automatizados
- [x] Critérios de pronto globais e específicos da etapa validados
- [x] Status revisado conforme governança do projeto
