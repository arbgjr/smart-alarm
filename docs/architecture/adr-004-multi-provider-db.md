# ADR-004: Multi-Provider Database Infrastructure

## Context

O Smart Alarm precisa suportar diferentes bancos de dados para ambientes distintos:

- **Desenvolvimento/Testes:** PostgreSQL (open source, fácil de rodar em Docker, sem custos de licença)
- **Produção:** Oracle Autonomous DB (OCI), padrão corporativo, alta disponibilidade e integração nativa com OCI Functions

## Decisão

- A camada de persistência foi abstraída por interfaces (Repository/UnitOfWork), com implementações específicas para cada provider.
- O provedor é selecionado via Dependency Injection e configuração de ambiente.
- O projeto de testes de integração utiliza PostgreSQL real via Docker Compose.
- O build e execução de testes garantem que todas as integrações (RabbitMQ, MinIO, Vault, PostgreSQL) estejam disponíveis e validadas.
- O código nunca expõe segredos ou strings de conexão em logs ou código-fonte.

## Consequências

- Permite desenvolvimento e testes locais rápidos, sem dependência de Oracle.
- Garante compatibilidade e portabilidade entre ambientes.
- Facilita a evolução futura para outros providers, se necessário.
- Exige atenção à compatibilidade de versões de EF Core e dependências NuGet.

## Status

Implementação concluída e validada em ambiente dockerizado, com todos os testes de integração passando.

---

- Data: 2025-07-05
- Responsável: GitHub Copilot
- Referências: `memory-bank/activeContext.md`, `progress.md`, `techContext.md`, `systemPatterns.md`.
