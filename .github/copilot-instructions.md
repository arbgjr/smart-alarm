# Smart Alarm – GitHub Copilot Instructions

## 1. Core Objective & Architecture

**Objective**: Develop a secure, modular, and scalable backend platform for intelligent alarm and routine management.

**Architecture**: The project is built on **.NET 8** following **Clean Architecture**. All services must strictly separate concerns into `Domain`, `Application`, `Infrastructure`, and `Api` layers. The system is designed for a **serverless-first** deployment on **OCI Functions**.

**Key Architectural Principles**:
- **SOLID**: Adhere to SOLID principles in all new and refactored code.
- **Validation**: Use **FluentValidation** for all incoming DTOs and commands.
- **Immutability**: Prefer immutable data structures where possible.
- **DI**: Leverage dependency injection for loose coupling and testability.

---

## 2. Development Workflow & Commands

**Dependencies & Build**:
- Restore dependencies: `dotnet restore SmartAlarm.sln`
- Build the solution: `dotnet build SmartAlarm.sln --no-restore`

**Testing**:
- **Run all tests**: `dotnet test --logger "console;verbosity=detailed"`
- **Run integration tests**: `dotnet test --filter Category=Integration --logger "console;verbosity=detailed"`
- **Code Coverage**: `dotnet test --collect:"XPlat Code Coverage" --settings tests/coverlet.runsettings`
- **Test Infrastructure**: Integration tests require local Docker services. Start them with `docker compose up -d --build`.
- **Test Pattern**: All tests must follow the **Arrange, Act, Assert (AAA)** pattern.

---

## 3. Key Technologies & Patterns

- **Persistence**: Multi-provider repository pattern.
  - **Development/Testing**: PostgreSQL
  - **Production**: Oracle
- **Messaging**: RabbitMQ is used across all environments, configured via environment variables for production clustering/SSL.
- **Storage**:
  - **Development/Staging**: MinIO (via `SmartStorageService` auto-detection).
  - **Production**: OCI Object Storage.
- **Security & Secrets**:
  - **Authentication**: JWT with FIDO2 support. A Redis-backed blocklist handles token revocation.
  - **Secrets Management**: A multi-provider `IKeyVaultService` abstracts secret sources (HashiCorp Vault for Dev, OCI/Azure Vault for Prod). **Never hardcode secrets.**
- **Observability**:
  - **Logging**: Structured logging with **Serilog**.
  - **Tracing & Metrics**: **OpenTelemetry** is the standard. Traces are exported via OTLP, and metrics are exposed for Prometheus.

---

## 4. Code Conventions

- **Naming**:
  - `PascalCase` for classes, records, public methods, and properties.
  - `camelCase` for local variables and private fields.
  - `_camelCase` for private fields.
- **File Organization**: Group files by feature and responsibility within the established layer structure.
- **Asynchronous Code**: Use `async/await` correctly. Avoid `async void` except for event handlers. Append `Async` to all awaitable method names.

---

## 5. Code Generation Examples

**Example: MediatR Handler with Validation, Logging, and Tracing**
```csharp
// In Application Layer

public class CreateAlarmCommandHandler : IRequestHandler<CreateAlarmCommand, AlarmResponse>
{
    private readonly ILogger<CreateAlarmCommandHandler> _logger;
    private readonly IValidator<CreateAlarmCommand> _validator;
    private readonly IAlarmRepository _alarmRepository;
    private readonly SmartAlarmActivitySource _activitySource; // OpenTelemetry

    // Constructor...

    public async Task<AlarmResponse> Handle(CreateAlarmCommand request, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity(nameof(CreateAlarmCommandHandler));

        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Validation failed for CreateAlarmCommand: {@Errors}", validationResult.Errors);
            activity?.SetStatus(ActivityStatusCode.Error, "Validation failed");
            throw new ValidationException(validationResult.Errors);
        }

        var alarm = new Alarm(request.Name, request.TriggerTime);
        await _alarmRepository.AddAsync(alarm, cancellationToken);

        _logger.LogInformation("New alarm created with ID {AlarmId}", alarm.Id);
        activity?.AddEvent(new ActivityEvent("Alarm Created"));
        
        return new AlarmResponse(alarm.Id, alarm.Name);
    }
}
```

**Example: xUnit Test with Moq**
```csharp
// In tests/SmartAlarm.Application.Tests

[Fact]
public async Task Handle_GivenInvalidCommand_ShouldThrowValidationException()
{
    // Arrange
    var mockValidator = new Mock<IValidator<CreateAlarmCommand>>();
    var invalidCommand = new CreateAlarmCommand { Name = "" }; // Invalid name
    var validationResult = new ValidationResult(new[] { new ValidationFailure("Name", "Name is required") });

    mockValidator.Setup(v => v.ValidateAsync(invalidCommand, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(validationResult);

    var handler = new CreateAlarmCommandHandler(
        Mock.Of<ILogger<CreateAlarmCommandHandler>>(),
        mockValidator.Object,
        Mock.Of<IAlarmRepository>(),
        new SmartAlarmActivitySource()
    );

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ValidationException>(
        () => handler.Handle(invalidCommand, CancellationToken.None)
    );

    exception.Errors.Should().HaveCount(1);
    exception.Errors.First().PropertyName.Should().Be("Name");
}
```
