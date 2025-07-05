# Exemplos de Respostas de Erro da API SmartAlarm

## 400 Bad Request – Erro de Validação

```json
{
  "statusCode": 400,
  "title": "Erro de validação",
  "detail": "Um ou mais campos estão inválidos.",
  "type": "ValidationError",
  "traceId": "string",
  "timestamp": "2025-07-04T12:00:00Z",
  "validationErrors": [
    {
      "field": "name",
      "message": "Validation.Required.AlarmName",
      "code": "NotEmptyValidator",
      "attemptedValue": null
    },
    {
      "field": "time",
      "message": "Validation.Range.FutureDateTime",
      "code": "GreaterThanValidator",
      "attemptedValue": "2023-01-01T00:00:00Z"
    }
  ],
  "extensions": {}
}
```

## 404 Not Found

```json
{
  "statusCode": 404,
  "title": "Recurso não encontrado",
  "detail": "O alarme solicitado não foi encontrado.",
  "type": "NotFoundError",
  "traceId": "string",
  "timestamp": "2025-07-04T12:00:00Z",
  "validationErrors": [],
  "extensions": {}
}
```

## 500 Internal Server Error

```json
{
  "statusCode": 500,
  "title": "Erro interno do servidor",
  "detail": "Ocorreu um erro inesperado.",
  "type": "SystemError",
  "traceId": "string",
  "timestamp": "2025-07-04T12:00:00Z",
  "validationErrors": [],
  "extensions": {}
}
```
