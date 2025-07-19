---
applyTo: "tests/integration/**/*.cs"
---
# Integration Test Instructions

## 1. Integration Testing Philosophy

Integration tests verify that multiple system components work together correctly, including database operations, message publishing, external API calls, and end-to-end workflows. They test the interaction between layers and services in a controlled environment.

## 2. Test Categories & Organization

### Test Categories
```csharp
[Trait("Category", "Integration")]
[Trait("Group", "Database")]
public class AlarmRepositoryIntegrationTests { }

[Trait("Category", "Integration")]
[Trait("Group", "Messaging")]
public class EventPublishingIntegrationTests { }

[Trait("Category", "Integration")]
[Trait("Group", "ExternalApi")]
public class CalendarSyncIntegrationTests { }

[Trait("Category", "Integration")]
[Trait("Group", "EndToEnd")]
public class AlarmWorkflowIntegrationTests { }
```

### Execution Strategies
```bash
# Run all integration tests
dotnet test --filter "Category=Integration"

# Run specific integration groups
dotnet test --filter "Category=Integration&Group=Database"
dotnet test --filter "Category=Integration&Group=Messaging"

# Run essential integration tests only
dotnet test --filter "Category=Integration&Group=Database|Group=Messaging"
```

## 3. Test Environment Setup

### Docker Compose Infrastructure
```yaml
# docker-compose.test.yml
version: '3.8'
services:
  postgres-test:
    image: postgres:15
    environment:
      POSTGRES_DB: smartalarm_test
      POSTGRES_USER: test_user
      POSTGRES_PASSWORD: test_password
    ports:
      - "5433:5432"
  
  rabbitmq-test:
    image: rabbitmq:3-management
    environment:
      RABBITMQ_DEFAULT_USER: test_user
      RABBITMQ_DEFAULT_PASS: test_password
    ports:
      - "5673:5672"
      - "15673:15672"
```

### Test Fixtures
```csharp
public class DatabaseFixture : IDisposable
{
    public SmartAlarmDbContext Context { get; private set; }
    public string ConnectionString { get; private set; }

    public DatabaseFixture()
    {
        ConnectionString = "Host=localhost;Port=5433;Database=smartalarm_test;Username=test_user;Password=test_password";
        
        var options = new DbContextOptionsBuilder<SmartAlarmDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        Context = new SmartAlarmDbContext(options);
        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Context.Database.EnsureDeleted();
        Context.Dispose();
    }
}

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture> { }
```

## 4. Database Integration Tests

### Repository Integration Testing
```csharp
[Collection("Database")]
[Trait("Category", "Integration")]
[Trait("Group", "Database")]
public class AlarmRepositoryIntegrationTests
{
    private readonly DatabaseFixture _fixture;
    private readonly IAlarmRepository _repository;

    public AlarmRepositoryIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _repository = new EfAlarmRepository(_fixture.Context);
    }

    [Fact]
    public async Task Should_PersistAndRetrieveAlarm_When_SavedToDatabase()
    {
        // Arrange
        var alarm = new Alarm(Guid.NewGuid(), "Integration Test Alarm", DateTime.UtcNow.AddHours(1));
        
        // Act
        await _repository.AddAsync(alarm);
        await _fixture.Context.SaveChangesAsync();
        
        var retrievedAlarm = await _repository.GetByIdAsync(alarm.Id);
        
        // Assert
        retrievedAlarm.Should().NotBeNull();
        retrievedAlarm.Id.Should().Be(alarm.Id);
        retrievedAlarm.Name.Should().Be(alarm.Name);
        retrievedAlarm.CreatedAt.Should().BeCloseTo(alarm.CreatedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Should_HandleConcurrentUpdates_When_MultipleClientsModifyAlarm()
    {
        // Arrange
        var alarm = new Alarm(Guid.NewGuid(), "Concurrent Test", DateTime.UtcNow);
        await _repository.AddAsync(alarm);
        await _fixture.Context.SaveChangesAsync();

        // Act - Simulate concurrent modification
        var alarm1 = await _repository.GetByIdAsync(alarm.Id);
        var alarm2 = await _repository.GetByIdAsync(alarm.Id);

        alarm1.UpdateName("Updated by Client 1");
        alarm2.UpdateName("Updated by Client 2");

        // First update should succeed
        await _fixture.Context.SaveChangesAsync();

        // Second update should detect concurrency conflict
        var action = async () => await _fixture.Context.SaveChangesAsync();

        // Assert
        await action.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }
}
```

### Database Migration Testing
```csharp
[Trait("Category", "Integration")]
[Trait("Group", "Database")]
public class DatabaseMigrationTests
{
    [Fact]
    public async Task Should_ApplyMigrationsSuccessfully_When_CreatingDatabase()
    {
        // Arrange
        var connectionString = "Host=localhost;Port=5433;Database=migration_test;Username=test_user;Password=test_password";
        
        using var context = new SmartAlarmDbContext(
            new DbContextOptionsBuilder<SmartAlarmDbContext>()
                .UseNpgsql(connectionString)
                .Options);

        try
        {
            // Act
            await context.Database.MigrateAsync();
            
            // Assert
            var canConnect = await context.Database.CanConnectAsync();
            canConnect.Should().BeTrue();
            
            // Verify key tables exist
            var tables = await context.Database.GetAppliedMigrationsAsync();
            tables.Should().NotBeEmpty();
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }
}
```

## 5. Messaging Integration Tests

### Event Publishing Tests
```csharp
[Trait("Category", "Integration")]
[Trait("Group", "Messaging")]
public class EventPublishingIntegrationTests
{
    private readonly IMessagingService _messagingService;
    private readonly Mock<ILogger<RabbitMqMessagingService>> _loggerMock;

    public EventPublishingIntegrationTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("RabbitMQ:ConnectionString", "amqp://test_user:test_password@localhost:5673")
            })
            .Build();

        _loggerMock = new Mock<ILogger<RabbitMqMessagingService>>();
        _messagingService = new RabbitMqMessagingService(_loggerMock.Object, configuration);
    }

    [Fact]
    public async Task Should_PublishAndReceiveMessage_When_EventIsPublished()
    {
        // Arrange
        var alarmCreatedEvent = new AlarmCreatedEvent
        {
            AlarmId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        var receivedEvent = default(AlarmCreatedEvent);
        var messageReceived = false;

        // Act
        await _messagingService.SubscribeAsync<AlarmCreatedEvent>("test-queue", (evt) =>
        {
            receivedEvent = evt;
            messageReceived = true;
            return Task.CompletedTask;
        });

        await _messagingService.PublishAsync(alarmCreatedEvent);

        // Wait for message processing
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Assert
        messageReceived.Should().BeTrue();
        receivedEvent.Should().NotBeNull();
        receivedEvent.AlarmId.Should().Be(alarmCreatedEvent.AlarmId);
    }
}
```

## 6. API Integration Tests

### Web Application Factory Setup
```csharp
public class SmartAlarmWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove the real database registration
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<SmartAlarmDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add in-memory database for testing
            services.AddDbContext<SmartAlarmDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            });

            // Override external services with mocks
            services.Replace(ServiceDescriptor.Scoped(_ => Mock.Of<IMessagingService>()));
        });

        builder.UseEnvironment("Testing");
    }
}

[Collection("WebApplication")]
[Trait("Category", "Integration")]
[Trait("Group", "Api")]
public class AlarmControllerIntegrationTests : IClassFixture<SmartAlarmWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly SmartAlarmWebApplicationFactory _factory;

    public AlarmControllerIntegrationTests(SmartAlarmWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Should_CreateAlarm_When_ValidRequestIsProvided()
    {
        // Arrange
        var request = new CreateAlarmRequest
        {
            Name = "Integration Test Alarm",
            TriggerTime = DateTime.UtcNow.AddHours(1),
            IsActive = true
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/alarms", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var createdAlarm = JsonSerializer.Deserialize<AlarmDto>(responseContent);
        
        createdAlarm.Should().NotBeNull();
        createdAlarm.Name.Should().Be(request.Name);
    }
}
```

## 7. External Service Integration Tests

### Health Check Integration
```csharp
[Trait("Category", "Integration")]
[Trait("Group", "ExternalApi")]
public class ExternalServiceHealthTests
{
    private readonly HttpClient _httpClient;

    public ExternalServiceHealthTests()
    {
        _httpClient = new HttpClient();
    }

    [Fact]
    public async Task Should_ConnectToMinIO_When_ServiceIsAvailable()
    {
        // Arrange
        var minioHealthEndpoint = "http://localhost:9000/minio/health/live";

        // Act
        var response = await _httpClient.GetAsync(minioHealthEndpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Should_ConnectToVault_When_ServiceIsAvailable()
    {
        // Arrange
        var vaultHealthEndpoint = "http://localhost:8200/v1/sys/health";

        // Act
        var response = await _httpClient.GetAsync(vaultHealthEndpoint);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
    }
}
```

### Calendar API Integration
```csharp
[Trait("Category", "Integration")]
[Trait("Group", "ExternalApi")]
public class GoogleCalendarIntegrationTests
{
    private readonly IGoogleCalendarClient _calendarClient;

    [Fact]
    public async Task Should_AuthenticateWithGoogle_When_ValidCredentialsProvided()
    {
        // This test would require test credentials or a test Google account
        // Skip if integration environment is not available
        if (!IsGoogleTestEnvironmentAvailable())
        {
            return; // Or use Skip.If() from xUnit
        }

        // Arrange
        var testUserId = Guid.NewGuid();

        // Act
        var calendars = await _calendarClient.GetCalendarsAsync(testUserId);

        // Assert
        calendars.Should().NotBeNull();
    }

    private bool IsGoogleTestEnvironmentAvailable()
    {
        return Environment.GetEnvironmentVariable("GOOGLE_TEST_CREDENTIALS") != null;
    }
}
```

## 8. End-to-End Workflow Tests

### Complete Alarm Lifecycle Test
```csharp
[Collection("WebApplication")]
[Trait("Category", "Integration")]
[Trait("Group", "EndToEnd")]
public class AlarmLifecycleIntegrationTests : IClassFixture<SmartAlarmWebApplicationFactory>
{
    [Fact]
    public async Task Should_CompleteAlarmLifecycle_When_UserCreatesAndTriggersAlarm()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var alarmRequest = new CreateAlarmRequest
        {
            Name = "E2E Test Alarm",
            TriggerTime = DateTime.UtcNow.AddMinutes(1),
            IsActive = true
        };

        // Act & Assert - Create Alarm
        var createResponse = await _client.PostAsync("/api/v1/alarms", 
            new StringContent(JsonSerializer.Serialize(alarmRequest), Encoding.UTF8, "application/json"));
        
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdAlarm = await createResponse.Content.ReadFromJsonAsync<AlarmDto>();

        // Act & Assert - Retrieve Alarm
        var getResponse = await _client.GetAsync($"/api/v1/alarms/{createdAlarm.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act & Assert - Trigger Alarm (simulate background job)
        var triggerResponse = await _client.PostAsync($"/api/v1/alarms/{createdAlarm.Id}/trigger", null);
        triggerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify alarm state changed
        var updatedAlarmResponse = await _client.GetAsync($"/api/v1/alarms/{createdAlarm.Id}");
        var updatedAlarm = await updatedAlarmResponse.Content.ReadFromJsonAsync<AlarmDto>();
        updatedAlarm.LastTriggeredAt.Should().NotBeNull();
    }
}
```

## 9. Performance Integration Tests

### Load Testing
```csharp
[Trait("Category", "Integration")]
[Trait("Group", "Performance")]
public class AlarmServicePerformanceTests
{
    [Fact]
    public async Task Should_HandleConcurrentAlarmCreation_When_MultipleUsersCreateAlarms()
    {
        // Arrange
        const int numberOfUsers = 100;
        const int alarmsPerUser = 5;
        var tasks = new List<Task>();

        // Act
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < numberOfUsers; i++)
        {
            var userId = Guid.NewGuid();
            
            for (int j = 0; j < alarmsPerUser; j++)
            {
                tasks.Add(CreateAlarmAsync(userId, $"Alarm {j}"));
            }
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(30));
        Console.WriteLine($"Created {numberOfUsers * alarmsPerUser} alarms in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
    }
}
```

## 10. Test Data Management

### Database Seeding for Integration Tests
```csharp
public static class TestDataSeeder
{
    public static async Task SeedTestDataAsync(SmartAlarmDbContext context)
    {
        if (!context.Users.Any())
        {
            var testUsers = new List<User>
            {
                new User(Guid.Parse("11111111-1111-1111-1111-111111111111"), "test.user1@example.com"),
                new User(Guid.Parse("22222222-2222-2222-2222-222222222222"), "test.user2@example.com")
            };

            context.Users.AddRange(testUsers);
            await context.SaveChangesAsync();
        }

        if (!context.Alarms.Any())
        {
            var testAlarms = GenerateTestAlarms();
            context.Alarms.AddRange(testAlarms);
            await context.SaveChangesAsync();
        }
    }
}
```

## 11. Test Execution & CI/CD Integration

### Test Execution Commands
```bash
# Start test infrastructure
docker-compose -f docker-compose.test.yml up -d

# Wait for services to be ready
./scripts/wait-for-services.sh

# Run integration tests with coverage
dotnet test --filter "Category=Integration" --collect:"XPlat Code Coverage"

# Run specific integration test groups
dotnet test --filter "Category=Integration&Group=Database"
dotnet test --filter "Category=Integration&Group=Messaging"

# Clean up test infrastructure
docker-compose -f docker-compose.test.yml down -v
```

### CI Pipeline Integration
```yaml
# GitHub Actions example
- name: Start Test Infrastructure
  run: docker-compose -f docker-compose.test.yml up -d

- name: Wait for Services
  run: ./scripts/wait-for-services.sh

- name: Run Integration Tests
  run: dotnet test --filter "Category=Integration" --logger "trx;LogFileName=integration-results.trx"

- name: Cleanup
  if: always()
  run: docker-compose -f docker-compose.test.yml down -v
```

## Quality Standards Checklist

- [ ] Tests verify real integration between components
- [ ] Database tests use actual database connections
- [ ] Message publishing/consuming works end-to-end
- [ ] External service integrations are tested (when available)
- [ ] Performance requirements are validated
- [ ] Test data is properly managed and cleaned up
- [ ] Tests can run in CI/CD pipeline
- [ ] Flaky tests are identified and fixed
- [ ] Test infrastructure is containerized
- [ ] Appropriate test categorization for selective execution
