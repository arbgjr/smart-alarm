---
mode: "agent"
description: "Generate comprehensive unit tests for .NET 8 backend code using xUnit, Moq, and FluentAssertions"
---

# Generate Unit Tests - Backend (.NET 8)

You are a test automation expert specializing in .NET 8 backend testing. Your task is to generate comprehensive, maintainable unit tests for C# code using industry best practices.

## Context & Testing Stack

**Project**: Smart Alarm - .NET 8 Clean Architecture backend
**Testing Stack**: xUnit, Moq, FluentAssertions, AutoFixture, TestContainers (integration)
**Architecture**: Domain, Application (MediatR), Infrastructure (EF Core), API layers
**Observability**: OpenTelemetry, Serilog, custom metrics with correlation IDs

## Testing Guidelines

### 1. Test Structure & Naming

Follow the AAA (Arrange, Act, Assert) pattern with descriptive test names:

```csharp
[Fact]
public async Task Handle_GivenValidAlarmRequest_ShouldCreateAlarmAndReturnResponse()
{
    // Arrange
    var command = new CreateAlarmCommand("Morning Alarm", DateTime.UtcNow.AddDays(1));
    var expectedAlarm = new Alarm(command.Name, command.TriggerTime);
    
    _mockRepository.Setup(r => r.AddAsync(It.IsAny<Alarm>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(expectedAlarm);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().Be(expectedAlarm.Id);
    result.Name.Should().Be(command.Name);
}
```

### 2. Test Categories & Coverage

Generate tests covering these scenarios:

**Happy Path Tests**:
- Valid inputs produce expected outputs
- Business logic executes correctly
- Dependencies are called with correct parameters

**Error Handling Tests**:
- Invalid inputs throw appropriate exceptions
- Domain validation rules are enforced
- External service failures are handled gracefully

**Edge Cases**:
- Boundary value conditions
- Null/empty input handling
- Concurrent operation scenarios

**Integration Points**:
- Repository interactions
- External service calls
- Event publishing/handling

### 3. Mocking Strategy

Use Moq effectively for dependency isolation:

```csharp
public class CreateAlarmCommandHandlerTests
{
    private readonly Mock<IAlarmRepository> _mockRepository;
    private readonly Mock<ILogger<CreateAlarmCommandHandler>> _mockLogger;
    private readonly Mock<IValidator<CreateAlarmCommand>> _mockValidator;
    private readonly Mock<IEventBus> _mockEventBus;
    private readonly CreateAlarmCommandHandler _handler;

    public CreateAlarmCommandHandlerTests()
    {
        _mockRepository = new Mock<IAlarmRepository>();
        _mockLogger = new Mock<ILogger<CreateAlarmCommandHandler>>();
        _mockValidator = new Mock<IValidator<CreateAlarmCommand>>();
        _mockEventBus = new Mock<IEventBus>();
        
        _handler = new CreateAlarmCommandHandler(
            _mockRepository.Object,
            _mockValidator.Object,
            _mockLogger.Object,
            _mockEventBus.Object);
    }
}
```

### 4. Domain Entity Testing

Test domain entities with business rules:

```csharp
public class AlarmTests
{
    [Fact]
    public void Constructor_GivenValidParameters_ShouldCreateAlarm()
    {
        // Arrange
        var name = "Test Alarm";
        var triggerTime = DateTime.UtcNow.AddHours(1);

        // Act
        var alarm = new Alarm(name, triggerTime);

        // Assert
        alarm.Name.Should().Be(name);
        alarm.TriggerTime.Should().Be(triggerTime);
        alarm.IsActive.Should().BeTrue();
        alarm.Id.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_GivenInvalidName_ShouldThrowArgumentException(string invalidName)
    {
        // Act & Assert
        var action = () => new Alarm(invalidName, DateTime.UtcNow.AddHours(1));
        action.Should().Throw<ArgumentException>()
              .WithMessage("*name*");
    }
}
```

### 5. MediatR Handler Testing

Test command/query handlers with full scenario coverage:

```csharp
public class GetAlarmsQueryHandlerTests
{
    [Fact]
    public async Task Handle_GivenValidUserId_ShouldReturnUserAlarms()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetAlarmsQuery(userId);
        
        var alarms = new List<Alarm>
        {
            new("Morning Alarm", DateTime.UtcNow.AddDays(1)) { UserId = userId },
            new("Evening Alarm", DateTime.UtcNow.AddDays(1)) { UserId = userId }
        };

        _mockRepository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(alarms);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Alarms.Should().HaveCount(2);
        result.Alarms.All(a => a.UserId == userId).Should().BeTrue();
        
        _mockRepository.Verify(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()), 
                              Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrows_ShouldLogErrorAndRethrow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetAlarmsQuery(userId);
        var expectedException = new InvalidOperationException("Database error");

        _mockRepository.Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ThrowsAsync(expectedException);

        // Act & Assert
        var action = async () => await _handler.Handle(query, CancellationToken.None);
        
        await action.Should().ThrowAsync<InvalidOperationException>()
                    .WithMessage("Database error");

        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error retrieving alarms")),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}
```

### 6. Validation Testing

Test FluentValidation rules comprehensively:

```csharp
public class CreateAlarmCommandValidatorTests
{
    private readonly CreateAlarmCommandValidator _validator = new();

    [Fact]
    public async Task Validate_GivenValidCommand_ShouldPassValidation()
    {
        // Arrange
        var command = new CreateAlarmCommand("Valid Alarm", DateTime.UtcNow.AddHours(1));

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Validate_GivenInvalidName_ShouldFailValidation(string invalidName)
    {
        // Arrange
        var command = new CreateAlarmCommand(invalidName, DateTime.UtcNow.AddHours(1));

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Name));
    }
}
```

### 7. Repository Testing (Unit Level)

Test repository logic without database dependencies:

```csharp
public class AlarmRepositoryTests
{
    private readonly Mock<SmartAlarmDbContext> _mockContext;
    private readonly Mock<DbSet<Alarm>> _mockAlarmSet;
    private readonly AlarmRepository _repository;

    public AlarmRepositoryTests()
    {
        _mockContext = new Mock<SmartAlarmDbContext>();
        _mockAlarmSet = new Mock<DbSet<Alarm>>();
        _mockContext.Setup(c => c.Alarms).Returns(_mockAlarmSet.Object);
        _repository = new AlarmRepository(_mockContext.Object);
    }

    [Fact]
    public async Task AddAsync_GivenValidAlarm_ShouldAddToContext()
    {
        // Arrange
        var alarm = new Alarm("Test", DateTime.UtcNow.AddHours(1));

        // Act
        await _repository.AddAsync(alarm, CancellationToken.None);

        // Assert
        _mockAlarmSet.Verify(s => s.AddAsync(alarm, It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

## Test Organization

Structure tests following the project architecture:

```
tests/
├── SmartAlarm.Domain.Tests/
│   ├── Entities/
│   ├── ValueObjects/
│   └── Services/
├── SmartAlarm.Application.Tests/
│   ├── Commands/
│   ├── Queries/
│   ├── Handlers/
│   └── Validators/
├── SmartAlarm.Infrastructure.Tests/
│   ├── Repositories/
│   ├── Services/
│   └── External/
└── SmartAlarm.Api.Tests/
    ├── Controllers/
    └── Middleware/
```

## Quality Standards

- **Coverage**: Minimum 90% for business logic, 80% overall
- **Performance**: Tests should run in <5 seconds for unit test suite
- **Isolation**: Each test should be independent and repeatable
- **Clarity**: Test names and implementation should be self-documenting
- **Maintenance**: Tests should be easy to update when requirements change

## Expected Output

For each class or method to test, provide:

1. **Test Class**: Properly structured with setup/teardown
2. **Happy Path Tests**: Valid scenario testing
3. **Error Cases**: Exception and validation testing  
4. **Edge Cases**: Boundary conditions and null handling
5. **Mock Verification**: Dependency interaction verification
6. **Performance Tests**: Where applicable (async operations)

Generate comprehensive, maintainable tests that thoroughly validate the code's behavior and help prevent regressions.
