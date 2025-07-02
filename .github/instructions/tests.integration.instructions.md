---
applyTo: "tests/integration/**"
---
describe("Fluxo de integração", () => {
# Instructions for Integration Tests

## Standards
- Use Vitest or Jest for integration tests.
- Test complete flows between services and APIs.
- Use mocks only for untested external dependencies.
- Validate responses, status, and side effects.

## Example
```typescript
describe("Integration flow", () => {
    it("should successfully integrate with external service", async () => {
        // Arrange
        // Act
        // Assert
    });
});
```
