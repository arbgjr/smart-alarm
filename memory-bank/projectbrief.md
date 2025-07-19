# Smart Alarm — Project Brief

## Objective

Develop a backend platform and services for intelligent management of alarms, routines, and integrations, focusing on accessibility, security, modularity, and scalability.

## Scope

- **Backend**: Exclusively in C# (.NET 8.0+), Clean Architecture, designed for serverless (OCI Functions).
- **APIs**: RESTful APIs for alarms, AI, and external integrations, documented with Swagger/OpenAPI.
- **Security**: JWT/FIDO2 authentication, token revocation, multi-provider KeyVault (OCI, Azure, Vault), LGPD compliance.
- **Observability**: Comprehensive stack with structured logging (Serilog), distributed tracing (OpenTelemetry, Jaeger), and metrics (Prometheus, Grafana).
- **Testing**: Automated tests (xUnit, Moq), with a minimum 80% coverage target for critical code.
- **Persistence**: Multi-provider repository pattern (PostgreSQL for Dev/Test, Oracle for Production).
- **Documentation**: Technical documentation in Markdown within the `docs/` folder.

## Constraints

- Only C# for the backend.
- The primary deployment target for serverless is Oracle Cloud Infrastructure (OCI).
- No exposure of secrets in code, logs, or artifacts.


