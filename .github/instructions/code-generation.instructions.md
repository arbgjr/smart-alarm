---
applyTo: "**"
---

# 🔧 Code Generation Instructions

## Fundamental Principles

### Architecture and Design
- Always apply SOLID principles and Clean Architecture
- Code must be testable, secure, and well documented
- Maintain consistency with patterns established in systemPatterns.md

### Code Quality
- Include proper error handling and validation
- Follow project naming and organization standards
- Implement structured logging where appropriate

### Knowledge Management
- Always consult the Memory Bank for project context
- Update the Memory Bank when explicitly requested
- Register technical debt and bugs found in the appropriate files

## Specific Guidelines

### Backend (C#/.NET)
- Use .NET 8.0 as the default version
- Implement Clean Architecture with clear layer separation
- Apply SOLID principles in all implementations
- Include validation with FluentValidation
- Implement consistent error handling

### Testing
- Always run tests with the parameter `--logger "console;verbosity=detailed"`
- Use the AAA pattern (Arrange, Act, Assert)
- Cover success, error, and edge cases
- Minimum 80% coverage for critical code

### Commands and Execution
- Always run dotnet commands with the `|| true` complement
- Include handling for failures in CI/CD pipelines
- Document dependencies and environment requirements
