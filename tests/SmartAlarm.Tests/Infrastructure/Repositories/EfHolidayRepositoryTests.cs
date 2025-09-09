using SmartAlarm.Domain.Abstractions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Infrastructure.Data;
using SmartAlarm.Infrastructure.Repositories.EntityFramework;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;

namespace SmartAlarm.Tests.Infrastructure.Repositories;

/// <summary>
/// Testes de integração para EfHolidayRepository
/// Seguindo padrões estabelecidos no projeto para testes de repositório.
/// </summary>
[Trait("Category", "Integration")]
public class EfHolidayRepositoryTests : IDisposable
{
    private readonly SmartAlarmDbContext _context;
    private readonly IHolidayRepository _repository;
    private readonly string _databaseName;

    public EfHolidayRepositoryTests()
    {
        _databaseName = $"TestDb_{Guid.NewGuid()}";
        
        // Verificar se deve usar PostgreSQL real (quando rodando em container)
        var useRealDatabase = Environment.GetEnvironmentVariable("POSTGRES_HOST") == "postgres" &&
                             !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("POSTGRES_USER"));
        
        // Log para debug - mostrar qual banco está sendo usado
        var logMessage = useRealDatabase ? "🐘 Usando PostgreSQL REAL do container" : "💾 Usando InMemory database (fallback)";
        Console.WriteLine($"[REPO TEST] {logMessage}");
        
        DbContextOptions<SmartAlarmDbContext> options;
        
        if (useRealDatabase)
        {
            // Usar PostgreSQL real do container para testes de integração
            var postgresHost = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
            var postgresPort = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
            var postgresUser = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "smartalarm";
            var postgresPassword = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "smartalarm123";
            var postgresDb = $"{Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "smartalarm"}_repo_test_{Guid.NewGuid():N}";
            
            var connectionString = $"Host={postgresHost};Port={postgresPort};Database={postgresDb};Username={postgresUser};Password={postgresPassword}";
            Console.WriteLine($"[REPO TEST] 🔗 Connection: Host={postgresHost}, Database={postgresDb}");
            
            options = new DbContextOptionsBuilder<SmartAlarmDbContext>()
                .UseNpgsql(connectionString)
                .Options;
        }
        else
        {
            // Fallback para InMemory quando PostgreSQL não está disponível
            Console.WriteLine($"[REPO TEST] 💾 InMemory Database: {_databaseName}");
            
            options = new DbContextOptionsBuilder<SmartAlarmDbContext>()
                .UseInMemoryDatabase(_databaseName)
                .Options;
        }

        _context = new SmartAlarmDbContext(options);
        
        // Create mock dependencies for holiday repository
        var logger = new Mock<ILogger<EfHolidayRepository>>();
        var meter = new Mock<SmartAlarmMeter>();
        var correlationContext = new Mock<ICorrelationContext>();
        var activitySource = new Mock<SmartAlarmActivitySource>();
        
        _repository = new EfHolidayRepository(_context, logger.Object, meter.Object, correlationContext.Object, activitySource.Object);
        
        // Garantir que o banco está criado
        _context.Database.EnsureCreated();
    }

    #region Add Tests

    [Fact]
    public async Task AddAsync_WithValidHoliday_ShouldSaveToDatabase()
    {
        // Arrange
        var holiday = new Holiday(
            DateTime.Parse("2024-12-25"), 
            "Christmas Day"
        );

        // Act
        await _repository.AddAsync(holiday);

        // Assert
        var savedHoliday = await _context.Holidays
            .FirstOrDefaultAsync(h => h.Id == holiday.Id);
        
        savedHoliday.Should().NotBeNull();
        savedHoliday!.Description.Should().Be("Christmas Day");
        savedHoliday.Date.Should().Be(DateTime.Parse("2024-12-25"));
    }

    [Fact]
    public async Task AddAsync_WithNullHoliday_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await _repository.Invoking(r => r.AddAsync(null!))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnHoliday()
    {
        // Arrange
        var holiday = new Holiday(
            DateTime.Parse("2024-01-01"), 
            "New Year's Day"
        );
        await _repository.AddAsync(holiday);

        // Act
        var result = await _repository.GetByIdAsync(holiday.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(holiday.Id);
        result.Description.Should().Be("New Year's Day");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistingId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithEmptyDatabase_ShouldReturnEmptyCollection()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleHolidays_ShouldReturnAllOrderedByDate()
    {
        // Arrange
        var christmas = new Holiday(DateTime.Parse("2024-12-25"), "Christmas");
        var newYear = new Holiday(DateTime.Parse("2024-01-01"), "New Year");
        var easter = new Holiday(DateTime.Parse("2024-03-31"), "Easter");

        await _repository.AddAsync(christmas);
        await _repository.AddAsync(newYear);
        await _repository.AddAsync(easter);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        var holidays = result.ToList();
        holidays.Should().HaveCount(3);
        holidays[0].Description.Should().Be("New Year");
        holidays[1].Description.Should().Be("Easter");
        holidays[2].Description.Should().Be("Christmas");
    }

    #endregion

    #region GetByDateAsync Tests

    [Fact]
    public async Task GetByDateAsync_WithExactMatch_ShouldReturnHoliday()
    {
        // Arrange
        var date = DateTime.Parse("2024-07-04");
        var holiday = new Holiday(date, "Independence Day");
        await _repository.AddAsync(holiday);

        // Act
        var result = await _repository.GetByDateAsync(DateOnly.FromDateTime(date));

        // Assert
        result.Should().HaveCount(1);
        result.First().Description.Should().Be("Independence Day");
    }

    [Fact]
    public async Task GetByDateAsync_WithRecurringHoliday_ShouldReturnRecurringHoliday()
    {
        // Arrange
        var recurringDate = DateTime.Parse("0001-12-25"); // Ano 1 = recorrente
        var holiday = new Holiday(recurringDate, "Christmas (Annual)");
        await _repository.AddAsync(holiday);

        // Act
        var result = await _repository.GetByDateAsync(DateOnly.Parse("2024-12-25"));

        // Assert
        result.Should().HaveCount(1);
        result.First().Description.Should().Be("Christmas (Annual)");
    }

    [Fact]
    public async Task GetByDateAsync_WithBothSpecificAndRecurring_ShouldReturnBothOrderedCorrectly()
    {
        // Arrange
        var recurringDate = DateTime.Parse("0001-12-25");
        var specificDate = DateTime.Parse("2024-12-25");
        
        var recurringHoliday = new Holiday(recurringDate, "Christmas (Annual)");
        var specificHoliday = new Holiday(specificDate, "Christmas 2024");
        
        await _repository.AddAsync(recurringHoliday);
        await _repository.AddAsync(specificHoliday);

        // Act
        var result = await _repository.GetByDateAsync(DateOnly.Parse("2024-12-25"));

        // Assert
        var holidays = result.ToList();
        holidays.Should().HaveCount(2);
        holidays[0].Description.Should().Be("Christmas (Annual)"); // Recorrente primeiro
        holidays[1].Description.Should().Be("Christmas 2024");
    }

    [Fact]
    public async Task GetByDateAsync_WithNoMatch_ShouldReturnEmptyCollection()
    {
        // Arrange
        var holiday = new Holiday(DateTime.Parse("2024-12-25"), "Christmas");
        await _repository.AddAsync(holiday);

        // Act
        var result = await _repository.GetByDateAsync(DateOnly.Parse("2024-07-04"));

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetRecurringAsync Tests

    [Fact]
    public async Task GetRecurringAsync_WithOnlyRecurringHolidays_ShouldReturnAll()
    {
        // Arrange
        var christmas = new Holiday(DateTime.Parse("0001-12-25"), "Christmas");
        var newYear = new Holiday(DateTime.Parse("0001-01-01"), "New Year");
        var nonRecurring = new Holiday(DateTime.Parse("2024-07-04"), "Independence Day 2024");

        await _repository.AddAsync(christmas);
        await _repository.AddAsync(newYear);
        await _repository.AddAsync(nonRecurring);

        // Act
        var result = await _repository.GetRecurringAsync();

        // Assert
        var holidays = result.ToList();
        holidays.Should().HaveCount(2);
        holidays.Should().Contain(h => h.Description == "Christmas");
        holidays.Should().Contain(h => h.Description == "New Year");
        holidays.Should().NotContain(h => h.Description == "Independence Day 2024");
    }

    [Fact]
    public async Task GetRecurringAsync_WithNoRecurringHolidays_ShouldReturnEmpty()
    {
        // Arrange
        var specificHoliday = new Holiday(DateTime.Parse("2024-07-04"), "Independence Day");
        await _repository.AddAsync(specificHoliday);

        // Act
        var result = await _repository.GetRecurringAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithExistingHoliday_ShouldUpdateDescription()
    {
        // Arrange
        var holiday = new Holiday(DateTime.Parse("2024-12-25"), "Christmas");
        await _repository.AddAsync(holiday);

        // Modificar a descrição
        holiday.UpdateDescription("Christmas Day - Updated");

        // Act
        await _repository.UpdateAsync(holiday);

        // Assert
        var updatedHoliday = await _repository.GetByIdAsync(holiday.Id);
        updatedHoliday.Should().NotBeNull();
        updatedHoliday!.Description.Should().Be("Christmas Day - Updated");
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingHoliday_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var holiday = new Holiday(DateTime.Parse("2024-12-25"), "Christmas");

        // Act & Assert
        await _repository.Invoking(r => r.UpdateAsync(holiday))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Holiday with ID {holiday.Id} not found for update.");
    }

    [Fact]
    public async Task UpdateAsync_WithNullHoliday_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await _repository.Invoking(r => r.UpdateAsync(null!))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingHoliday_ShouldRemoveAndReturnTrue()
    {
        // Arrange
        var holiday = new Holiday(DateTime.Parse("2024-12-25"), "Christmas");
        await _repository.AddAsync(holiday);

        // Act
        var result = await _repository.DeleteAsync(holiday.Id);

        // Assert
        result.Should().BeTrue();
        
        var deletedHoliday = await _repository.GetByIdAsync(holiday.Id);
        deletedHoliday.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingHoliday_ShouldReturnFalse()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _repository.DeleteAsync(nonExistingId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ExistsOnDateAsync Tests

    [Fact]
    public async Task ExistsOnDateAsync_WithExistingHoliday_ShouldReturnTrue()
    {
        // Arrange
        var holiday = new Holiday(DateTime.Parse("2024-12-25"), "Christmas");
        await _repository.AddAsync(holiday);

        // Act
        var result = await _repository.ExistsOnDateAsync(DateOnly.Parse("2024-12-25"));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsOnDateAsync_WithRecurringHoliday_ShouldReturnTrue()
    {
        // Arrange
        var holiday = new Holiday(DateTime.Parse("0001-12-25"), "Christmas Annual");
        await _repository.AddAsync(holiday);

        // Act
        var result = await _repository.ExistsOnDateAsync(DateOnly.Parse("2024-12-25"));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsOnDateAsync_WithNoHoliday_ShouldReturnFalse()
    {
        // Arrange
        var holiday = new Holiday(DateTime.Parse("2024-12-25"), "Christmas");
        await _repository.AddAsync(holiday);

        // Act
        var result = await _repository.ExistsOnDateAsync(DateOnly.Parse("2024-07-04"));

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task Repository_CompleteWorkflow_ShouldWorkCorrectly()
    {
        // Arrange & Act - Add
        var holiday = new Holiday(DateTime.Parse("2024-12-25"), "Christmas");
        await _repository.AddAsync(holiday);

        // Act - Verify exists
        var exists = await _repository.ExistsOnDateAsync(DateOnly.Parse("2024-12-25"));
        exists.Should().BeTrue();

        // Act - Get by date
        var byDate = await _repository.GetByDateAsync(DateOnly.Parse("2024-12-25"));
        byDate.Should().HaveCount(1);

        // Act - Update
        holiday.UpdateDescription("Christmas Day - Complete");
        await _repository.UpdateAsync(holiday);

        // Act - Verify update
        var updated = await _repository.GetByIdAsync(holiday.Id);
        updated!.Description.Should().Be("Christmas Day - Complete");

        // Act - Delete
        var deleted = await _repository.DeleteAsync(holiday.Id);
        deleted.Should().BeTrue();

        // Act - Verify deletion
        var afterDelete = await _repository.GetByIdAsync(holiday.Id);
        afterDelete.Should().BeNull();
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
