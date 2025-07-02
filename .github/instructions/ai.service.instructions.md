---
applyTo: "services/ai-service/**"
---
# Instructions for Node.js Services (AI Service)

## Structure
- Separate business logic, infrastructure, and controllers.
- Use classes and interfaces to define clear contracts.
- Prefer dependency injection to facilitate testing.

## Code Standards
- Use camelCase for variables and functions, PascalCase for classes.
- Always handle errors and return clear messages.
- Document public functions with JSDoc.

## Testing
- Write unit tests for each business function.
- Use mocks for external dependencies.
- Include error and success cases.

## Security
- Never expose secrets in code.
- Validate all inputs received by APIs.

## Example Function
```typescript
/**
 * Processes an AI request
 */
export const processRequest = async (input: string): Promise<Result> => {
    if (!input) {
        throw new Error("Input required");
    }
    // ...business logic...
};
```
