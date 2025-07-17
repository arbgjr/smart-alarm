using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.ValueObjects;
using SmartAlarm.Infrastructure.Data;
using SmartAlarm.Infrastructure.Repositories.EntityFramework;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests.Repositories
{
    /// <summary>
    /// Integration tests for Entity Framework repositories.
    /// Tests repository implementations against in-memory database.
    /// </summary>
    public class EfRepositoryTests : IDisposable
    {
        private readonly SmartAlarmDbContext _context;
        private readonly EfUserRepository _userRepository;
        private readonly EfAlarmRepository _alarmRepository;

        public EfRepositoryTests()
        {
            var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
            connection.Open();
            var options = new DbContextOptionsBuilder<SmartAlarmDbContext>()
                .UseSqlite(connection)
                .Options;

            _context = new SmartAlarmDbContext(options);
            _context.Database.EnsureCreated();
            _userRepository = new EfUserRepository(_context);
            
            // Create mock dependencies for alarm repository
            var logger = new Mock<ILogger<EfAlarmRepository>>();
            var meter = new Mock<SmartAlarmMeter>();
            var correlationContext = new Mock<ICorrelationContext>();
            var activitySource = new Mock<SmartAlarmActivitySource>();
            
            _alarmRepository = new EfAlarmRepository(_context, logger.Object, meter.Object, correlationContext.Object, activitySource.Object);
        }

        [Fact]
        public async Task EfUserRepository_Should_AddAndRetrieveUser()
        {
            // Arrange
            var user = new User(Guid.NewGuid(), "Test User", "test@example.com");

            // Act
            await _userRepository.AddAsync(user);
            await _context.SaveChangesAsync();

            var retrievedUser = await _userRepository.GetByIdAsync(user.Id);

            // Assert
            retrievedUser.Should().NotBeNull();
            retrievedUser.Id.Should().Be(user.Id);
            retrievedUser.Name.Value.Should().Be("Test User");
            retrievedUser.Email.Address.Should().Be("test@example.com");
        }

        [Fact]
        public async Task EfUserRepository_Should_FindUserByEmail()
        {
            // Arrange
            var user = new User(Guid.NewGuid(), "Email User", "email@example.com");
            await _userRepository.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var retrievedUser = await _userRepository.GetByEmailAsync("email@example.com");

            // Assert
            retrievedUser.Should().NotBeNull();
            retrievedUser.Email.Address.Should().Be("email@example.com");
        }

        [Fact]
        public async Task EfAlarmRepository_Should_AddAndRetrieveAlarm()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User(userId, "Alarm User", "alarm@example.com");
            await _userRepository.AddAsync(user);

            var alarm = new Alarm(Guid.NewGuid(), new Name("Test Alarm"), DateTime.UtcNow.AddHours(1), true, userId);

            // Act
            await _alarmRepository.AddAsync(alarm);
            await _context.SaveChangesAsync();

            var retrievedAlarm = await _alarmRepository.GetByIdAsync(alarm.Id);

            // Assert
            retrievedAlarm.Should().NotBeNull();
            retrievedAlarm.Id.Should().Be(alarm.Id);
            retrievedAlarm.Name.Value.Should().Be("Test Alarm");
            retrievedAlarm.UserId.Should().Be(userId);
        }

        [Fact]
        public async Task EfAlarmRepository_Should_GetAlarmsByUserId()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User(userId, "Multi Alarm User", "multi@example.com");
            await _userRepository.AddAsync(user);

            var alarm1 = new Alarm(Guid.NewGuid(), new Name("Alarm 1"), DateTime.UtcNow.AddHours(1), true, userId);
            var alarm2 = new Alarm(Guid.NewGuid(), new Name("Alarm 2"), DateTime.UtcNow.AddHours(2), true, userId);

            await _alarmRepository.AddAsync(alarm1);
            await _alarmRepository.AddAsync(alarm2);
            await _context.SaveChangesAsync();

            // Act
            var userAlarms = await _alarmRepository.GetByUserIdAsync(userId);

            // Assert
            userAlarms.Should().HaveCount(2);
            userAlarms.Should().Contain(a => a.Name.Value == "Alarm 1");
            userAlarms.Should().Contain(a => a.Name.Value == "Alarm 2");
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}