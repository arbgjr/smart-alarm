using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Infrastructure.Data;
using SmartAlarm.Infrastructure.Repositories;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests.Repositories
{
    public class EfExceptionPeriodRepositoryTests : IDisposable
    {
        private readonly SmartAlarmDbContext _context;
        private readonly EfExceptionPeriodRepository _repository;
        private readonly Mock<ILogger<EfExceptionPeriodRepository>> _loggerMock;
        private readonly Guid _testUserId = Guid.NewGuid();

        public EfExceptionPeriodRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<SmartAlarmDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new SmartAlarmDbContext(options);
            _loggerMock = new Mock<ILogger<EfExceptionPeriodRepository>>();
            _repository = new EfExceptionPeriodRepository(_context, _loggerMock.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_Should_Throw_ArgumentNullException_When_Context_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new EfExceptionPeriodRepository(null!, _loggerMock.Object));
        }

        [Fact]
        public void Constructor_Should_Throw_ArgumentNullException_When_Logger_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new EfExceptionPeriodRepository(_context, null!));
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_Should_Return_ExceptionPeriod_When_Found()
        {
            // Arrange
            var exceptionPeriod = CreateTestExceptionPeriod();
            await _context.ExceptionPeriods.AddAsync(exceptionPeriod);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(exceptionPeriod.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(exceptionPeriod.Id, result.Id);
            Assert.Equal(exceptionPeriod.Name, result.Name);
        }

        [Fact]
        public async Task GetByIdAsync_Should_Return_Null_When_Not_Found()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _repository.GetByIdAsync(nonExistentId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetByUserIdAsync Tests

        [Fact]
        public async Task GetByUserIdAsync_Should_Return_ExceptionPeriods_For_User()
        {
            // Arrange
            var period1 = CreateTestExceptionPeriod(startDate: DateTime.Today);
            var period2 = CreateTestExceptionPeriod(startDate: DateTime.Today.AddDays(10));
            var otherUserPeriod = CreateTestExceptionPeriod(userId: Guid.NewGuid());

            await _context.ExceptionPeriods.AddRangeAsync(period1, period2, otherUserPeriod);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByUserIdAsync(_testUserId);

            // Assert
            Assert.Equal(2, result.Count());
            Assert.All(result, ep => Assert.Equal(_testUserId, ep.UserId));
            // Should be ordered by StartDate
            Assert.True(result.First().StartDate <= result.Last().StartDate);
        }

        [Fact]
        public async Task GetByUserIdAsync_Should_Return_Empty_When_No_Periods_Found()
        {
            // Act
            var result = await _repository.GetByUserIdAsync(_testUserId);

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region GetActivePeriodsOnDateAsync Tests

        [Fact]
        public async Task GetActivePeriodsOnDateAsync_Should_Return_Active_Periods_On_Date()
        {
            // Arrange
            var testDate = DateTime.Today.AddDays(5);
            var activePeriod = CreateTestExceptionPeriod(
                startDate: DateTime.Today, 
                endDate: DateTime.Today.AddDays(10),
                isActive: true);
            var inactivePeriod = CreateTestExceptionPeriod(
                startDate: DateTime.Today, 
                endDate: DateTime.Today.AddDays(10),
                isActive: false);
            var nonOverlappingPeriod = CreateTestExceptionPeriod(
                startDate: DateTime.Today.AddDays(20), 
                endDate: DateTime.Today.AddDays(30));

            await _context.ExceptionPeriods.AddRangeAsync(activePeriod, inactivePeriod, nonOverlappingPeriod);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetActivePeriodsOnDateAsync(_testUserId, testDate);

            // Assert
            Assert.Single(result);
            Assert.Equal(activePeriod.Id, result.First().Id);
        }

        #endregion

        #region GetOverlappingPeriodsAsync Tests

        [Fact]
        public async Task GetOverlappingPeriodsAsync_Should_Return_Overlapping_Periods()
        {
            // Arrange
            var overlapping1 = CreateTestExceptionPeriod(
                startDate: DateTime.Today.AddDays(-5), 
                endDate: DateTime.Today.AddDays(5));
            var overlapping2 = CreateTestExceptionPeriod(
                startDate: DateTime.Today.AddDays(5), 
                endDate: DateTime.Today.AddDays(15));
            var nonOverlapping = CreateTestExceptionPeriod(
                startDate: DateTime.Today.AddDays(20), 
                endDate: DateTime.Today.AddDays(30));

            await _context.ExceptionPeriods.AddRangeAsync(overlapping1, overlapping2, nonOverlapping);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetOverlappingPeriodsAsync(
                _testUserId, DateTime.Today, DateTime.Today.AddDays(10));

            // Assert
            Assert.Equal(2, result.Count());
            Assert.Contains(result, ep => ep.Id == overlapping1.Id);
            Assert.Contains(result, ep => ep.Id == overlapping2.Id);
        }

        [Fact]
        public async Task GetOverlappingPeriodsAsync_Should_Exclude_Specified_Id()
        {
            // Arrange
            var period1 = CreateTestExceptionPeriod(
                startDate: DateTime.Today, 
                endDate: DateTime.Today.AddDays(10));
            var period2 = CreateTestExceptionPeriod(
                startDate: DateTime.Today.AddDays(5), 
                endDate: DateTime.Today.AddDays(15));

            await _context.ExceptionPeriods.AddRangeAsync(period1, period2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetOverlappingPeriodsAsync(
                _testUserId, DateTime.Today, DateTime.Today.AddDays(10), period1.Id);

            // Assert
            Assert.Single(result);
            Assert.Equal(period2.Id, result.First().Id);
        }

        #endregion

        #region GetByTypeAsync Tests

        [Fact]
        public async Task GetByTypeAsync_Should_Return_Periods_Of_Specified_Type()
        {
            // Arrange
            var vacationPeriod = CreateTestExceptionPeriod(type: ExceptionPeriodType.Vacation);
            var holidayPeriod = CreateTestExceptionPeriod(type: ExceptionPeriodType.Holiday);

            await _context.ExceptionPeriods.AddRangeAsync(vacationPeriod, holidayPeriod);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByTypeAsync(_testUserId, ExceptionPeriodType.Vacation);

            // Assert
            Assert.Single(result);
            Assert.Equal(vacationPeriod.Id, result.First().Id);
        }

        #endregion

        #region CountByUserIdAsync Tests

        [Fact]
        public async Task CountByUserIdAsync_Should_Return_Correct_Count()
        {
            // Arrange
            var period1 = CreateTestExceptionPeriod();
            var period2 = CreateTestExceptionPeriod();
            var otherUserPeriod = CreateTestExceptionPeriod(userId: Guid.NewGuid());

            await _context.ExceptionPeriods.AddRangeAsync(period1, period2, otherUserPeriod);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.CountByUserIdAsync(_testUserId);

            // Assert
            Assert.Equal(2, result);
        }

        #endregion

        #region HasActivePeriodOnDateAsync Tests

        [Fact]
        public async Task HasActivePeriodOnDateAsync_Should_Return_True_When_Active_Period_Exists()
        {
            // Arrange
            var testDate = DateTime.Today.AddDays(5);
            var activePeriod = CreateTestExceptionPeriod(
                startDate: DateTime.Today, 
                endDate: DateTime.Today.AddDays(10),
                isActive: true);

            await _context.ExceptionPeriods.AddAsync(activePeriod);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.HasActivePeriodOnDateAsync(_testUserId, testDate);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task HasActivePeriodOnDateAsync_Should_Return_False_When_No_Active_Period_Exists()
        {
            // Arrange
            var testDate = DateTime.Today.AddDays(5);
            var inactivePeriod = CreateTestExceptionPeriod(
                startDate: DateTime.Today, 
                endDate: DateTime.Today.AddDays(10),
                isActive: false);

            await _context.ExceptionPeriods.AddAsync(inactivePeriod);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.HasActivePeriodOnDateAsync(_testUserId, testDate);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region AddAsync Tests

        [Fact]
        public async Task AddAsync_Should_Add_ExceptionPeriod_Successfully()
        {
            // Arrange
            var exceptionPeriod = CreateTestExceptionPeriod();

            // Act
            await _repository.AddAsync(exceptionPeriod);

            // Assert
            var savedPeriod = await _context.ExceptionPeriods.FindAsync(exceptionPeriod.Id);
            Assert.NotNull(savedPeriod);
            Assert.Equal(exceptionPeriod.Name, savedPeriod.Name);
        }

        [Fact]
        public async Task AddAsync_Should_Throw_ArgumentNullException_When_ExceptionPeriod_Is_Null()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _repository.AddAsync(null!));
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_Should_Update_ExceptionPeriod_Successfully()
        {
            // Arrange
            var exceptionPeriod = CreateTestExceptionPeriod();
            await _context.ExceptionPeriods.AddAsync(exceptionPeriod);
            await _context.SaveChangesAsync();

            // Detach to simulate a new context
            _context.Entry(exceptionPeriod).State = EntityState.Detached;

            // Modify the period
            exceptionPeriod.UpdateName("Updated Name");

            // Act
            await _repository.UpdateAsync(exceptionPeriod);

            // Assert
            var updatedPeriod = await _context.ExceptionPeriods.FindAsync(exceptionPeriod.Id);
            Assert.NotNull(updatedPeriod);
            Assert.Equal("Updated Name", updatedPeriod.Name);
        }

        [Fact]
        public async Task UpdateAsync_Should_Throw_ArgumentNullException_When_ExceptionPeriod_Is_Null()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _repository.UpdateAsync(null!));
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_Should_Delete_ExceptionPeriod_Successfully()
        {
            // Arrange
            var exceptionPeriod = CreateTestExceptionPeriod();
            await _context.ExceptionPeriods.AddAsync(exceptionPeriod);
            await _context.SaveChangesAsync();

            // Act
            await _repository.DeleteAsync(exceptionPeriod.Id);

            // Assert
            var deletedPeriod = await _context.ExceptionPeriods.FindAsync(exceptionPeriod.Id);
            Assert.Null(deletedPeriod);
        }

        [Fact]
        public async Task DeleteAsync_Should_Not_Throw_When_ExceptionPeriod_Not_Found()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act & Assert
            await _repository.DeleteAsync(nonExistentId); // Should not throw
        }

        #endregion

        #region Helper Methods

        private ExceptionPeriod CreateTestExceptionPeriod(
            Guid? userId = null, 
            DateTime? startDate = null, 
            DateTime? endDate = null,
            ExceptionPeriodType type = ExceptionPeriodType.Vacation,
            bool isActive = true)
        {
            var start = startDate ?? DateTime.Today;
            var end = endDate ?? start.AddDays(7);
            
            var period = new ExceptionPeriod(
                Guid.NewGuid(),
                "Test Period",
                start,
                end,
                type,
                userId ?? _testUserId,
                "Test description");

            if (!isActive)
            {
                period.Deactivate();
            }

            return period;
        }

        #endregion

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
