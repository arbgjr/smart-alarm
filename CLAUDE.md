# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Quick Commands

### Build and Test

- **Build solution**: `dotnet build SmartAlarm.sln --no-restore`
- **Restore dependencies**: `dotnet restore SmartAlarm.sln`
- **Run all tests**: `dotnet test --logger "console;verbosity=detailed"`
- **Run integration tests**: `dotnet test --filter Category=Integration --logger "console;verbosity=detailed"`
- **Run OAuth tests**: `dotnet test --filter "FullyQualifiedName~OAuth" --logger "console;verbosity=detailed"`
- **Code coverage**: `dotnet test --collect:"XPlat Code Coverage" --settings tests/coverlet.runsettings`

### Frontend Development

- **Start frontend dev**: `cd frontend && npm run dev`
- **Frontend build**: `cd frontend && npm run build`
- **Frontend tests**: `cd frontend && npm test`
- **E2E tests**: `cd frontend && npm run test:e2e`
- **OAuth E2E tests**: `cd frontend && npm run test:e2e:auth`
- **E2E with Docker**: `cd frontend && npm run test:e2e:docker`
- **Lint frontend**: `cd frontend && npm run lint`

### Infrastructure

- **Start dev environment**: `docker compose up -d`
- **Start all services**: `docker compose -f docker-compose.dev.yml up -d`
- **Integration test environment**: `docker compose -f docker-compose.test.yml up -d`

## Architecture Overview

Smart Alarm follows **Clean Architecture** with strict separation of concerns:

### Backend (.NET 8)

```
src/
├── SmartAlarm.Domain/          # Business entities, value objects, domain services
├── SmartAlarm.Application/     # Use cases, CQRS handlers, DTOs, validators
├── SmartAlarm.Infrastructure/  # External concerns (DB, storage, messaging)
├── SmartAlarm.Api/            # REST API controllers, middleware
├── SmartAlarm.KeyVault/       # Multi-provider secrets management
└── SmartAlarm.Observability/  # Tracing, metrics, logging abstractions
```

### Frontend (React PWA)

```
frontend/src/
├── components/  # Reusable React components
├── pages/       # Route-specific page components  
├── stores/      # Zustand state management
├── services/    # API client services
├── utils/       # ML, sync, PWA utilities
└── hooks/       # Custom React hooks
```

### Microservices

```
services/
├── ai-service/         # ML.NET powered AI capabilities
├── alarm-service/      # Background processing with Hangfire
└── integration-service/ # External API integrations with Polly
```

## Key Patterns and Technologies

### CQRS with MediatR

- Commands for mutations, Queries for reads
- All handlers follow pattern: validate → execute → log/trace
- Use `FluentValidation` for all input validation

### Multi-Provider Infrastructure

- **Database**: PostgreSQL (dev/test), Oracle (production) via auto-detection
- **Storage**: MinIO (dev), OCI Object Storage (prod)
- **Secrets**: HashiCorp Vault (dev), OCI/Azure Vault (prod)
- **Cache/Blacklist**: Redis for distributed caching and JWT token revocation

### Authentication & Security

- JWT with FIDO2 support and OAuth2 providers (Google, GitHub, Facebook, Microsoft)
- Redis-backed token blacklist for secure logout
- Rate limiting (disabled in testing environments)
- OWASP security headers and validation
- PKCE implementation for OAuth2 security

### Observability Stack

- **Logging**: Serilog with structured logging
- **Tracing**: OpenTelemetry with Jaeger export
- **Metrics**: OpenTelemetry with Prometheus export
- All handlers must include tracing activities

## Development Guidelines

### Code Conventions

- **PascalCase**: Classes, records, public methods/properties
- **camelCase**: Local variables, private fields (with `_` prefix)
- **Async methods**: Always suffix with `Async`, use `async/await` properly
- **Validation**: Every command/DTO must have FluentValidation validator

### Testing Requirements

- **AAA Pattern**: Arrange, Act, Assert for all tests
- **Categories**: Use `[Category("Integration")]` for integration tests
- **Mocking**: Use Moq for dependencies in unit tests
- **Assertions**: Use FluentAssertions for readable test assertions
- **Docker**: Integration tests require `docker compose up -d` for infrastructure

### Frontend Specific

- **PWA Features**: Service worker, offline sync, push notifications
- **State Management**: Zustand with persistence for app state
- **API Integration**: React Query for caching and synchronization
- **Accessibility**: WCAG AAA compliance, neurodivergent-friendly design
- **Testing**: Vitest for unit tests, Playwright for E2E

## Key Files and Entry Points

### Backend Entry Points

- `src/SmartAlarm.Api/Program.cs` - Main API configuration and startup
- `src/SmartAlarm.Application/Commands/` - CQRS command handlers
- `src/SmartAlarm.Application/Queries/` - CQRS query handlers
- `src/SmartAlarm.Domain/Entities/Alarm.cs` - Core domain entity

### Frontend Entry Points

- `frontend/src/main.tsx` - React app entry point
- `frontend/src/App.tsx` - Main app component with routing
- `frontend/vite.config.ts` - Build configuration with PWA setup
- `frontend/src/stores/` - Zustand state management

### Configuration Files

- `Directory.Packages.props` - Centralized NuGet package versions
- `SmartAlarm.sln` - Main solution file with all projects
- `docker-compose.yml` - Development infrastructure services
- `tests/coverlet.runsettings` - Code coverage configuration

## Environment Configuration

### Required Environment Variables

```bash
# Database
DATABASE_PROVIDER=PostgreSQL  # or Oracle for production
CONNECTION_STRING=Host=localhost;Database=SmartAlarm;Username=dev;Password=dev

# Security
JWT_SECRET_KEY=your-jwt-secret
JWT_BLACKLIST_ENABLED=true

# OAuth2 Providers (stored in KeyVault)
GOOGLE_CLIENT_ID=your-google-client-id
GOOGLE_CLIENT_SECRET=your-google-client-secret
GITHUB_CLIENT_ID=your-github-client-id
GITHUB_CLIENT_SECRET=your-github-client-secret
FACEBOOK_CLIENT_ID=your-facebook-client-id
FACEBOOK_CLIENT_SECRET=your-facebook-client-secret
MICROSOFT_CLIENT_ID=your-microsoft-client-id
MICROSOFT_CLIENT_SECRET=your-microsoft-client-secret

# Redis
REDIS_CONNECTION_STRING=localhost:6379

# Messaging
RABBITMQ_HOST=localhost
RABBITMQ_USER=guest
RABBITMQ_PASS=guest

# Secrets Management
KEYVAULT_ENABLED=true
HASHICORP_VAULT__SERVER_ADDRESS=http://localhost:8200
HASHICORP_VAULT__TOKEN=dev-token

# Observability
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
PROMETHEUS_ENDPOINT=http://localhost:9090
```

### Provider Auto-Detection

The system automatically detects and configures providers:

- **Development**: PostgreSQL + MinIO + HashiCorp Vault
- **Production**: Oracle + OCI Object Storage + OCI Vault
- **Testing**: In-memory providers with Testcontainers

## Special Considerations

### Neurodivergent-Friendly Design

- Accessibility-first approach with WCAG AAA compliance
- Reduced cognitive load and clear error messages
- Privacy-preserving ML for behavioral patterns
- Offline-first PWA design for reliability

### Serverless Architecture

- Designed for OCI Functions deployment
- Stateless operations with external state management
- Multi-cloud compatibility through provider abstractions

### Security Focus

- Never commit secrets (use KeyVault providers)
- JWT token blacklist for secure logout
- Rate limiting and OWASP security headers
- LGPD/GDPR compliant data handling

## Common Tasks

### Adding a New Feature

1. Create command/query in `Application` layer with validator
2. Implement handler with proper logging and tracing
3. Add controller endpoint in `Api` layer
4. Write comprehensive tests (unit + integration)
5. Update frontend components if needed
6. Run full test suite and coverage analysis

### Database Changes

1. Update domain entities in `Domain` layer
2. Create EF Core migration: `dotnet ef migrations add MigrationName`
3. Update repository interfaces and implementations
4. Add/update database seeders if needed
5. Test with both PostgreSQL and Oracle providers

### Adding External Integration

1. Create abstraction in `Domain` layer
2. Implement provider in `Infrastructure` layer
3. Use Polly for resilience patterns (circuit breaker, retry)
4. Add comprehensive integration tests
5. Update configuration and environment variables
