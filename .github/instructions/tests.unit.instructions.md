---
applyTo: "tests/**/*.cs"
---
# Unit Test Instructions

## 1. Testing Philosophy

Unit tests verify individual components in isolation, focusing on business logic, domain rules, and individual service behaviors. They should be fast, reliable, and independent of external dependencies like databases, file systems, or network calls.

## 2. Test Structure Standards

### AAA Pattern (Arrange, Act, Assert)
```csharp
[Fact]
public void Should_CreateAlarm_When_ValidDataProvided()
{
    // Arrange
    var userId = Guid.NewGuid();
    var alarmName = "Morning Alarm";
    var triggerTime = DateTime.UtcNow.AddHours(8);
    
    // Act
    var alarm = new Alarm(userId, alarmName, triggerTime);
    
    // Assert
    alarm.UserId.Should().Be(userId);
    alarm.Name.Should().Be(alarmName);
    alarm.TriggerTime.Should().Be(triggerTime);
    alarm.IsActive.Should().BeTrue();
}
```

### Test Naming Convention
- **Pattern**: `Should_ExpectedBehavior_When_StateUnderTest`
- **Examples**: 
  - `Should_ThrowArgumentException_When_NameIsEmpty`
  - `Should_ReturnTrue_When_UserCanCreateAlarm`
  - `Should_PublishDomainEvent_When_AlarmIsTriggered`

## 3. Testing Framework Stack

### Core Testing Tools
- **xUnit**: Primary test framework
- **FluentAssertions**: Readable assertions (`result.Should().NotBeNull()`)
- **Moq**: Mocking framework for dependencies
- **Bogus**: Test data generation for complex scenarios

### Test Categories
```csharp
[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class AlarmTests { }

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class CreateAlarmCommandHandlerTests { }
```

## 4. Domain Layer Testing

### Entity Testing
```csharp
public class AlarmEntityTests
{
    [Fact]
    public void Should_CalculateNextTriggerTime_When_AlarmIsRecurring()
    {
        // Arrange
        var alarm = new Alarm(Guid.NewGuid(), "Test", DateTime.UtcNow);
        alarm.SetRecurrencePattern(RecurrencePattern.Daily);
        
        // Act
        var nextTrigger = alarm.CalculateNextTriggerTime();
        
        // Assert
        nextTrigger.Should().BeAfter(alarm.TriggerTime);
        nextTrigger.Should().Be(alarm.TriggerTime.AddDays(1));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Should_ThrowArgumentException_When_NameIsInvalid(string invalidName)
    {
        // Arrange & Act
        var action = () => new Alarm(Guid.NewGuid(), invalidName, DateTime.UtcNow);
        
        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("Alarm name cannot be empty*");
    }
}
```

### Value Object Testing
```csharp
public class TimeRangeTests
{
    [Fact]
    public void Should_BeEqual_When_SameTimeValues()
    {
        // Arrange
        var timeRange1 = new TimeRange(TimeSpan.FromHours(9), TimeSpan.FromHours(17));
        var timeRange2 = new TimeRange(TimeSpan.FromHours(9), TimeSpan.FromHours(17));
        
        // Assert
        timeRange1.Should().Be(timeRange2);
        timeRange1.GetHashCode().Should().Be(timeRange2.GetHashCode());
    }
}
```

### Domain Service Testing
```csharp
public class AlarmDomainServiceTests
{
    private readonly Mock<IAlarmRepository> _alarmRepositoryMock;
    private readonly Mock<ILogger<AlarmDomainService>> _loggerMock;
    private readonly AlarmDomainService _domainService;

    public AlarmDomainServiceTests()
    {
        _alarmRepositoryMock = new Mock<IAlarmRepository>();
        _loggerMock = new Mock<ILogger<AlarmDomainService>>();
        _domainService = new AlarmDomainService(_alarmRepositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Should_ReturnFalse_When_UserExceedsAlarmLimit()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingAlarms = GenerateAlarms(count: 10, userId);
        
        _alarmRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(existingAlarms);

        // Act
        var canCreate = await _domainService.CanUserCreateAlarmAsync(userId);

        // Assert
        canCreate.Should().BeFalse();
    }
}
```

## 5. Application Layer Testing

### Command Handler Testing
```csharp
public class CreateAlarmCommandHandlerTests
{
    private readonly Mock<IAlarmRepository> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMessagingService> _messagingMock;
    private readonly CreateAlarmCommandHandler _handler;

    public CreateAlarmCommandHandlerTests()
    {
        _repositoryMock = new Mock<IAlarmRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _messagingMock = new Mock<IMessagingService>();
        
        _handler = new CreateAlarmCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _messagingMock.Object);
    }

    [Fact]
    public async Task Should_CreateAlarmAndPublishEvent_When_CommandIsValid()
    {
        // Arrange
        var command = new CreateAlarmCommand
        {
            UserId = Guid.NewGuid(),
            Name = "Morning Alarm",
            TriggerTime = DateTime.UtcNow.AddHours(8)
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<Alarm>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _messagingMock.Verify(x => x.PublishAsync(It.IsAny<AlarmCreatedEvent>()), Times.Once);
    }
}
```

### Query Handler Testing
```csharp
public class GetUserAlarmsQueryHandlerTests
{
    [Fact]
    public async Task Should_ReturnUserAlarms_When_UserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var alarms = GenerateUserAlarms(userId, count: 3);
        
        _repositoryMock
            .Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(alarms);

        var query = new GetUserAlarmsQuery { UserId = userId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Alarms.Should().HaveCount(3);
        result.Alarms.Should().OnlyContain(a => a.UserId == userId);
    }
}
```

## 6. Infrastructure Layer Testing

### Repository Testing (Unit)
```csharp
public class EfAlarmRepositoryTests
{
    private DbContextOptions<SmartAlarmDbContext> GetInMemoryDbOptions()
    {
        return new DbContextOptionsBuilder<SmartAlarmDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task Should_SaveAndRetrieveAlarm_When_AddedToRepository()
    {
        // Arrange
        using var context = new SmartAlarmDbContext(GetInMemoryDbOptions());
        var repository = new EfAlarmRepository(context);
        var alarm = new Alarm(Guid.NewGuid(), "Test Alarm", DateTime.UtcNow);

        // Act
        await repository.AddAsync(alarm);
        await context.SaveChangesAsync();
        var retrievedAlarm = await repository.GetByIdAsync(alarm.Id);

        // Assert
        retrievedAlarm.Should().NotBeNull();
        retrievedAlarm.Id.Should().Be(alarm.Id);
        retrievedAlarm.Name.Should().Be(alarm.Name);
    }
}
```

## 7. Test Data Generation

### Using Bogus for Complex Test Data
```csharp
public class AlarmTestDataBuilder
{
    private readonly Faker<Alarm> _alarmFaker;

    public AlarmTestDataBuilder()
    {
        _alarmFaker = new Faker<Alarm>()
            .CustomInstantiator(f => new Alarm(
                f.Random.Guid(),
                f.Lorem.Sentence(3),
                f.Date.Future()))
            .RuleFor(a => a.IsActive, f => f.Random.Bool())
            .RuleFor(a => a.Volume, f => f.Random.Int(1, 100));
    }

    public Alarm Build() => _alarmFaker.Generate();
    public List<Alarm> BuildMany(int count) => _alarmFaker.Generate(count);
    
    public AlarmTestDataBuilder WithUserId(Guid userId)
    {
        _alarmFaker.RuleFor(a => a.UserId, userId);
        return this;
    }
}
```

## 8. Mocking Best Practices

### Mock Setup Patterns
```csharp
// Specific setup for expected calls
_repositoryMock
    .Setup(x => x.GetByIdAsync(It.Is<Guid>(id => id == expectedId)))
    .ReturnsAsync(expectedAlarm);

// Generic setup with any parameters
_repositoryMock
    .Setup(x => x.GetByUserIdAsync(It.IsAny<Guid>()))
    .ReturnsAsync(new List<Alarm>());

// Setup for throwing exceptions
_repositoryMock
    .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
    .ThrowsAsync(new NotFoundException("Alarm not found"));
```

### Verification Patterns
```csharp
// Verify method was called with specific parameters
_repositoryMock.Verify(x => x.AddAsync(
    It.Is<Alarm>(a => a.UserId == userId && a.Name == expectedName)), 
    Times.Once);

// Verify method was never called
_messagingMock.Verify(x => x.PublishAsync(It.IsAny<DomainEvent>()), Times.Never);

// Verify all expected interactions occurred
_repositoryMock.VerifyAll();
```

## 9. Test Execution & Coverage

### Running Tests
```bash
# Run all unit tests
dotnet test --filter "Category=Unit" --logger "console;verbosity=detailed"

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --filter "Category=Unit"

# Run specific test class
dotnet test --filter "FullyQualifiedName~AlarmDomainServiceTests"
```

### Coverage Requirements
- **Domain Logic**: 95%+ coverage for business rules and domain entities
- **Application Handlers**: 90%+ coverage for command and query handlers
- **Infrastructure**: 80%+ coverage for repository implementations
- **Overall Target**: 85%+ code coverage for critical business logic

## 10. Common Anti-Patterns to Avoid

### What NOT to Test
- **Framework Code**: Don't test Entity Framework, ASP.NET Core, or third-party libraries
- **Simple Properties**: Don't test basic getters/setters without logic
- **Private Methods**: Test through public interfaces, not implementation details

### Mocking Anti-Patterns
```csharp
// ❌ Don't mock what you don't own
var mockDateTime = new Mock<DateTime>();

// ❌ Don't mock value objects
var mockGuid = new Mock<Guid>();

// ✅ Use test doubles or builders instead
var testDateTime = new DateTime(2023, 1, 1);
var testGuid = Guid.NewGuid();
```

## Quality Checklist

- [ ] All tests follow AAA pattern
- [ ] Test names clearly describe the scenario
- [ ] Tests are isolated and can run in any order
- [ ] Mocks are used appropriately for dependencies
- [ ] Business logic is thoroughly tested
- [ ] Edge cases and error conditions are covered
- [ ] Test data is generated consistently
- [ ] No hardcoded values or magic numbers
- [ ] Tests run fast (under 100ms each)
- [ ] Code coverage meets minimum thresholds
