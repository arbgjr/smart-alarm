# Smart Alarm — Tech Context

## Technologies

- **Backend**: C# (.NET 8.0+), Clean Architecture, ASP.NET Core
- **Padrões de Arquitetura**: CQRS com MediatR, Injeção de Dependência, Repositório, Unit of Work
- **Serviços de Microsserviços**:
  - `ai-service`: Focado em Machine Learning com **ML.NET**.
  - `alarm-service`: Gerenciamento de tarefas em background com **Hangfire**.
  - `integration-service`: Integrações externas com resiliência (Polly) e autenticação JWT.
- **Persistência**:
  - **Bancos de Dados**: Entity Framework Core para **PostgreSQL** (desenvolvimento/testes) e **Oracle** (produção). Suporte legado a Dapper.
  - **Cache**: StackExchange.Redis para cache distribuído e blacklisting de JWT.
  - **Armazenamento de Objetos**: **MinIO** (desenvolvimento/testes), **OCI Object Storage** (produção).
- **Frontend**: React, TypeScript, PWA (Progressive Web App)
- **Cloud & Secrets**:
  - **Multi-Cloud Vault**: Suporte para **HashiCorp Vault** (desenvolvimento/testes), **Azure Key Vault**, **OCI Vault**, AWS Secrets Manager, Google Secret Manager.
  - **Cloud Providers**: Oracle Cloud (OCI), Azure, AWS, Google Cloud.
- **Infraestrutura**: Docker, Docker Compose, Kubernetes, Terraform
- **Observabilidade (Stack Completa)**:
  - **Métricas**: **OpenTelemetry** e **Prometheus**.
  - **Logging**: **Serilog** para logging estruturado, com **Loki** para agregação.
  - **Tracing**: **OpenTelemetry** e **Jaeger** para tracing distribuído.
  - **Visualização**: **Grafana** para dashboards e visualização de dados.
- **Testes**:
  - **Frameworks**: xUnit, Moq, FluentAssertions.
  - **Testes de Integração**: **Testcontainers** para provisionamento de dependências (bancos de dados, etc.) e `Microsoft.AspNetCore.Mvc.Testing`.

## Configurations

- **CI/CD**: Validação com testes e linting em pipelines automatizados.
- **Documentação de API**: Geração automática com Swagger/OpenAPI via Swashbuckle.
- **Gerenciamento de Configuração**: Variáveis de ambiente, `appsettings.json`, e provedores de segredos (Key Vault).
- **Gerenciamento de Pacotes**: Centralizado com `Directory.Packages.props`.

## Dependencies

- **Core & Framework**: MediatR, FluentValidation, Polly, HttpClientFactory, Serilog, OpenTelemetry.
- **Segurança**: JWT, FIDO2, BCrypt.Net-Next para hashing de senhas.
- **Integrações Externas**: Google Calendar API, Microsoft Graph API.
- **Agendamento de Tarefas**: Hangfire.
- **Machine Learning**: Microsoft.ML.
- **Documentação de API**: Swashbuckle.
- **Serialização/Deserialização**: CsvHelper, System.Text.Json.
