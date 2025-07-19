# ADR-999: Estratégia de Persistência para Autenticação JWT/FIDO2

## Status
Aceito

## Contexto
Necessidade de definir estratégia de persistência para sistema de autenticação JWT/FIDO2, considerando performance, simplicidade e compatibilidade com a arquitetura existente.

## Decisão
Utilizar **Entity Framework Core** como ORM principal para persistência de dados de autenticação.

## Justificativa
- **Performance**: EF Core 8.0+ oferece excelente performance para operações CRUD
- **Integração**: Já existe infraestrutura EF configurada no projeto
- **Type Safety**: Queries fortemente tipadas e compile-time checks
- **Migrations**: Suporte nativo a migrations para evolução do schema
- **Testing**: Facilidade de mockar repositories para testes unitários
- **Produtividade**: Desenvolvimento mais rápido comparado a Dapper/SQL raw

## Consequências
### Positivas
- Integração nativa com .NET 8
- Suporte completo a migrations
- Type-safe queries e compile-time validation
- Facilidade de mocking para testes
- Consistent com padrões já estabelecidos no projeto

### Negativas
- Overhead mínimo comparado a Dapper (aceitável para o contexto)
- Curva de aprendizado para queries complexas (mitigado pela experiência da equipe)

## Alternativas Consideradas
- **Dapper**: Descartado por já existir infraestrutura EF
- **SQL Raw**: Descartado pela falta de type safety

## Implementação
- Utilizar repositórios EF existentes (`EfUserRepository`)
- Criar `EfUserCredentialRepository` para credenciais FIDO2
- Manter padrões de Unit of Work já estabelecidos

---
*Data: 2025-07-03*
*Revisores: Equipe de Arquitetura*
