---
applyTo: "services/alarm-service/**"
---
# Instructions for Alarm Service (Node.js)

## Structure
- Separate business logic, infrastructure, and controllers.
- Use interfaces for data and service contracts.
- Prefer dependency injection.

## Code Standards
- camelCase for variables and functions, PascalCase for classes.
- Always handle errors and return clear messages.
- Document public functions with JSDoc.

## Testing
- Write unit and integration tests for critical endpoints.
- Use mocks for external dependencies.
- Include error and success cases.

## Security
- Never expose secrets in code.
- Validate all inputs received by APIs.

## Example Endpoint
```typescript
/**
 * Creates a new alarm
 */
export const createAlarm = async (input: CreateAlarmDTO): Promise<Alarm> => {
    if (!input.time) {
        throw new Error("Time required");
    }
    // ...creation logic...
};
```
