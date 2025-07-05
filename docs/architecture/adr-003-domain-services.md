# ADR-003: Implementação dos Serviços de Domínio (AlarmDomainService, UserDomainService)

**Status:** Accepted
**Date:** 2025-07-05
**Authors:** @arman
**Decision Makers:** Tech Lead, Product Owner

## Contexto

Para garantir a centralização das regras de negócio, testabilidade e aderência à Clean Architecture, foi decidido implementar serviços de domínio específicos para alarmes e usuários, desacoplados da infraestrutura e com testes unitários robustos.

## Decisão

- Implementar `AlarmDomainService` e `UserDomainService` como serviços de domínio, cada um responsável por regras críticas do negócio.
- Garantir que toda lógica de validação, limites, unicidade, ativação/desativação e vínculo entre entidades seja tratada nesses serviços.
- Cobertura mínima de 80% de testes unitários AAA para todos os fluxos críticos.
- Documentação e exemplos de uso obrigatórios.

## Justificativa

- Centralização das regras de negócio facilita manutenção, evolução e auditoria.
- Testabilidade: serviços desacoplados permitem mocks e testes AAA.
- Consistência: regras de negócio não se dispersam por controladores ou infraestrutura.
- Governança: owners definidos, documentação e ADR registrados.

## Implicações

- Serviços de domínio passam a ser ponto único de orquestração de regras.
- Mudanças futuras em regras de negócio devem ser refletidas nesses serviços.
- Testes de integração e unitários devem sempre cobrir cenários de sucesso, erro e edge cases.

## Critérios de Sucesso

- Serviços implementados, testados e documentados.
- Owners definidos e checklist de governança seguido.
- ADR e documentação atualizados.

---
*Este ADR deve ser revisado a cada alteração relevante nos serviços de domínio.*
