---
mode: "agent"
description: "Generate unit tests for frontend (React/TypeScript) components and hooks using Testing Library and Vitest."
---

# Prompt: Generate Unit Tests for Frontend (React/TypeScript)

Your goal is to generate unit tests for React components or hooks in the Smart Alarm project.

## Requirements
- Use Testing Library and Vitest as the test framework.
- Cover the happy path, error cases, and edge scenarios.
- Use mocks for external dependencies and context.
- Name tests descriptively.
- Follow the AAA pattern (Arrange, Act, Assert).
- Ensure accessibility checks (where applicable).
- Follow the project's frontend testing standards.

## Example Prompt

Target component or hook:
```typescript
export const sum = (a: number, b: number): number => {
    if (typeof a !== "number" || typeof b !== "number") {
        throw new Error("Invalid parameters");
    }
    return a + b;
};
```

Prompt for Copilot:
"""
Generate unit tests for the function above using Testing Library and Vitest, including success and error cases.
"""
