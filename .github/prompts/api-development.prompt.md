---
mode: "agent"
description: "Implementar API com autenticação JWT, validação de entrada, tratamento de erros, logging estruturado, documentação OpenAPI e testes."
---

# API Development

Develop a REST API called <ask the API name> following the guidelines in [api-security.instructions.md](../instructions/api-security.instructions.md).

## Mandatory Requirements

- Implement JWT authentication
- Strict input validation using DTOs
- Standardized error handling
- Structured logging with correlationId
- OpenAPI/Swagger documentation
- Unit and integration tests
- Follow Clean Architecture and SOLID principles.
- Validate and handle input errors.
- Document the endpoint with JSDoc.
- Include example tests using `.rest` file that follows the standards of [RFC 9110 HTTP Semantics](https://www.rfc-editor.org/rfc/rfc9110.html)) for the endpoint.

## Expected Structure

- Controllers: Only routing and validation
- Services: Business logic
- Repositories: Data access
- DTOs: Input/output contracts

## Example of Expected Response

Always specify the files where each code should be placed and include necessary imports.
