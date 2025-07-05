# SmartAlarm API – Alarm Endpoints Documentation

This document provides detailed documentation for the Alarm endpoints exposed by the SmartAlarm API (Presentation Layer).

## Base Path

```
/api/v1/alarms
```

---

## Endpoints

### 1. Create Alarm

- **POST** `/api/v1/alarms`
- **Description:** Creates a new alarm for the authenticated user.
- **Request Body:**
  - `name` (string, required): Alarm name (1-100 chars)
  - `time` (string, required): DateTime in ISO 8601 format (must be in the future)
- **Responses:**
  - `201 Created`: Returns the created alarm object
  - `400 Bad Request`: Validation error (see error schema)
  - `401 Unauthorized`: User not authenticated

#### Example Request

```json
{
  "name": "Wake Up",
  "time": "2025-07-06T07:00:00Z"
}
```

#### Example Response (201)

```json
{
  "id": "...",
  "name": "Wake Up",
  "time": "2025-07-06T07:00:00Z",
  "userId": "..."
}
```

---

### 2. List Alarms

- **GET** `/api/v1/alarms`
- **Description:** Lists all alarms for the authenticated user.
- **Responses:**
  - `200 OK`: Array of alarm objects
  - `401 Unauthorized`: User not authenticated

#### Example Response

```json
[
  {
    "id": "...",
    "name": "Wake Up",
    "time": "2025-07-06T07:00:00Z",
    "userId": "..."
  }
]
```

---

### 3. Get Alarm by Id

- **GET** `/api/v1/alarms/{id}`
- **Description:** Retrieves a specific alarm by its unique identifier.
- **Parameters:**
  - `id` (GUID, required): Alarm identifier
- **Responses:**
  - `200 OK`: Alarm object
  - `404 Not Found`: Alarm not found

---

### 4. Update Alarm

- **PUT** `/api/v1/alarms/{id}`
- **Description:** Updates an existing alarm.
- **Parameters:**
  - `id` (GUID, required): Alarm identifier (must match body)
- **Request Body:**
  - `alarmId` (GUID, required): Alarm identifier
  - `name` (string, optional): New name
  - `time` (string, optional): New time (must be in the future)
- **Responses:**
  - `200 OK`: Updated alarm object
  - `400 Bad Request`: Validation error or id mismatch
  - `404 Not Found`: Alarm not found

---

### 5. Delete Alarm

- **DELETE** `/api/v1/alarms/{id}`
- **Description:** Deletes an alarm by its unique identifier.
- **Parameters:**
  - `id` (GUID, required): Alarm identifier
- **Responses:**
  - `204 No Content`: Alarm deleted
  - `404 Not Found`: Alarm not found

---

## Error Response Schema

```json
{
  "statusCode": 400,
  "title": "Validation error",
  "detail": "One or more fields are invalid.",
  "type": "ValidationError",
  "traceId": "...",
  "timestamp": "2025-07-05T12:00:00Z",
  "validationErrors": [
    {
      "field": "Name",
      "message": "Validation.Required.AlarmName",
      "code": "NotEmptyValidator",
      "attemptedValue": null
    }
  ]
}
```

---

## Notes

- All endpoints require authentication (JWT Bearer).
- All responses are in JSON format.
- For full OpenAPI/Swagger documentation, access `/swagger` in the running API.
