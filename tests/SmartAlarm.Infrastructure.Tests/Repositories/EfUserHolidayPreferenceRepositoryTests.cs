using SmartAlarm.Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Infrastructure.Data;
using SmartAlarm.Infrastructure.Repositories;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests.Repositories
{
    /// <summary>
    /// Integration tests for EfUserHolidayPreferenceRepository.
    /// Tests database operations, joins, and referential integrity.
    /// </summary>
    public class EfUserHolidayPreferenceRepositoryTests : IDisposable
    {
        private readonly SmartAlarmDbContext _context;
        private readonly EfUserHolidayPreferenceRepository _repository;
        private readonly ILogger<EfUserHolidayPreferenceRepository> _logger;

        private readonly User _testUser;
        private readonly Holiday _testHoliday;
        private readonly Holiday _testRecurringHoliday;

        public EfUserHolidayPreferenceRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<SmartAlarmDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new SmartAlarmDbContext(options);
            _logger = new LoggerFactory().CreateLogger<EfUserHolidayPreferenceRepository>();
            
            // Create observability mocks
            var meterMock = new Mock<SmartAlarmMeter>();
            var correlationContextMock = new Mock<ICorrelationContext>();
            var activitySourceMock = new Mock<SmartAlarmActivitySource>();
            
            _repository = new EfUserHolidayPreferenceRepository(_context, _logger, 
                meterMock.Object, correlationContextMock.Object, activitySourceMock.Object);

            // Setup test data
            _testUser = new User(Guid.NewGuid(), "Test User", "test@example.com");
            _testHoliday = new Holiday(DateTime.Today.AddDays(10), "Test Holiday");
            _testRecurringHoliday = new Holiday(new DateTime(1, 12, 25), "Christmas (Recurring)");

            _context.Users.Add(_testUser);
            _context.Holidays.AddRange(_testHoliday, _testRecurringHoliday);
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        #region AddAsync Tests

        [Fact]
        public async Task AddAsync_ShouldAddPreferenceSuccessfully()
        {
            // Arrange
            var preference = new UserHolidayPreference(
                Guid.NewGuid(),
                _testUser.Id,
                _testHoliday.Id,
                true,
                HolidayPreferenceAction.Disable);

            // Act
            await _repository.AddAsync(preference);

            // Assert
            var saved = await _context.UserHolidayPreferences.FindAsync(preference.Id);
            Assert.NotNull(saved);
            Assert.Equal(_testUser.Id, saved.UserId);
            Assert.Equal(_testHoliday.Id, saved.HolidayId);
            Assert.True(saved.IsEnabled);
            Assert.Equal(HolidayPreferenceAction.Disable, saved.Action);
        }

        [Fact]
        public async Task AddAsync_WithDelayAction_ShouldAddWithDelayInMinutes()
        {
            // Arrange
            var preference = new UserHolidayPreference(
                Guid.NewGuid(),
                _testUser.Id,
                _testHoliday.Id,
                true,
                HolidayPreferenceAction.Delay,
                60);

            // Act
            await _repository.AddAsync(preference);

            // Assert
            var saved = await _context.UserHolidayPreferences.FindAsync(preference.Id);
            Assert.NotNull(saved);
            Assert.Equal(HolidayPreferenceAction.Delay, saved.Action);
            Assert.Equal(60, saved.DelayInMinutes);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_ShouldReturnPreferenceWithNavigationProperties()
        {
            // Arrange
            var preference = new UserHolidayPreference(
                Guid.NewGuid(),
                _testUser.Id,
                _testHoliday.Id,
                true,
                HolidayPreferenceAction.Skip);

            await _repository.AddAsync(preference);

            // Act
            var result = await _repository.GetByIdAsync(preference.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(preference.Id, result.Id);
            Assert.NotNull(result.User);
            Assert.Equal(_testUser.Id, result.User.Id);
            Assert.NotNull(result.Holiday);
            Assert.Equal(_testHoliday.Id, result.Holiday.Id);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Act
            var result = await _repository.GetByIdAsync(Guid.NewGuid());

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetByUserAndHolidayAsync Tests

        [Fact]
        public async Task GetByUserAndHolidayAsync_ShouldReturnCorrectPreference()
        {
            // Arrange
            var preference = new UserHolidayPreference(
                Guid.NewGuid(),
                _testUser.Id,
                _testHoliday.Id,
                true,
                HolidayPreferenceAction.Delay,
                120);

            await _repository.AddAsync(preference);

            // Act
            var result = await _repository.GetByUserAndHolidayAsync(_testUser.Id, _testHoliday.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(preference.Id, result.Id);
            Assert.Equal(_testUser.Id, result.UserId);
            Assert.Equal(_testHoliday.Id, result.HolidayId);
        }

        [Fact]
        public async Task GetByUserAndHolidayAsync_WithNonExistentCombination_ShouldReturnNull()
        {
            // Act
            var result = await _repository.GetByUserAndHolidayAsync(Guid.NewGuid(), Guid.NewGuid());

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetByUserIdAsync Tests

        [Fact]
        public async Task GetByUserIdAsync_ShouldReturnAllUserPreferences()
        {
            // Arrange
            var preference1 = new UserHolidayPreference(
                Guid.NewGuid(), _testUser.Id, _testHoliday.Id, true, HolidayPreferenceAction.Disable);
            var preference2 = new UserHolidayPreference(
                Guid.NewGuid(), _testUser.Id, _testRecurringHoliday.Id, false, HolidayPreferenceAction.Skip);

            await _repository.AddAsync(preference1);
            await _repository.AddAsync(preference2);

            // Act
            var results = await _repository.GetByUserIdAsync(_testUser.Id);

            // Assert
            var resultsList = results.ToList();
            Assert.Equal(2, resultsList.Count);
            Assert.All(resultsList, p => Assert.Equal(_testUser.Id, p.UserId));
            Assert.All(resultsList, p => Assert.NotNull(p.Holiday));
        }

        [Fact]
        public async Task GetByUserIdAsync_WithNoPreferences_ShouldReturnEmpty()
        {
            // Act
            var results = await _repository.GetByUserIdAsync(_testUser.Id);

            // Assert
            Assert.Empty(results);
        }

        #endregion

        #region GetActiveByUserIdAsync Tests

        [Fact]
        public async Task GetActiveByUserIdAsync_ShouldReturnOnlyActivePreferences()
        {
            // Arrange
            var activePreference = new UserHolidayPreference(
                Guid.NewGuid(), _testUser.Id, _testHoliday.Id, true, HolidayPreferenceAction.Disable);
            var inactivePreference = new UserHolidayPreference(
                Guid.NewGuid(), _testUser.Id, _testRecurringHoliday.Id, false, HolidayPreferenceAction.Skip);

            await _repository.AddAsync(activePreference);
            await _repository.AddAsync(inactivePreference);

            // Act
            var results = await _repository.GetActiveByUserIdAsync(_testUser.Id);

            // Assert
            var resultsList = results.ToList();
            Assert.Single(resultsList);
            Assert.Equal(activePreference.Id, resultsList.First().Id);
            Assert.True(resultsList.First().IsEnabled);
        }

        #endregion

        #region GetApplicableForDateAsync Tests

        [Fact]
        public async Task GetApplicableForDateAsync_ShouldReturnApplicablePreferences()
        {
            // Arrange
            var targetDate = DateTime.Today.AddDays(10);
            var preference = new UserHolidayPreference(
                Guid.NewGuid(), _testUser.Id, _testHoliday.Id, true, HolidayPreferenceAction.Delay, 30);

            await _repository.AddAsync(preference);

            // Act
            var results = await _repository.GetApplicableForDateAsync(_testUser.Id, targetDate);

            // Assert
            var resultsList = results.ToList();
            Assert.Single(resultsList);
            Assert.Equal(preference.Id, resultsList.First().Id);
        }

        [Fact]
        public async Task GetApplicableForDateAsync_WithRecurringHoliday_ShouldReturnMatch()
        {
            // Arrange
            var christmasThisYear = new DateTime(DateTime.Today.Year, 12, 25);
            var preference = new UserHolidayPreference(
                Guid.NewGuid(), _testUser.Id, _testRecurringHoliday.Id, true, HolidayPreferenceAction.Skip);

            await _repository.AddAsync(preference);

            // Act
            var results = await _repository.GetApplicableForDateAsync(_testUser.Id, christmasThisYear);

            // Assert
            var resultsList = results.ToList();
            Assert.Single(resultsList);
            Assert.Equal(preference.Id, resultsList.First().Id);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_ShouldUpdatePreferenceSuccessfully()
        {
            // Arrange
            var preference = new UserHolidayPreference(
                Guid.NewGuid(), _testUser.Id, _testHoliday.Id, true, HolidayPreferenceAction.Disable);

            await _repository.AddAsync(preference);

            // Act
            preference.UpdateAction(HolidayPreferenceAction.Delay, 90);
            await _repository.UpdateAsync(preference);

            // Assert
            var updated = await _context.UserHolidayPreferences.FindAsync(preference.Id);
            Assert.NotNull(updated);
            Assert.Equal(HolidayPreferenceAction.Delay, updated.Action);
            Assert.Equal(90, updated.DelayInMinutes);
            Assert.NotNull(updated.UpdatedAt);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_ShouldRemovePreferenceFromDatabase()
        {
            // Arrange
            var preference = new UserHolidayPreference(
                Guid.NewGuid(), _testUser.Id, _testHoliday.Id, true, HolidayPreferenceAction.Skip);

            await _repository.AddAsync(preference);

            // Act
            await _repository.DeleteAsync(preference);

            // Assert
            var deleted = await _context.UserHolidayPreferences.FindAsync(preference.Id);
            Assert.Null(deleted);
        }

        #endregion

        #region ExistsAsync Tests

        [Fact]
        public async Task ExistsAsync_WithExistingPreference_ShouldReturnTrue()
        {
            // Arrange
            var preference = new UserHolidayPreference(
                Guid.NewGuid(), _testUser.Id, _testHoliday.Id, true, HolidayPreferenceAction.Disable);

            await _repository.AddAsync(preference);

            // Act
            var exists = await _repository.ExistsAsync(_testUser.Id, _testHoliday.Id);

            // Assert
            Assert.True(exists);
        }

        [Fact]
        public async Task ExistsAsync_WithNonExistentPreference_ShouldReturnFalse()
        {
            // Act
            var exists = await _repository.ExistsAsync(_testUser.Id, _testHoliday.Id);

            // Assert
            Assert.False(exists);
        }

        #endregion

        #region CountActiveByUserIdAsync Tests

        [Fact]
        public async Task CountActiveByUserIdAsync_ShouldReturnCorrectCount()
        {
            // Arrange
            var activePreference1 = new UserHolidayPreference(
                Guid.NewGuid(), _testUser.Id, _testHoliday.Id, true, HolidayPreferenceAction.Disable);
            var activePreference2 = new UserHolidayPreference(
                Guid.NewGuid(), _testUser.Id, _testRecurringHoliday.Id, true, HolidayPreferenceAction.Skip);
            var inactivePreference = new UserHolidayPreference(
                Guid.NewGuid(), _testUser.Id, _testRecurringHoliday.Id, false, HolidayPreferenceAction.Delay, 60);

            await _repository.AddAsync(activePreference1);
            await _repository.AddAsync(activePreference2);
            await _repository.AddAsync(inactivePreference);

            // Act
            var count = await _repository.CountActiveByUserIdAsync(_testUser.Id);

            // Assert
            Assert.Equal(2, count);
        }

        [Fact]
        public async Task CountActiveByUserIdAsync_WithNoActivePreferences_ShouldReturnZero()
        {
            // Act
            var count = await _repository.CountActiveByUserIdAsync(_testUser.Id);

            // Assert
            Assert.Equal(0, count);
        }

        #endregion

        #region Referential Integrity Tests

        [Fact]
        public async Task CascadeDelete_WhenUserDeleted_ShouldRemovePreferences()
        {
            // Arrange
            var preference = new UserHolidayPreference(
                Guid.NewGuid(), _testUser.Id, _testHoliday.Id, true, HolidayPreferenceAction.Disable);

            await _repository.AddAsync(preference);

            // Act
            _context.Users.Remove(_testUser);
            await _context.SaveChangesAsync();

            // Assert
            var preferences = await _context.UserHolidayPreferences
                .Where(p => p.UserId == _testUser.Id)
                .ToListAsync();
            Assert.Empty(preferences);
        }

        [Fact]
        public async Task CascadeDelete_WhenHolidayDeleted_ShouldRemovePreferences()
        {
            // Arrange
            var preference = new UserHolidayPreference(
                Guid.NewGuid(), _testUser.Id, _testHoliday.Id, true, HolidayPreferenceAction.Skip);

            await _repository.AddAsync(preference);

            // Act
            _context.Holidays.Remove(_testHoliday);
            await _context.SaveChangesAsync();

            // Assert
            var preferences = await _context.UserHolidayPreferences
                .Where(p => p.HolidayId == _testHoliday.Id)
                .ToListAsync();
            Assert.Empty(preferences);
        }

        #endregion

        #region Complex Join Queries Tests

        [Fact]
        public async Task GetAllAsync_ShouldIncludeNavigationProperties()
        {
            // Arrange
            var preference = new UserHolidayPreference(
                Guid.NewGuid(), _testUser.Id, _testHoliday.Id, true, HolidayPreferenceAction.Delay, 45);

            await _repository.AddAsync(preference);

            // Act
            var results = await _repository.GetAllAsync();

            // Assert
            var resultsList = results.ToList();
            Assert.Single(resultsList);
            var result = resultsList.First();
            
            Assert.NotNull(result.User);
            Assert.Equal(_testUser.Name.Value, result.User.Name.Value);
            Assert.NotNull(result.Holiday);
            Assert.Equal(_testHoliday.Description, result.Holiday.Description);
        }

        #endregion
    }
}
