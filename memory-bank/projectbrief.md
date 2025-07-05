# Smart Alarm — Project Brief

## Objective

Develop a backend platform and services for intelligent management of alarms, routines, and integrations, focusing on accessibility, security, modularity, and scalability.

## Scope

- Backend exclusively in C# (.NET 8.0+), Clean Architecture, serverless (OCI Functions)
- RESTful APIs for alarms, AI, and external integrations
- Security: JWT/FIDO2, LGPD, structured logging, Application Insights
- Automated tests (xUnit, Moq), minimum 80% coverage for critical code
- Documentation via Swagger/OpenAPI and Markdown files

## Constraints

- Only C# for backend
- Serverless standard: Oracle Cloud Infrastructure
- No exposure of secrets in code or logs

## Atualização - Etapa 5 (API Layer)

A camada de API foi implementada conforme Clean Architecture, com controllers RESTful, middlewares globais, documentação Swagger/OpenAPI e testes automatizados cobrindo todos os fluxos MVP. Governança, documentação e critérios de pronto validados.
