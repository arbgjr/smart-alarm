---
applyTo: "services/integration-service/**"
---
# Instructions for Integration Service

## Structure
- Separate external integrations into isolated modules.
- Use interfaces to abstract integrations.
- Document endpoints and integration flows.

## Code Standards
- camelCase for variables and functions, PascalCase for classes.
- Always handle errors and return clear messages.
- Use logs to track integration failures.

## Testing
- Write unit tests for integration adapters.
- Use mocks for external APIs.
- Include error and success cases.

## Security
- Never expose secrets in code.
- Validate and sanitize data received from integrations.

## Example Function
```typescript
/**
 * Checks status in external system
 */
export const checkStatus = async (id: string): Promise<Status> => {
    if (!id) {
        throw new Error("ID required");
    }
    // ...integration logic...
};
```
