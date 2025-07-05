> **Status:** Etapa 5 (API Layer) concluída e validada em 05/07/2025. Todos os requisitos de Clean Architecture, autenticação, validação, logging, tracing, métricas e testes estão implementados e documentados.

# SmartAlarm.Api - AlarmService API Documentation

## Overview

RESTful API for managing alarms. Follows Clean Architecture, SOLID, validation (FluentValidation), error handling, logging (Serilog), tracing (OpenTelemetry), and JWT authentication.

## Base URL

```
http://localhost:5000/api/v1/alarms
```

## Authentication

- All endpoints require JWT Bearer token in the `Authorization` header.
- The `sub` claim in the JWT is used as the UserId.

## Endpoints

### Create Alarm

- **POST** `/api/v1/alarms`
- **Body:**

```json
{
  "name": "Wake Up",
  "time": "2025-07-03T07:00:00Z",
  "enabled": true
}
```

- **Response:** `201 Created`

```json
{
  "id": "<alarm_id>",
  "name": "Wake Up",
  "time": "2025-07-03T07:00:00Z",
  "enabled": true,
  "userId": "<user_id>"
}
```

### List Alarms (User)

- **GET** `/api/v1/alarms`
- **Response:** `200 OK` (array of alarms)

### Get Alarm by Id

- **GET** `/api/v1/alarms/{id}`
- **Response:** `200 OK` (alarm) or `404 Not Found`

### Update Alarm

- **PUT** `/api/v1/alarms/{id}`
- **Body:**

```json
{
  "id": "<alarm_id>",
  "name": "Wake Up Updated",
  "time": "2025-07-03T07:30:00Z",
  "enabled": false
}
```

- **Response:** `200 OK` (updated alarm) or `404 Not Found`

### Delete Alarm

- **DELETE** `/api/v1/alarms/{id}`
- **Response:** `204 No Content` or `404 Not Found`

## Error Handling

- `400 Bad Request`: Validation errors (see details in response)
- `401 Unauthorized`: Missing/invalid JWT
- `404 Not Found`: Resource not found
- `500 Internal Server Error`: Unexpected errors (logged)

## Observability

- All requests are logged (Serilog) and traced (OpenTelemetry)
- Metrics exposed via Prometheus endpoint

## Example JWT Payload

```json
{
  "sub": "<user_id>",
  "name": "John Doe",
  "exp": 9999999999
}
```

## .http File

- Veja o arquivo `SmartAlarm.Api.http` para exemplos de requisições.

## Contact

- Para dúvidas técnicas, consulte a documentação do projeto ou entre em contato com o time backend.

## Serverless (OCI Function)

Além da API REST, o fluxo de criação de alarme está disponível como handler serverless compatível com OCI Functions, seguindo Clean Architecture e padrões de segurança.

- **Handler:** `AlarmFunction` (ver documentação técnica em `docs/AlarmFunction.md`)
- **Deploy:** Automatizado via script PowerShell e OCI CLI
- **Parâmetros:** Segredos via KeyVault, ambiente via variáveis
- **Testes:** Cobertura mínima de 80% (unitário/integrado)

Consulte a documentação técnica para detalhes de uso, exemplos e integração serverless.

## Conformidade

- Todos os endpoints, autenticação, validação, logging, tracing e métricas implementados conforme padrões.
- Testes unitários e integrados cobrindo todos os fluxos críticos.
- Documentação e exemplos atualizados.
