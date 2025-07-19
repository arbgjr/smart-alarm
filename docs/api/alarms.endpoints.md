# SmartAlarm API – Alarm Endpoints Documentation

This document provides detailed documentation for the Alarm endpoints exposed by the SmartAlarm API (Presentation Layer).

## Base Path

```
/api/v1/alarms
```

---

## Endpoints - Alarmes (SmartAlarm API)

Documentação dos endpoints RESTful para gerenciamento de alarmes.

## Autenticação

- JWT Bearer obrigatório em todos os endpoints (exceto login)

## Endpoints

### Criar Alarme

- **POST** `/api/v1/alarms`
- **Body:**

```json
{
  "name": "string",
  "time": "2025-07-05T08:00:00Z"
}
```

- **Response:** `201 Created`

```json
{
  "id": "guid",
  "name": "string",
  "time": "2025-07-05T08:00:00Z",
  "enabled": true,
  "userId": "guid"
}
```

### Listar Alarmes

- **GET** `/api/v1/alarms`
- **Response:** `200 OK`

```json
[
  {
    "id": "guid",
    "name": "string",
    "time": "2025-07-05T08:00:00Z",
    "enabled": true,
    "userId": "guid"
  }
]
```

### Buscar Alarme por ID

- **GET** `/api/v1/alarms/{id}`
- **Response:** `200 OK` ou `404 Not Found`

### Atualizar Alarme

- **PUT** `/api/v1/alarms/{id}`
- **Body:** igual ao de criação
- **Response:** `200 OK` ou `404 Not Found`

### Excluir Alarme

- **DELETE** `/api/v1/alarms/{id}`
- **Response:** `204 No Content` ou `404 Not Found`

## Respostas de Erro

- `400 Bad Request` (validação)
- `401 Unauthorized` (não autenticado)
- `404 Not Found` (não encontrado)
- `500 Internal Server Error` (erro interno)

## Observações

- Todos os endpoints retornam erros no padrão `ErrorResponse`.
- Consulte `/swagger` para documentação OpenAPI interativa.
