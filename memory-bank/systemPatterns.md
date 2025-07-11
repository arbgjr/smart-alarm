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
- Implement integration tests para serviços externos usando HTTP health checks para verificar disponibilidade.
- Para testes de MinIO, Vault, e outros serviços, usar HttpClient para simplificar verificações de disponibilidade.
- Organizar testes de integração por categoria para permitir execução seletiva (MinIO, Vault, Database, etc.).

### Integration Tests

- **Categorização**: Usar filtros Category e Trait para organizar e executar testes específicos
  - Essential: MinIO, Vault, PostgreSQL, RabbitMQ
  - Observability: Grafana, Loki, Jaeger, Prometheus
  - Exemplo: `dotnet test --filter "Category=Integration&Trait=Essential"`
- **Verificação de Saúde**: Preferir HTTP health endpoints para verificações de disponibilidade
  - MinIO: `/minio/health/live`
  - Vault: `/v1/sys/health` ou `/v1/sys/seal-status`
  - PostgreSQL: Usar `pg_isready`
  - RabbitMQ: Usar `rabbitmqctl status`
  - Grafana: `/api/health`
  - Prometheus: `/-/healthy`
  - Loki: `/ready`
- **Orquestração com Docker**:
  - Usar script `docker-test.sh` com verificação dinâmica de serviços
  - Inicializar serviços condicionalmente baseado nos testes a executar
  - Implementar diagnósticos detalhados para falhas
  - Oferecer modos seletivos de execução (essentials, observability, debug)
- **Isolamento e Reprodutibilidade**:
  - Criar rede Docker dedicada para testes (`smartalarm-test`)
  - Reiniciar serviços entre execuções para garantir estado limpo
  - Parametrizar testes para facilitar execução em CI/CD
  - Fornecer logs detalhados para diagnóstico de falhas

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

**✅ IMPLEMENTADO**: Todos os handlers e pontos críticos da Application Layer implementam tracing distribuído (OpenTelemetry, Application Insights) e coleta de métricas customizadas.

**Padrões obrigatórios para novos handlers, comandos e queries:**

#### Distributed Tracing (OBRIGATÓRIO)

```csharp
public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken)
{
    using var activity = SmartAlarmTracing.ActivitySource.StartActivity("HandlerName.Handle");
    activity?.SetTag("entity.id", request.Id.ToString());
    activity?.SetTag("operation.type", "create"); // create, read, update, delete, list
    
    try
    {
        // Business logic
        var result = await DoWork(request);
        activity?.SetStatus(ActivityStatusCode.Ok);
        SmartAlarmMetrics.SuccessCounter.Add(1);
        return result;
    }
    catch (Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        SmartAlarmMetrics.ErrorCounter.Add(1);
        throw;
    }
}
```

#### Structured Logging (OBRIGATÓRIO)

```csharp
// Sucesso
_logger.LogInformation("Entity created: {EntityId} for user {UserId}", entity.Id, userId);

// Avisos
_logger.LogWarning("Entity not found: {EntityId}", entityId);

// Erros
_logger.LogError(ex, "Failed to process request for user {UserId}", userId);
```

#### Métricas Customizadas (OBRIGATÓRIO)

```csharp
// Contadores de operações
SmartAlarmMetrics.EntityCreatedCounter.Add(1);
SmartAlarmMetrics.ValidationErrorsCounter.Add(1);

// Histogramas de performance (opcional)
using var timer = SmartAlarmMetrics.HandlerDuration.CreateTimer();
```

#### Code Review Checklist

- [ ] Handler cria activity com nome descritivo
- [ ] Activity inclui tags relevantes (entity.id, user.id, operation.type)
- [ ] Activity status é definido corretamente (Ok/Error)
- [ ] Logs estruturados usam parâmetros ao invés de interpolação
- [ ] Métricas são atualizadas para sucessos e erros
- [ ] Nenhuma informação sensível em logs/traces

**📚 Documentação completa**: Consulte `docs/architecture/observability-patterns.md` para exemplos detalhados e padrões de implementação.

## API Layer - Padrões e Critérios

- Controllers RESTful para todos os recursos MVP
- Middlewares globais: logging (Serilog), tracing (OpenTelemetry), autenticação (JWT), validação (FluentValidation), tratamento de erros (ExceptionHandlingMiddleware)
- Documentação automática via Swagger/OpenAPI
- Resposta de erro padronizada (ErrorResponse)
- Testes xUnit cobrindo sucesso, erro e edge cases (mínimo 80% de cobertura)
- Governança: checklist de PR, ADR atualizado, Memory Bank atualizado

Consulte ADR-005 para detalhes e justificativas.

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
