# ADR-002: Migration to a Single C# Backend

**Status:** Accepted  
**Date:** 2025-07-02  
**Authors:** Development Team  
**Decision Makers:** Product Owner, Tech Lead

## Context and Problem

Originally, our architecture envisioned multiple languages for the backend, but operational complexity, maintenance difficulty, and the need for standardization motivated the migration to a single stack.

## Decision

**The entire backend will be unified in C# (.NET)**, eliminating any dependency on Go, Python, Node.js, or other languages for core services. All services (alarm CRUD, AI/behavioral analysis, external integrations) will be implemented as independent .NET projects, preferably serverless (Azure Functions), following Clean Architecture and SOLID principles.

## Justification

- **Standardization and Simplicity:** A single language drastically reduces operational complexity, facilitates onboarding, maintenance, and system evolution.
- **Performance and Productivity:** Modern .NET (6+) offers performance close to low-level languages for CRUD operations, with excellent productivity and development tools.
- **AI and Analysis:** ML.NET covers most required machine learning scenarios. Integrations with TensorFlow/PyTorch can be done via .NET libraries, and Python.NET will only be used in exceptional cases, always encapsulated.
- **External Integrations:** The .NET ecosystem has mature libraries for integrations with third-party APIs, notifications, calendars, etc.
- **Serverless and Cloud-Native:** Azure Functions natively supports C#, with competitive cold start and easy integration with Azure services.
- **Security and Testability:** Clean Architecture, SOLID, automated tests, structured logging, JWT/FIDO2 authentication, documentation via Swagger/OpenAPI.

## Implications

### Positive

- Reduction of operational costs and DevOps overhead.
- Uniform codebase, easy to audit and evolve.
- Faster onboarding and lower learning curve.
- Standardized analysis, profiling, and monitoring tools.
- Adequate performance for all product scenarios.

### Negative

- Initial migration cost of legacy services.
- Possible need for integration with Python libraries for very specific AI (mitigated by encapsulated Python.NET).

### Technical Architecture

- **AlarmService:** Alarm CRUD, business rules, and notifications, all in C#.
- **AnalysisService:** AI/behavioral analysis with ML.NET, Python interoperability only if necessary.
- **IntegrationService:** External integrations (calendars, notifications, etc.) via .NET libraries.
- All services as independent .NET projects, preferably Azure Functions.

## Implementation Plan

1. Setup of C# infrastructure and implementation of AlarmService.
2. Implementation of IntegrationService and migration of external integrations.
3. Implementation of AnalysisService with ML.NET and Python interoperability only if necessary.
4. Integration, performance, and full validation tests.

Throughout the process, keep legacy services only until full validation of the new C# services.

## Success Criteria

- Equivalent or better latency for 95% of CRUD operations.
- 99.9% uptime for all services.
- 30% reduction in time to implement new features.
- 50% reduction in time spent on infrastructure maintenance.

---

*This ADR will be reviewed after full implementation or if significant new information arises during the migration process.*
