# Padrão de Resposta de Erro - SmartAlarm API

Todos os endpoints da API retornam erros no formato padronizado `ErrorResponse`.

## Exemplo de resposta de erro

```json
{
  "statusCode": 400,
  "title": "Erro de validação",
  "detail": "Um ou mais campos estão inválidos.",
  "type": "ValidationError",
  "traceId": "string",
  "timestamp": "2025-07-05T12:00:00Z",
  "validationErrors": [
    {
      "field": "name",
      "message": "Nome do alarme é obrigatório.",
      "code": "NotEmptyValidator",
      "attemptedValue": ""
    }
  ],
  "extensions": {}
}
```

## Campos

- `statusCode`: Código HTTP do erro
- `title`: Título resumido do erro
- `detail`: Descrição detalhada
- `type`: Tipo de erro (ValidationError, BusinessError, SystemError, etc.)
- `traceId`: ID único para rastreamento
- `timestamp`: Data/hora UTC do erro
- `validationErrors`: Lista de erros de validação (quando aplicável)
- `extensions`: Dados adicionais

## Tipos de erro

- `ValidationError`: Erros de validação de dados
- `BusinessError`: Regras de negócio
- `SystemError`: Erros internos
- `AuthenticationError`: Falha de autenticação
- `NotFoundError`: Recurso não encontrado

Consulte também o arquivo `ErrorMessages.json` para mensagens centralizadas.
