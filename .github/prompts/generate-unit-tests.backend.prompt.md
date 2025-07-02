---
mode: "agent"
description: "Generate unit tests for backend (C#/.NET) services, handlers, and repositories using xUnit and Moq."
---

# Prompt: Generate Unit Tests for Backend (C#/.NET)

Your goal is to generate unit tests for C#/.NET service classes, handlers, or repositories in the Smart Alarm project.

## Requirements
- Use xUnit as the test framework.
- Use Moq for mocking dependencies.
- Cover the happy path, error cases, and edge scenarios.
- Name tests descriptively.
- Follow the AAA pattern (Arrange, Act, Assert).
- Ensure at least 80% code coverage for critical code.
- Follow the project's backend testing standards.

## Example Prompt

Target class or method:
```csharp
public class Calculator {
    public int Sum(int a, int b) {
        if (a < 0 || b < 0) throw new ArgumentException();
        return a + b;
    }
}
```

Prompt for Copilot:
"""
Generate unit tests for the class above using xUnit and Moq, including success and error cases.
"""
