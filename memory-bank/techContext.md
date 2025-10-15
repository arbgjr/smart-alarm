# Smart Alarm — Tech Context (Updated 19/07/2025)

## Current Technology Stack - Production Ready

### **Backend Architecture (C# .NET 8)**

**Core Framework**: ASP.NET Core 8.0 with Clean Architecture implementation
**Microservices**: Three specialized services, all C#-based:

1. **SmartAlarm.AiService**: ML.NET for behavioral analysis and predictive recommendations
2. **SmartAlarm.AlarmService**: Hangfire for background job scheduling and alarm management  
3. **SmartAlarm.IntegrationService**: External API integrations with resilience patterns

**Architectural Patterns**:

- **CQRS with MediatR**: Command/Query separation for all business operations
- **Repository + Unit of Work**: Abstracted data access with multi-provider support
- **Dependency Injection**: Native .NET DI container with environment-based configuration
- **Domain Events**: Event-driven architecture for inter-service communication

### **Data Persistence (Multi-Provider)**

**Primary Databases**:

- **Development/Testing**: PostgreSQL with Entity Framework Core
- **Production**: Oracle Autonomous Database with EF Core Oracle provider
- **Legacy Support**: Dapper for complex queries and stored procedures

**Caching Strategy**:

- **Development**: In-memory caching and session state
- **Staging/Production**: StackExchange.Redis for distributed caching and JWT blacklisting
- **Cache Patterns**: Write-through, write-behind, and cache-aside based on data criticality

**Object Storage**:

- **Development/Staging**: MinIO with automatic health check and fallback to mock storage
- **Production**: OCI Object Storage with intelligent retry policies
- **Fallback Logic**: SmartStorageService automatically detects availability and switches providers

### **Security & Authentication**

**Authentication Stack**:

- **Primary**: JWT tokens with RS256 signing
- **Advanced**: FIDO2/WebAuthn support for passwordless authentication
- **Token Management**: Redis-backed blacklist for token revocation and logout

**Secrets Management (Multi-Cloud)**:

- **Development**: HashiCorp Vault (local or cloud)
- **Production**: Environment-specific providers:
  - OCI Vault for Oracle Cloud deployments
  - Azure Key Vault for Azure deployments  
  - AWS Secrets Manager for AWS deployments
  - Google Secret Manager for GCP deployments

**Security Features**:

- **Password Hashing**: BCrypt.Net-Next with configurable work factors
- **Input Validation**: FluentValidation for all DTOs and commands
- **RBAC**: Role-based access control with custom authorization policies
- **LGPD Compliance**: Data protection and privacy controls built-in

### **Observability Stack (Production-Grade)**

**Distributed Tracing**:

- **Framework**: OpenTelemetry with .NET instrumentation
- **Export**: OTLP protocol to Jaeger for development, cloud-native solutions for production
- **Custom Instrumentation**: Every service operation traced with correlation IDs

**Structured Logging**:

- **Framework**: Serilog with structured JSON output
- **Sinks**: Console (dev), File (staging), Loki (production)
- **Correlation**: Request correlation IDs for distributed debugging
- **Security**: Sensitive data filtering and redaction

**Metrics & Monitoring**:

- **Collection**: Prometheus metrics with .NET meter API
- **Visualization**: Grafana dashboards for all services
- **Alerting**: PrometheusAlert manager for critical system events
- **Custom Metrics**: Business metrics (alarms created, AI predictions, integration calls)

**Health Checks**:

- **Framework**: ASP.NET Core Health Checks
- **Dependencies**: Database, Redis, external APIs, storage services
- **Monitoring**: Health check endpoints for orchestration platforms

### **Messaging & Communication**

**Message Broker**:

- **Development**: RabbitMQ with management UI
- **Staging**: RabbitMQ with SSL/TLS and clustering
- **Production**: OCI Streaming (Apache Kafka-compatible) with high availability

**HTTP Communication**:

- **Framework**: IHttpClientFactory with Polly integration
- **Resilience**: Circuit breaker, retry with exponential backoff, timeout policies
- **External APIs**: Google Calendar, Microsoft Graph, holiday APIs

**Event-Driven Architecture**:

- **Pattern**: Domain events published after successful database commits
- **Serialization**: System.Text.Json for performance and compatibility
- **Error Handling**: Dead letter queues and retry mechanisms

### **External Integrations**

**Calendar Services**:

- **Google Calendar**: OAuth 2.0 with incremental consent and offline access
- **Microsoft Graph**: Microsoft Graph SDK with application and delegated permissions
- **Apple CloudKit**: CloudKit Web Services for calendar synchronization
- **CalDAV**: RFC 4791 implementation for generic calendar servers

**Notification Providers**:

- **Firebase Cloud Messaging**: Push notifications with fallback to email
- **Email**: SMTP with template engine and delivery tracking
- **SMS**: Twilio integration for critical notifications

**AI/ML Services**:

- **Core Engine**: ML.NET for on-premises machine learning
- **External APIs**: Optional integration with Azure Cognitive Services
- **Data Processing**: Local processing for privacy-sensitive behavioral analysis

### **Development & Deployment**

**Build System**:

- **Package Management**: Centralized with Directory.Packages.props
- **Build Tool**: .NET CLI with automated dependency resolution
- **CI/CD**: GitHub Actions with multi-environment deployment
- **Quality Gates**: SonarQube, CodeQL security scanning, test coverage reports

**Testing Strategy**:

- **Unit Testing**: xUnit with FluentAssertions for readable test assertions
- **Mocking**: Moq for dependency isolation and behavior verification
- **Integration Testing**: Testcontainers for real database and service dependencies
- **End-to-End**: Playwright for full application workflow testing

**Containerization**:

- **Runtime**: Docker with multi-stage builds for optimization
- **Orchestration**: Kubernetes with Helm charts for deployment
- **Development**: Docker Compose for local multi-service development
- **Production**: Serverless deployment on OCI Functions

### **Infrastructure as Code**

**Provisioning**:

- **Primary**: Terraform for cloud infrastructure management
- **Alternative**: Azure Bicep for Azure-specific deployments
- **Configuration**: Environment-specific tfvars files
- **State Management**: Remote backend with encryption and locking

**Monitoring Infrastructure**:

- **Metrics**: Prometheus server with persistent storage
- **Visualization**: Grafana with automated dashboard provisioning
- **Logging**: Loki for log aggregation and querying
- **Tracing**: Jaeger with distributed storage backend

### **Development Tools & IDE Support**

**Primary IDE**: Visual Studio Code with C# Dev Kit
**Extensions**:

- C# language support with IntelliSense
- Thunder Client for API testing
- Docker extension for container management
- GitLens for advanced Git integration
- REST Client for endpoint testing

**WSL Development Environment** ✅:

- **Platform**: WSL2 (Ubuntu) with Windows host access
- **Node.js**: v22.17.1 with npm 10.9.2
- **Vite Configuration**: External access (host: '0.0.0.0', port: 5173)
- **Network**: Auto-detected IP (current: 172.24.66.127:5173)
- **Scripts**:
  - `start-wsl-dev.sh` - Automated development server startup
  - `verify-wsl-setup.sh` - Environment verification and health checks
- **Documentation**: Complete WSL setup guide in `docs/development/WSL-SETUP-GUIDE.md`
- **Cross-Platform**: Windows ↔ WSL seamless development workflow

**Code Quality**:

- **Linting**: Roslyn analyzers with custom rule sets  
- **Formatting**: EditorConfig for consistent code style
- **Documentation**: XML documentation comments for all public APIs
- **Architecture**: ArchUnit.NET for architectural constraint testing

---

## **Configuration Management**

**Environment Variables**: Hierarchical configuration with appsettings.json inheritance
**Secret Management**: Never stored in code, always retrieved from key vaults
**Feature Flags**: Environment-based feature toggling for gradual rollouts
**Validation**: Startup-time configuration validation with clear error messages

## **Performance Optimization**

**Database**:

- Connection pooling with optimized pool sizes
- Query optimization with EF Core query analysis
- Database indexes based on query patterns
- Read replicas for reporting queries

**Caching**:

- Multi-level caching (L1: memory, L2: Redis)
- Cache stampede protection with request coalescing
- TTL strategies based on data volatility
- Cache warming for critical data

**API Performance**:

- Response compression with Brotli/Gzip
- Pagination for large result sets
- Async operations throughout the stack
- Connection keep-alive and HTTP/2 support

---

**Last Updated**: July 19, 2025  
**Technology Status**: Production Ready ✅  
**Architecture Maturity**: Enterprise Grade ✅

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
