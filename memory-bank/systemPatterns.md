# systemPatterns.md

## General Patterns of the Smart Alarm Project

### Architecture
- Follow Clean Architecture and SOLID principles (backend) and Atomic Design (frontend).
- Clearly separate domain, application, infrastructure, and presentation layers (backend) and components, pages, hooks, and contexts (frontend).
- Use dependency injection to facilitate testing and maintenance (backend) and component composition (frontend).
- Do not introduce types or values into the global scope.

### Code Organization
- Group files by business domain and responsibility (backend) and by feature/component (frontend).
- Keep tests close to the implemented code.
- On the frontend, organize components in folders by atomicity (atoms, molecules, organisms, pages).
- Document architectural decisions in `docs/architecture/`.

### Naming
- camelCase for variables, functions, and methods.
- PascalCase for classes, types, React components, and files.
- UPPER_SNAKE_CASE for global constants.
- Descriptive and clear names, no abbreviations.

### JavaScript/TypeScript (Frontend)
- Use double quotes for user-visible strings and single quotes for internal strings.
- Always use a semicolon at the end of statements.
- Prefer const for immutable variables and let for mutable ones. Avoid var.
- Use arrow functions `=>` and only add parentheses to parameters when necessary.
- Always use braces in conditionals and loops, with the opening brace on the same line.
- Use JSDoc to document public functions, classes, and interfaces (backend) and TypeScript types/interfaces for props and states (frontend).
- Do not export types or functions unnecessarily.
- On the frontend, use React.FC for functional components and prefer hooks for reusable logic.

### Tests
- Write unit tests for all business logic (Vitest or Jest).
- Include success, failure, and edge cases.
- Use mocks for external dependencies.
- Name tests descriptively (e.g., "should return error if...").
- Follow the AAA pattern (Arrange, Act, Assert).
- On the frontend, use Testing Library for React components, cover interactions, accessibility, and visual states.

### Error Handling
- Use try/catch to capture exceptions.
- Prefer throwing specific errors.
- Always log errors with relevant context.
- Validate all user inputs and external data.
- On the frontend, handle API errors and display user-friendly messages.

### Security
- Never expose credentials or secrets in code.
- Use environment variables for sensitive data.
- Validate and sanitize user inputs.
- Do not log sensitive information.
- On the frontend, never expose tokens or secrets in code or the bundle.
- Implement authentication and authorization when consuming APIs.
- Follow accessibility (WCAG) and privacy (LGPD) practices in the interface.

### Backend (APIs and Services)
- Follow Clean Architecture and SOLID principles for all backend logic.
- Separate controllers, services, repositories, and entities.
- Use DTOs for data input and output.
- Implement authentication and authorization as needed by the domain.
- Always validate and sanitize data received in endpoints.
- Use structured logs to track requests and errors, without exposing sensitive data.
- Implement unit and integration tests for critical endpoints and services.
- Document endpoints and API contracts (e.g., Swagger/OpenAPI).
- Prefer middlewares for error handling and authentication.
- Never expose secrets or sensitive variables in code or logs.

### Frontend (React/PWA)
- Follow Atomic Design for component organization.
- Use React, TypeScript, and hooks for UI logic.
- Separate components by atomicity (atoms, molecules, organisms, pages).
- Use context API for global state and custom hooks for shared logic.
- Implement accessibility (WCAG), responsiveness, and internationalization.
- Use Service Workers for PWA and notifications.
- Test components with Testing Library and simulate real interactions.
- Document props and component contracts with TypeScript.

### Integrations and APIs
- Use tools and best practices for OCI, OpenAI, GitHub, and other integrations.
- Always consult the specific instructions for each service (e.g., OCI Functions, SWA, etc).
- On the frontend, consume APIs via HttpClient/Fetch, handle errors and loading states.

### Development Flow
- Install dependencies with `npm install`.
- Compile with `npm run compile` (or `npm run build` on the frontend).
- Run tests with `npm run test:unit` and `npm run test:integration`.
- Use simulation and integration scripts to validate complete scenarios.
- On the frontend, use linters (ESLint), formatters (Prettier), and check accessibility (axe, Lighthouse).

### Review and Pull Requests
- Follow the conventional commit format.
- Clearly describe what changed and why.
- Include context, changes, tests performed, and pending items in the PR description.
- On the frontend, review accessibility, responsiveness, and visual impact of changes.

### Observabilidade: Tracing e Métricas
- TODO: Garantir que todos os handlers e pontos críticos da Application Layer implementem tracing distribuído (OpenTelemetry, Application Insights) e coleta de métricas customizadas.
- TODO: Validar em code review se spans, logs e métricas estão presentes e bem definidos.
- TODO: Documentar exemplos e padrões de uso para rastreamento e métricas no repositório.

Essas práticas são obrigatórias para todos os novos handlers, comandos e queries, conforme padrão do projeto.

### Good Practice Examples

#### Asynchronous Function (Backend)
```csharp
public async Task<User> GetUserByIdAsync(Guid id)
{
    if (id == Guid.Empty)
        throw new ArgumentException("ID is required");
    var user = await _userRepository.GetByIdAsync(id);
    if (user == null)
        throw new NotFoundException("User not found");
    return user;
}
```

#### Unit Test (Backend)
```csharp
[Fact]
public async Task Should_ThrowArgumentException_When_IdIsEmpty()
{
    var service = new UserService(...);
    await Assert.ThrowsAsync<ArgumentException>(() => service.GetUserByIdAsync(Guid.Empty));
}
```

#### React Component (Frontend)
```tsx
import React from "react";

type ButtonProps = {
    label: string;
    onClick: () => void;
};

export const Button: React.FC<ButtonProps> = ({ label, onClick }) => (
    <button onClick={onClick} aria-label={label}>
        {label}
    </button>
);
```

#### Component Test (Frontend)
```typescript
import { render, screen, fireEvent } from "@testing-library/react";
import { Button } from "./Button";

test("should call onClick when clicked", () => {
    const onClick = vi.fn();
    render(<Button label="Save" onClick={onClick} />);
    fireEvent.click(screen.getByRole("button"));
    expect(onClick).toHaveBeenCalled();
});
```
