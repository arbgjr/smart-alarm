---
mode: "agent"
description: "Implementar revisão de código seguindo princípios SOLID, segurança, cobertura de testes e documentação."
---

# Prompt: Code Review

Your goal is to review the proposed code for the file <ask the file path> or all Smart Alarm project.

## Requirements:
- According to the file type follow the rules defined in 
  - [`.github\instructions\code-review.dotnet.instructions.md`](.github\instructions\code-review.dotnet.instructions.md), 
  - [`.github\instructions\code-review.frontend.instructions.md`](.github\instructions\code-review.frontend.instructions.md) 
  - Or [`.github\instructions\code-review.infrastructure.instructions.md`](.github\instructions\code-review.infrastructure.instructions.md) 
- Point out security, performance, and maintainability issues.
- If necessary, suggest improvements for architecture and clarity.
- If exists, indicate possible technical debt points.
- Follow the project's coding standards and best practices.
