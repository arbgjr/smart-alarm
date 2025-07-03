# Error Handling and Validation Patterns

## Overview

This document describes the standardized error handling and validation patterns implemented in the Smart Alarm API. All errors follow a consistent format and use centralized error messages for maintainability and internationalization.

## Error Response Format

All API errors return a standardized `ErrorResponse` object:

```json
{
  "statusCode": 400,
  "title": "Validation Failed",
  "detail": "One or more validation errors occurred.",
  "type": "ValidationError",
  "traceId": "00-abc123...",
  "timestamp": "2024-01-15T10:30:00Z",
  "validationErrors": [
    {
      "field": "Name",
      "message": "Nome do alarme é obrigatório.",
      "code": "Required",
      "attemptedValue": ""
    }
  ],
  "extensions": {
    "additionalInfo": "value"
  }
}
```

### Error Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `statusCode` | int | HTTP status code |
| `title` | string | Brief error title |
| `detail` | string | Detailed error description |
| `type` | string | Error category (ValidationError, BusinessError, SystemError, AuthenticationError) |
| `traceId` | string | Unique identifier for request tracing |
| `timestamp` | DateTime | When the error occurred (UTC) |
| `validationErrors` | array | List of field-specific validation errors (for validation failures) |
| `extensions` | object | Additional context-specific information |

## HTTP Status Codes

### Success Codes
- **200 OK** - Request successful, data returned
- **201 Created** - Resource created successfully
- **204 No Content** - Request successful, no data returned (e.g., DELETE operations)

### Client Error Codes
- **400 Bad Request** - Validation errors, malformed request
- **401 Unauthorized** - Authentication required or invalid
- **403 Forbidden** - Insufficient permissions
- **404 Not Found** - Resource not found
- **409 Conflict** - Business rule violation (e.g., duplicate alarm)

### Server Error Codes
- **500 Internal Server Error** - Unexpected server error
- **502 Bad Gateway** - External service error
- **503 Service Unavailable** - Service temporarily unavailable

## Error Types and Categories

### 1. Validation Errors (400 Bad Request)

**Type**: `ValidationError`

**Scenarios**:
- Required fields missing
- Invalid field formats
- Field length violations
- Data range violations

**Example**:
```json
{
  "statusCode": 400,
  "title": "Validation Failed",
  "detail": "Falha na validação dos dados fornecidos.",
  "type": "ValidationError",
  "validationErrors": [
    {
      "field": "Name",
      "message": "Nome do alarme é obrigatório.",
      "code": "Required"
    },
    {
      "field": "Time",
      "message": "A data/hora deve ser no futuro.",
      "code": "FutureDateTime"
    }
  ]
}
```

### 2. Business Errors (404 Not Found, 409 Conflict)

**Type**: `BusinessError` or `NotFoundError`

**Scenarios**:
- Entity not found
- Business rule violations
- Duplicate resources

**Example - Not Found**:
```json
{
  "statusCode": 404,
  "title": "Resource Not Found",
  "detail": "Alarme não encontrado.",
  "type": "NotFoundError"
}
```

**Example - Conflict**:
```json
{
  "statusCode": 409,
  "title": "Business Rule Violation",
  "detail": "Já existe um alarme com este nome para o usuário.",
  "type": "BusinessError"
}
```

### 3. Authentication Errors (401 Unauthorized, 403 Forbidden)

**Type**: `AuthenticationError`

**Scenarios**:
- Missing or invalid authentication token
- Expired token
- Insufficient permissions

**Example**:
```json
{
  "statusCode": 401,
  "title": "Authentication Required",
  "detail": "Token de autenticação inválido.",
  "type": "AuthenticationError"
}
```

### 4. System Errors (500 Internal Server Error)

**Type**: `SystemError`

**Scenarios**:
- Unexpected exceptions
- Database connection errors
- External service failures

**Example**:
```json
{
  "statusCode": 500,
  "title": "Internal Server Error",
  "detail": "Ocorreu um erro interno no servidor.",
  "type": "SystemError",
  "extensions": {
    "exceptionType": "SqlException"
  }
}
```

## Validation Rules

### Alarm Validation

#### CreateAlarmDto
| Field | Rules | Error Messages |
|-------|-------|----------------|
| `Name` | Required, 1-100 characters | `Validation.Required.AlarmName`, `Validation.Length.AlarmNameMaxLength` |
| `Time` | Required, future date/time | `Validation.Required.AlarmTime`, `Validation.Range.FutureDateTime` |
| `UserId` | Required, valid GUID | `Validation.Required.UserId` |

#### UpdateAlarmCommand
| Field | Rules | Error Messages |
|-------|-------|----------------|
| `AlarmId` | Required, valid GUID | `Validation.Required.AlarmId` |
| `Alarm` | Required, valid CreateAlarmDto | `Validation.Required.AlarmData` |

#### DeleteAlarmCommand
| Field | Rules | Error Messages |
|-------|-------|----------------|
| `AlarmId` | Required, valid GUID | `Validation.Required.AlarmId` |

#### GetAlarmByIdQuery
| Field | Rules | Error Messages |
|-------|-------|----------------|
| `Id` | Required, valid GUID | `Validation.Required.AlarmId` |

#### ListAlarmsQuery
| Field | Rules | Error Messages |
|-------|-------|----------------|
| `UserId` | Required, valid GUID | `Validation.Required.UserId` |

## Error Message Centralization

Error messages are stored in `/src/SmartAlarm.Api/Resources/ErrorMessages.json` and organized by category:

```json
{
  "Validation": {
    "Required": {
      "AlarmName": "Nome do alarme é obrigatório.",
      "AlarmTime": "Horário do alarme é obrigatório.",
      "UserId": "Usuário é obrigatório."
    },
    "Length": {
      "AlarmNameMaxLength": "Nome deve ter até {MaxLength} caracteres."
    }
  },
  "Business": {
    "AlarmNotFound": "Alarme não encontrado.",
    "AlarmAlreadyExists": "Já existe um alarme com este nome para o usuário."
  },
  "System": {
    "InternalServerError": "Ocorreu um erro interno no servidor."
  },
  "Authentication": {
    "InvalidToken": "Token de autenticação inválido."
  }
}
```

## Implementation Details

### Validation Pipeline

The validation system uses MediatR pipeline behavior (`ValidationBehavior<TRequest, TResponse>`) to automatically validate all commands and queries before they reach the handlers.

### Exception Handling

The global exception handling middleware (`ExceptionHandlingMiddleware`) catches all unhandled exceptions and converts them to standardized error responses:

1. **FluentValidation.ValidationException** → 400 Bad Request
2. **NotFoundException** → 404 Not Found  
3. **UnauthorizedAccessException** → 401 Unauthorized
4. **ArgumentException** → 400 Bad Request
5. **Exception** (generic) → 500 Internal Server Error

### Error Message Service

The `ErrorMessageService` loads error messages from the JSON file and provides methods to:
- Get messages by key path (e.g., "Validation.Required.AlarmName")
- Format messages with parameters (e.g., "Nome deve ter até {MaxLength} caracteres.")
- Check if a message key exists

## Testing

All validation and error handling components include comprehensive unit tests:

- **ErrorMessageServiceTests**: Tests message loading, formatting, and key existence
- **ValidationBehaviorTests**: Tests the MediatR validation pipeline
- **Validator Tests**: Tests all FluentValidation validators for proper error messages

## Best Practices

1. **Consistent Error Format**: Always use the standardized `ErrorResponse` format
2. **Meaningful Messages**: Provide clear, actionable error messages in Portuguese
3. **Trace IDs**: Include trace IDs for debugging and support
4. **Security**: Don't expose sensitive information in error messages
5. **Logging**: Log errors with appropriate levels (Warning for validation, Error for system errors)
6. **Testing**: Test both happy path and error scenarios
7. **Documentation**: Keep this documentation updated when adding new error types

## Client Integration

Frontend applications should:

1. Check the `statusCode` for the HTTP status
2. Display the `title` for user-friendly error summaries
3. Show `detail` for more specific error information
4. Process `validationErrors` array for field-specific validation feedback
5. Use `traceId` when reporting issues to support
6. Handle different error `type`s appropriately (show validation errors inline, system errors as notifications)

## Migration and Backward Compatibility

This error handling system replaces the previous basic error responses. New code should use the standardized format, while legacy error responses will be gradually migrated to maintain backward compatibility.