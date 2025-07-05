---
title: ADR-004: Abstração de Acesso a Dados Multi-Provider (PostgreSQL/Oracle)
date: 2025-07-05
status: accepted
---

# ADR-004: Abstração de Acesso a Dados Multi-Provider (PostgreSQL/Oracle)

## Contexto

Para garantir testabilidade, flexibilidade e compatibilidade com múltiplos bancos (PostgreSQL em desenvolvimento, Oracle em produção), optou-se por adotar uma abstração de acesso a dados baseada em interfaces (Repository/UnitOfWork) e injeção de dependência.

## Decisão

- Todas as operações de persistência serão abstraídas por interfaces no domínio.
- Implementações específicas para cada banco (PostgreSQL, Oracle) serão criadas na camada de infraestrutura.
- A seleção do provider será feita via DI, conforme ambiente/configuração.
- Testes de integração usarão PostgreSQL para garantir rollback real e cobertura de cenários críticos.

## Consequências

- Domínio permanece desacoplado da infraestrutura.
- Facilita testes reais e evolução independente dos providers.
- Necessário garantir compatibilidade de queries e mapeamentos.

## Referências

- systemPatterns.md
- projectPlanning.md
- techDebt.md
