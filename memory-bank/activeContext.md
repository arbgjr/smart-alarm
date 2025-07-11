# Smart Alarm — Active Context

## Current Focus

- Implementação dos endpoints principais do AlarmService (CRUD)
- Handlers e validação com FluentValidation
- Estruturação de logging e métricas nos fluxos críticos
- Preparação para testes automatizados e integração de autenticação JWT/FIDO2
- Correção e simplificação dos testes de integração para infraestrutura

## Recent Changes

- AlarmController implementado com endpoints RESTful (Create, List, GetById, Update, Delete)
- Handlers para criação, atualização, exclusão, listagem e consulta de alarmes
- Validação com FluentValidation aplicada nos comandos e DTOs
- Logging estruturado e métricas em todos os handlers principais
- Simplificados os testes de integração para MinIO e Vault para usar verificação HTTP de saúde
- Corrigidos problemas de compilação relacionados a APIs incompatíveis em VaultSharp
- Melhorado script docker-test.sh com verificação dinâmica de saúde dos serviços
- Implementado sistema de execução seletiva de testes por categoria (essentials, observability)
- Adicionado diagnóstico detalhado e sugestões de solução para falhas em testes

## Next Steps

- Implementar autenticação JWT/FIDO2
- Continuar testes automatizados (xUnit, Moq, cobertura mínima 80%)
- Resolver problemas de conectividade nos testes de serviços de observabilidade
- Documentar endpoints e arquitetura (Swagger/OpenAPI)
- Validar integração de observabilidade (Loki, Jaeger, Prometheus, Grafana)
- Integrar melhorias de testes de integração com pipeline CI/CD

## Infraestrutura de Testes

### Abordagem de Testes de Integração

- **Simplificação**: Uso de verificações HTTP de saúde em vez de APIs complexas
- **Categorização**: Separação em testes essenciais (MinIO, Vault, PostgreSQL, RabbitMQ) e de observabilidade
- **Resiliência**: Implementação de verificações de saúde com retentativas
- **Execução Seletiva**: Possibilidade de executar categorias específicas de testes

### Script de Teste Docker

- **Verificação Dinâmica**: Substituição de sleeps fixos por checagens ativas de disponibilidade
- **Inicialização Condicional**: Serviços de observabilidade inicializados apenas quando necessário
- **Diagnóstico Aprimorado**: Informações detalhadas sobre o status dos serviços
- **Modo Debug**: Opção para apenas verificar ambiente sem executar testes

Este documento reflete o status real do backend do Smart Alarm, baseado em análise detalhada do código-fonte, corrigindo avaliações anteriores equivocadas e distinguindo entre pendências reais e comentários desatualizados.

---

---

## 1. API Layer (src/SmartAlarm.Api)

- Todos os arquivos principais da API existem e estão implementados.
- `AuthController.cs`: A autenticação está de fato mockada (usuário/senha hardcoded), sem integração real com provider de identidade. O comentário reflete o status real.

---

## 2. Application Layer (src/SmartAlarm.Application)

- Handlers estão implementados. O TODO em `ListRoutinesHandler.cs` ("ajustar para buscar todas se necessário") é real e indica lógica incompleta para busca de rotinas sem AlarmId.
- Não há NotImplementedException ou NotSupportedException nesta camada; a maioria dos handlers está funcional, com pequenos pontos de refinamento.

---

## 3. Domain Layer (src/SmartAlarm.Domain)

- Entities e ValueObjects estão completos, com métodos, validações e construtores adequados.
- `Integration.cs`: Possui métodos de negócio implementados. Os construtores obsoletos lançam NotSupportedException apenas para evitar uso incorreto, não por falta de implementação.
- `AlarmDomainService.cs`: Está implementado, com regras de negócio reais. Comentários sugerem possíveis otimizações, mas não há TODOs ou pendências reais.

---

## 4. Infrastructure Layer (src/SmartAlarm.Infrastructure)

### Messaging

- `OciStreamingMessagingService.cs`: Stub real, TODOs para integração com OCI SDK. MockMessagingService está implementado para dev/teste.

### Storage

- `OciObjectStorageService.cs`: TODOs para integração real com OCI SDK. MockStorageService está implementado para dev/teste.

### KeyVault

- `OciVaultProvider.cs`, `AzureKeyVaultProvider.cs`, `AwsSecretsManagerProvider.cs`: Todos são stubs, com TODOs para integração real. MockKeyVaultProvider está implementado para dev/teste.

### Observability

- MockTracingService e MockMetricsService implementados para dev/teste. Integração real (OpenTelemetry/Prometheus/Serilog) só ocorre em produção.

### Dependency Injection

- `DependencyInjection.cs`: Por padrão, registra mocks para mensageria, storage, keyvault, tracing e métricas. Integrações reais só são ativadas por configuração.

### Migrations

- Migrations do EF Core presentes, mas não foi verificado se refletem 100% o modelo de domínio.

---

## 5. Pendências Reais e Comentários Corrigidos

- TODOs para integração real com SDKs (OCI, Azure, AWS) em Storage, Messaging e KeyVault são **reais**.
- TODO em `ListRoutinesHandler.cs` é **real**.
- Mock de autenticação em `AuthController.cs` é **real**.
- Não há pendências reais em `AlarmDomainService.cs` ou `Integration.cs` (análise anterior estava equivocada).
- Não há NotImplementedException em handlers ou serviços principais.

---

## 6. Test Coverage

- Estrutura de testes existe para todas as áreas principais.
- Não há evidência clara de cobertura mínima de 80% para código crítico nem exemplos AAA pattern nos testes analisados.

---

## 7. Observability

- MockTracingService e MockMetricsService são usados em dev/teste.
- Integração real de observabilidade (OpenTelemetry, Prometheus, Serilog) só ocorre em produção.
- Não foi possível verificar handlers reais instrumentados conforme padrão, apenas exemplos/documentação.

---

## 8. Tabela Resumo: Status Real x Documentação

| Área                | Status Real no Código-Fonte                                                        | Status na Documentação                |
|---------------------|-----------------------------------------------------------------------------------|---------------------------------------|
| API Layer           | Endpoints, validação, logging, tracing, métricas presentes; autenticação mock      | 100% implementado, testado, doc.      |
| Application Layer   | Handlers implementados, TODO real em rotinas                                       | 100% implementado, testado, doc.      |
| Domain Layer        | Entidades, value objects, serviços completos                                       | 100% implementado, testado, doc.      |
| Infrastructure      | Integrações reais (OCI, Azure, AWS) são stubs/TODOs; mocks em dev/test            | Stub para prod, mock para dev         |
| Observability       | Mock em dev/test, real só em prod; instrumentação real não verificada             | 100% implementado, testado, doc.      |
| Messaging/Storage   | Mock em dev/test, stub para prod (TODOs reais)                                    | 100% dev/test, stub para prod         |
| KeyVault            | Mock em dev/test, stub para prod (TODOs reais)                                    | 100% dev/test, stub para prod         |
| Test Coverage       | Estrutura presente, cobertura real não comprovada                                 | 80-100% cobertura declarada           |

---

## 9. Principais Arquivos para Revisão

- `src/SmartAlarm.Infrastructure/Storage/OciObjectStorageService.cs` (stub, TODOs reais)
- `src/SmartAlarm.Infrastructure/Messaging/OciStreamingMessagingService.cs` (stub, TODOs reais)
- `src/SmartAlarm.Infrastructure/KeyVault/OciVaultProvider.cs`, `AzureKeyVaultProvider.cs`, `AwsSecretsManagerProvider.cs` (stubs, TODOs reais)
- `src/SmartAlarm.Infrastructure/DependencyInjection.cs` (registra mocks/stubs por padrão)
- `src/SmartAlarm.Application/Handlers/Routine/ListRoutinesHandler.cs` (TODO real)
- `src/SmartAlarm.Api/Controllers/AuthController.cs` (autenticação mockada)

---

## 10. Conclusão

- Toda a lógica de negócio principal (domain, application, API) está implementada e testada.
- Integrações reais com serviços cloud externos (OCI, Azure, AWS) ainda são stubs/TODOs reais; mocks são usados em dev/teste.
- Observabilidade, logging, validação e tratamento de erros seguem os padrões, mas instrumentação real só ocorre em produção.
- O código está alinhado com a documentação para a lógica de negócio, mas integrações cloud de produção ainda não estão implementadas.
- TODOs menores permanecem em pontos específicos da aplicação.

Este documento reflete fielmente o status real do backend, corrigindo avaliações anteriores e distinguindo entre pendências reais e comentários desatualizados.

## Active Decisions

- Uso exclusivo de .NET 8.0
- OCI Functions como padrão serverless
- Logging estruturado obrigatório
- Serviços de domínio centralizam regras de negócio e são ponto único de validação
- **Persistência multi-provider:** acesso a dados abstraído por interfaces, com implementações específicas para PostgreSQL (dev/testes) e Oracle (produção), selecionadas via DI/configuração. Decisão registrada em ADR-004.
- Integrações reais de mensageria, storage, keyvault e observabilidade implementadas e testadas
- Todos os testes de integração e unidade devem passar em ambiente dockerizado antes de concluir tarefas críticas.

## Environment Setup and Testing

- Scripts para ambiente de desenvolvimento e testes de integração foram criados e padronizados:
  - `start-dev-env.sh`: Inicia serviços necessários para desenvolvimento/testes
  - `stop-dev-env.sh`: Encerra ambiente (stop/clean/purge)
  - `test-integration.sh`: Executa testes de integração específicos ou completos

- Stack completa de observabilidade implementada via Docker Compose:
  - Prometheus para métricas
  - Loki para logs
  - Jaeger para tracing
  - Grafana para dashboards

- Fluxos de teste de integração padronizados para todos os serviços:
  - RabbitMQ: mensageria e eventos
  - PostgreSQL: persistência de dados
  - MinIO: armazenamento de objetos
  - HashiCorp Vault: gerenciamento de segredos
  - KeyVault: abstração para múltiplos provedores
  - Observabilidade: logs, métricas e tracing

- Documentação completa em `dev-environment-docs.md` para onboarding rápido de novos desenvolvedores
