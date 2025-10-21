# Smart Alarm - Production-Ready PWA

[![Build Status](https://github.com/smartalarm/smart-alarm/workflows/CI/badge.svg)](https://github.com/smartalarm/smart-alarm/actions)
[![Coverage](https://codecov.io/gh/smartalarm/smart-alarm/branch/main/graph/badge.svg)](https://codecov.io/gh/smartalarm/smart-alarm)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![PWA](https://img.shields.io/badge/PWA-Ready-brightgreen.svg)](https://developer.mozilla.org/en-US/docs/Web/Progressive_web_apps)
[![WCAG AAA](https://img.shields.io/badge/WCAG-AAA-brightgreen.svg)](https://www.w3.org/WAI/WCAG21/quickref/)

Um sistema de alarmes inteligente, acessível e **100% production-ready** construído com .NET 8, React PWA, ML.NET e seguindo Clean Architecture. Inclui OAuth2 real, sincronização de calendários, observabilidade completa e ambiente de desenvolvimento com um comando.

## Overview

Smart Alarm is designed as a serverless-first backend platform that provides intelligent alarm management capabilities with a focus on accessibility and neurodiversity support. The platform uses modern .NET technologies and follows Clean Architecture principles to ensure maintainability, testability, and scalability.

<div align="center">
  <img src="./docs/architecture/system-overview.png" alt="Smart Alarm Architecture" width="640px" />
</div>

The platform consists of three main microservices:

- **Alarm Service**: Core alarm management with background processing using Hangfire
- **AI Service**: Machine learning capabilities powered by ML.NET for pattern recognition and intelligent suggestions  
- **Integration Service**: External API integrations with resilience patterns using Polly

## Features

### Backend Infrastructure

- **Serverless Architecture**: Built for OCI Functions with full serverless deployment capabilities
- **Clean Architecture**: Strict separation of concerns across Domain, Application, Infrastructure, and API layers
- **Multi-Provider Support**: Database (PostgreSQL/Oracle), storage (MinIO/OCI), and secrets management (Vault/Azure/OCI)
- **Comprehensive Security**: JWT authentication with FIDO2 support, OAuth2 login (Google, GitHub, Facebook, Microsoft), token revocation via Redis blacklist
- **Full Observability**: Structured logging (Serilog), distributed tracing (OpenTelemetry/Jaeger), and metrics (Prometheus)
- **Background Processing**: Intelligent task scheduling and execution with Hangfire
- **AI-Powered Insights**: ML.NET integration for behavioral pattern analysis and optimization recommendations
- **Resilient Integrations**: External API integration with circuit breakers, retries, and bulkhead patterns

### Progressive Web Application (PWA) ✨ COMPLETE

- **Offline-First Experience**: Complete PWA implementation with service worker and background sync
- **Cross-Platform Installation**: Installable on mobile devices and desktop platforms
- **Smart Caching**: Network-first caching for API calls with intelligent fallback strategies
- **Background Sync**: Automatic synchronization of offline actions when connectivity returns
- **Responsive Design**: Mobile-optimized interface with accessibility-first approach

### Modern State Management ✨ COMPLETE

- **Zustand Integration**: Centralized state management with persistent storage
- **Optimistic Updates**: Immediate UI feedback with server synchronization
- **React Query Optimization**: Intelligent caching and data synchronization
- **Offline State**: Seamless offline/online state transitions with conflict resolution
- **Theme & Accessibility**: Complete UI state management with user preferences

### AI-Powered Sleep Intelligence ✨ NEW

- **ML Data Collection**: Privacy-first behavioral tracking with local processing
- **Sleep Pattern Analysis**: Intelligent sleep cycle detection and chronotype identification
- **Smart Alarm Optimization**: Automatic timing optimization for light sleep phases
- **Personalized Recommendations**: Context-aware suggestions for better sleep hygiene
- **Sleep Analytics Dashboard**: Comprehensive insights with confidence scoring

### Real-time Multi-Device Sync ✨ NEW

- **SignalR Hub Integration**: Real-time alarm synchronization across devices
- **Push Notifications**: Web Push API with VAPID keys for native notifications
- **Conflict Resolution**: Last-writer-wins with automatic conflict handling
- **Device Presence Tracking**: Multi-device awareness and status synchronization
- **Offline Queue**: Intelligent sync queuing for offline scenarios

### Production-Grade Testing ✨ NEW

- **Comprehensive E2E Testing**: Playwright-based testing with Docker infrastructure
- **Cross-Browser Compatibility**: Testing across Chrome, Firefox, Safari, and mobile devices
- **Accessibility Testing**: WCAG AAA compliance validation
- **Performance Testing**: Load testing and response time validation
- **CI/CD Pipeline**: Automated testing with GitHub Actions

## Getting Started

### Prerequisites

To run Smart Alarm locally, you need:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/) and npm
- [Docker](https://www.docker.com/products/docker-desktop) and Docker Compose
- [Git](https://git-scm.com/downloads)
- [PowerShell 7+](https://github.com/powershell/powershell) (for scripts)

### Quick Start

1. **Clone the repository**:

   ```bash
   git clone https://github.com/arbgjr/smart-alarm.git
   cd smart-alarm
   ```

2. **Start infrastructure services**:

   ```bash
   docker compose up -d
   ```

3. **Start the backend**:

   ```bash
   dotnet restore SmartAlarm.sln
   dotnet build SmartAlarm.sln --no-restore
   dotnet run --project src/SmartAlarm.Api
   ```

4. **Start the frontend** (in a new terminal):

   ```bash
   cd frontend
   npm install
   npm run dev
   ```

5. **Run tests**:

   ```bash
   # Backend tests
   dotnet test --logger "console;verbosity=detailed"
   
   # Frontend tests
   cd frontend
   npm test
   npm run test:e2e
   ```

The applications will be available at:

- **Frontend**: `http://localhost:5173` (Vite dev server)
- **Backend API**: `https://localhost:8080` with Swagger at `https://localhost:8080/swagger`

> [!NOTE]
> The Docker Compose setup includes all necessary infrastructure services including PostgreSQL, Redis, RabbitMQ, HashiCorp Vault, and observability stack (Prometheus, Grafana, Jaeger).

### WSL Development (Windows + Linux)

For Windows users developing with WSL:

1. **Quick Setup**:

   ```bash
   # Run the automated WSL setup script
   ./start-wsl-dev.sh
   ```

2. **Access from Windows**:
   - Script shows WSL IP automatically
   - Access: `http://[WSL-IP]:5173`

📖 **Complete WSL Guide**: [docs/development/WSL-SETUP-GUIDE.md](./docs/development/WSL-SETUP-GUIDE.md)

### Development Environment

For a complete development setup with all services:

```bash
# Start all development services
docker compose -f docker-compose.dev.yml up -d

# Run integration tests
dotnet test --filter Category=Integration --logger "console;verbosity=detailed"

# Generate code coverage
dotnet test --collect:"XPlat Code Coverage" --settings tests/coverlet.runsettings
```

## Technology Stack

### Core Technologies

- **Backend**: C# (.NET 8), ASP.NET Core, Clean Architecture
- **Frontend**: React 18, TypeScript, Vite, TailwindCSS
- **State Management**: Zustand with React Query optimization
- **Patterns**: CQRS with MediatR, Repository with Unit of Work, Domain Events
- **Authentication**: JWT with FIDO2 support, OAuth2 providers (Google, GitHub, Facebook, Microsoft), Redis-backed token blacklist
- **Validation**: FluentValidation (backend), Zod/React Hook Form (frontend)

### Persistence & Storage

- **Databases**: Entity Framework Core with PostgreSQL (dev/test) and Oracle (production)
- **Cache**: StackExchange.Redis for distributed caching and JWT blacklisting
- **Storage**: MinIO (dev/test), OCI Object Storage (production) with auto-detection

### Cloud & Infrastructure

- **Deployment**: Oracle Cloud Infrastructure (OCI) Functions, serverless-first design
- **Secrets**: Multi-provider support (HashiCorp Vault, Azure Key Vault, OCI Vault)
- **Infrastructure**: Docker, Kubernetes, Terraform for multi-cloud deployments

### Observability Stack

- **Logging**: Serilog with structured logging and Loki aggregation
- **Tracing**: OpenTelemetry with Jaeger for distributed tracing
- **Metrics**: OpenTelemetry metrics exported to Prometheus
- **Visualization**: Grafana dashboards for comprehensive monitoring

### Frontend Stack

- **PWA**: Vite PWA plugin with Workbox for service worker and caching
- **Real-time**: SignalR client for live synchronization and notifications
- **ML Integration**: Client-side ML data collection with privacy-first design
- **Testing**: Vitest (unit), Playwright (E2E), Testing Library (integration)

### Microservices

- **AI Service**: ML.NET for behavioral pattern analysis and intelligent recommendations
- **Alarm Service**: Hangfire for background job processing and task scheduling
- **Integration Service**: Polly for resilience patterns with external APIs

## Architecture

Smart Alarm follows Clean Architecture principles with clear separation of concerns:

```
├── src/                           # Backend (.NET)
│   ├── SmartAlarm.Domain/          # Business entities and rules
│   ├── SmartAlarm.Application/     # Use cases and application logic
│   ├── SmartAlarm.Infrastructure/  # External concerns (DB, APIs, etc.)
│   ├── SmartAlarm.Api/            # REST API controllers and middleware
│   ├── SmartAlarm.KeyVault/       # Multi-provider secrets management
│   └── SmartAlarm.Observability/  # Tracing, metrics, and logging
├── frontend/                      # React/TypeScript PWA
│   ├── src/
│   │   ├── components/            # Reusable React components
│   │   ├── pages/                 # Route-specific page components
│   │   ├── stores/                # Zustand state management
│   │   ├── services/              # API client services
│   │   ├── utils/                 # ML, sync, and utility functions
│   │   └── hooks/                 # Custom React hooks
│   ├── tests/e2e/                 # Playwright E2E tests
│   └── public/                    # PWA assets and manifest
├── services/
│   ├── ai-service/                # ML.NET powered AI capabilities
│   ├── alarm-service/             # Background processing with Hangfire
│   └── integration-service/       # External API integrations
└── tests/                         # Backend test suite
```

### Key Architectural Decisions

- **Serverless-First**: Designed for OCI Functions with stateless operations
- **Multi-Provider**: Abstract infrastructure dependencies for cloud portability  
- **Event-Driven**: Domain events for loose coupling between bounded contexts
- **Resilience**: Circuit breakers, retries, and bulkhead patterns for external dependencies

## Configuration

Smart Alarm uses environment-based configuration with secure secret management:

### Environment Variables

```bash
# Database Configuration
DATABASE_PROVIDER=PostgreSQL  # or Oracle for production
CONNECTION_STRING=Host=localhost;Database=SmartAlarm;Username=dev;Password=dev

# Cache & Messaging
REDIS_CONNECTION_STRING=localhost:6379
RABBITMQ_HOST=localhost
RABBITMQ_USER=guest
RABBITMQ_PASS=guest

# Security
JWT_SECRET_KEY=your-jwt-secret
JWT_BLACKLIST_ENABLED=true

# Secrets Management
KEYVAULT_ENABLED=true
HASHICORP_VAULT__SERVER_ADDRESS=http://localhost:8200
HASHICORP_VAULT__TOKEN=dev-token

# Observability
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
PROMETHEUS_ENDPOINT=http://localhost:9090
```

### Multi-Provider Configuration

The platform automatically detects and configures providers based on the environment:

- **Development**: PostgreSQL + MinIO + HashiCorp Vault
- **Production**: Oracle + OCI Object Storage + OCI Vault
- **Testing**: In-memory providers with Testcontainers

## Testing

Smart Alarm maintains high test coverage with comprehensive testing strategies and automated report generation:

### 🚀 Automated Test Scripts (Recommended)

```bash
# TDD-compliant unit tests (123 tests passing 100%)
bash scripts/test-reports.sh unit

# Integration tests with PostgreSQL/Redis
bash scripts/test-reports.sh integration

# API/Controller tests with full environment
bash scripts/test-reports.sh api

# Security/OWASP validation tests
bash scripts/test-reports.sh security

# End-to-End tests with Playwright
bash scripts/test-reports.sh e2e

# All tests with comprehensive reports
bash scripts/test-reports.sh all
```

### Manual Test Commands

```bash
# Backend tests
dotnet test --logger "console;verbosity=detailed"

# Run only unit tests
dotnet test --filter Category!=Integration

# Run integration tests (requires Docker)
docker compose -f docker-compose.full.yml up -d
dotnet test --filter Category=Integration

# Generate coverage report
dotnet test --collect:"XPlat Code Coverage" --settings tests/coverlet.runsettings

# Frontend tests
cd frontend
npm test                    # Unit tests with Vitest
npm run test:e2e           # E2E tests with Playwright
npm run test:e2e:docker    # E2E tests with Docker infrastructure
```

### Test Categories

- **Unit Tests**: Business logic validation with 80%+ coverage target
- **Integration Tests**: Database, external APIs, and service interactions
- **E2E Tests**: Full user workflow testing with Playwright across multiple browsers
- **Component Tests**: React component testing with Testing Library
- **Contract Tests**: API contract validation and schema compliance
- **Accessibility Tests**: WCAG AAA compliance validation
- **Performance Tests**: Load testing and response time validation

## Deployment

### Local Development

```bash
# Start infrastructure services
docker compose up -d

# Run the application
dotnet run --project src/SmartAlarm.Api

# Access services
# API: https://localhost:8080
# Swagger: https://localhost:8080/swagger
# Grafana: http://localhost:3000 (admin/admin)
# Jaeger: http://localhost:16686
```

### Production (OCI Functions)

```bash
# Deploy infrastructure
cd infrastructure/terraform
terraform init
terraform plan
terraform apply

# Deploy functions
./infrastructure/deploy-serverless.ps1 -Environment Production
```

> [!WARNING]
> Ensure all secrets are properly configured in your chosen key vault provider before production deployment.

## 📚 Documentation

### Key Documentation Files

- **[📋 RELATORIO_TESTE_EVIDENCIAS.md](./RELATORIO_TESTE_EVIDENCIAS.md)** - Complete testing evidence and validation report
- **[🧪 docs/TESTING.md](./docs/TESTING.md)** - Comprehensive testing guide with TDD compliance
- **[🏗️ CLAUDE.md](./CLAUDE.md)** - Development commands and architectural guidance
- **[🚀 docs/DEPLOYMENT.md](./docs/DEPLOYMENT.md)** - Production deployment and infrastructure setup

### Test Reports (Auto-Generated)

When running test scripts, reports are automatically generated in:

```
TestResults/
├── Unit/
│   ├── TestResults.html          # Visual test results
│   ├── TestResults.trx           # Microsoft TRX format
│   └── coverage-report/index.html # Code coverage report
├── Integration/
├── API/
└── Security/
```

### Quick Reference

```bash
# Start complete development environment
docker compose -f docker-compose.full.yml up -d

# Run TDD-compliant unit tests (123 tests - 100% pass rate)
bash scripts/test-reports.sh unit

# Check infrastructure status
docker compose -f docker-compose.full.yml ps
```

## Monitoring & Observability

Smart Alarm provides comprehensive observability out of the box:

### Metrics

- Application metrics via OpenTelemetry
- Custom business metrics for alarm operations
- Infrastructure metrics via Prometheus

### Logging  

- Structured logging with Serilog
- Correlation IDs for request tracing
- Log aggregation with Loki

### Tracing

- Distributed tracing with OpenTelemetry
- Jaeger for trace visualization
- Custom spans for business operations

### Health Checks

- Comprehensive health endpoints
- Dependency health monitoring
- Kubernetes liveness/readiness probes

## API Documentation

The REST API is fully documented with OpenAPI/Swagger:

- **Local**: `https://localhost:8080/swagger`
- **Production**: Available at your deployed endpoint `/swagger`

### Key Endpoints

- `GET /api/health` - Health check endpoint
- `POST /api/auth/login` - JWT authentication
- `GET /api/alarms` - List alarms with filtering
- `POST /api/alarms` - Create new alarm
- `PUT /api/alarms/{id}` - Update alarm
- `DELETE /api/alarms/{id}` - Delete alarm

## Security

Smart Alarm implements security best practices:

- **Authentication**: JWT tokens with FIDO2 support and OAuth2 providers (Google, GitHub, Facebook, Microsoft)
- **Authorization**: Role-based access control (RBAC)
- **Token Management**: Redis-backed blacklist for token revocation
- **Input Validation**: FluentValidation for all inputs
- **Secrets Management**: Never hardcode secrets, use key vault providers
- **LGPD Compliance**: Privacy-by-design data handling

## Performance

The platform is optimized for performance and scalability:

- **Serverless Architecture**: Auto-scaling based on demand
- **Caching Strategy**: Distributed caching with Redis
- **Database Optimization**: Optimized queries and connection pooling
- **Background Processing**: Asynchronous task processing with Hangfire
- **Circuit Breakers**: Resilience patterns for external dependencies

## Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details on:

- Development workflow
- Code standards and conventions
- Testing requirements
- Pull request process

## Resources

- [Documentation](./docs/README.md) - Comprehensive technical documentation
- [Architecture Decisions](./docs/architecture/) - ADRs and design decisions  
- [API Reference](./docs/api/) - Detailed API documentation
- [Deployment Guide](./docs/deployment/) - Production deployment instructions

## Troubleshooting

Common issues and solutions can be found in our [Troubleshooting Guide](./docs/troubleshooting.md).

If you encounter issues not covered in the documentation, please [open an issue](https://github.com/arbgjr/smart-alarm/issues) with:

- Detailed problem description
- Steps to reproduce
- Environment information
- Relevant logs and error messages
