> **Status:** Etapa 5 (API Layer) concluída e validada em 05/07/2025. Todos os requisitos de autenticação JWT, extração de contexto, tratamento de erros e testes estão implementados e documentados.

# Authentication & Security – SmartAlarm API

## JWT Authentication

- All protected endpoints require a valid JWT Bearer token.
- Tokens must be signed with a strong secret (minimum 32 chars, never exposed in code or logs).
- Only tokens issued by the configured Issuer and Audience are accepted.
- Expired, malformed, or invalid tokens result in HTTP 401 with a generic error message.

## User Context Extraction

- The `CurrentUserService` provides access to UserId, Email, Roles, and authentication status.
- Never trust user input for identity; always use claims from the validated JWT.

## Error Handling

- Authentication failures return HTTP 401 with no sensitive details.
- Authorization failures return HTTP 403.
- All errors are logged via Serilog, but tokens and sensitive data are never logged.

## Security Checklist

- [x] JWT validation (issuer, audience, signature, expiration)
- [x] Secure error handling (no sensitive info)
- [x] User context extraction via claims
- [x] Logging without sensitive data
- [x] Automated tests for all auth scenarios

## Example: .http Usage

```http
# Replace <token> with a valid JWT
GET https://localhost:5001/api/alarms
Authorization: Bearer <token>
```

## Configuration (appsettings.json)

```json
"Jwt": {
  "Secret": "REPLACE_WITH_A_STRONG_SECRET_KEY_32CHARS",
  "Issuer": "SmartAlarmIssuer",
  "Audience": "SmartAlarmAudience"
}
```

## References

- [OWASP JWT Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/JSON_Web_Token_for_Java_Cheat_Sheet.html)
- [Microsoft Docs: JWT Bearer Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwtbearer/)
