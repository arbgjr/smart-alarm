using SmartAlarm.Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Domain.Services;
using Xunit;

namespace SmartAlarm.Domain.Tests
{
    public class AlarmDomainServiceTests
    {
        private readonly Mock<IAlarmRepository> _alarmRepositoryMock;
        private readonly Mock<ILogger<AlarmDomainService>> _loggerMock;
        private readonly AlarmDomainService _service;

        public AlarmDomainServiceTests()
        {
            _alarmRepositoryMock = new Mock<IAlarmRepository>();
            _loggerMock = new Mock<ILogger<AlarmDomainService>>();
            _service = new AlarmDomainService(_alarmRepositoryMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task CanUserCreateAlarmAsync_Should_Return_True_When_Less_Than_10_Alarms()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _alarmRepositoryMock.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(new List<Alarm> { new Alarm(Guid.NewGuid(), "A", DateTime.Now, true, userId) });

            // Act
            var result = await _service.CanUserCreateAlarmAsync(userId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CanUserCreateAlarmAsync_Should_Return_False_When_10_Alarms()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var alarms = Enumerable.Range(0, 10).Select(i => new Alarm(Guid.NewGuid(), $"A{i}", DateTime.Now, true, userId)).ToList();
            _alarmRepositoryMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(alarms);

            // Act
            var result = await _service.CanUserCreateAlarmAsync(userId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CanTriggerAlarmAsync_Should_Return_False_When_Alarm_Not_Found()
        {
            // Arrange
            var alarmId = Guid.NewGuid();
            _alarmRepositoryMock.Setup(r => r.GetByIdAsync(alarmId, It.IsAny<CancellationToken>())).ReturnsAsync((Alarm)null);

            // Act
            var result = await _service.CanTriggerAlarmAsync(alarmId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CanTriggerAlarmAsync_Should_Return_True_When_Alarm_Enabled()
        {
            // Arrange
            var alarmId = Guid.NewGuid();
            var alarm = new Alarm(alarmId, "Test Alarm", DateTime.Now, true, Guid.NewGuid());
            _alarmRepositoryMock.Setup(r => r.GetByIdAsync(alarmId, It.IsAny<CancellationToken>())).ReturnsAsync(alarm);

            // Act
            var result = await _service.CanTriggerAlarmAsync(alarmId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidAlarmTime_Should_Return_False_For_Past()
        {
            // Arrange
            var past = DateTime.UtcNow.AddMinutes(-1);

            // Act
            var result = _service.IsValidAlarmTime(past);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidAlarmTime_Should_Return_True_For_Future()
        {
            // Arrange
            var future = DateTime.UtcNow.AddMinutes(10);

            // Act
            var result = _service.IsValidAlarmTime(future);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task GetAlarmsDueForTriggeringAsync_Should_Use_Optimized_Repository_Method_When_Available()
        {
            // Arrange
            var now = DateTime.Now;
            var expectedAlarms = new List<Alarm>
            {
                new Alarm(Guid.NewGuid(), "Test Alarm 1", now, true, Guid.NewGuid()),
                new Alarm(Guid.NewGuid(), "Test Alarm 2", now, true, Guid.NewGuid())
            };

            _alarmRepositoryMock.Setup(r => r.GetDueForTriggeringAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(expectedAlarms);

            // Act
            var result = await _service.GetAlarmsDueForTriggeringAsync();

            // Assert
            Assert.Equal(2, result.Count());
            _alarmRepositoryMock.Verify(r => r.GetDueForTriggeringAsync(It.IsAny<DateTime>()), Times.Once);
            _alarmRepositoryMock.Verify(r => r.GetAllEnabledAsync(), Times.Never);
        }

        [Fact]
        public async Task GetAlarmsDueForTriggeringAsync_Should_Fallback_To_GetAllEnabled_When_Optimized_Returns_Empty()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var alarmDue = new Alarm(Guid.NewGuid(), "Test Alarm", DateTime.Now, true, userId);
            var alarmNotDue = new Alarm(Guid.NewGuid(), "Test Alarm 2", DateTime.Now, true, userId);

            var allEnabledAlarms = new List<Alarm> { alarmDue, alarmNotDue };

            _alarmRepositoryMock.Setup(r => r.GetDueForTriggeringAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Alarm>());

            _alarmRepositoryMock.Setup(r => r.GetAllEnabledAsync())
                .ReturnsAsync(allEnabledAlarms);

            // Act
            var result = await _service.GetAlarmsDueForTriggeringAsync();

            // Assert
            Assert.NotNull(result);
            _alarmRepositoryMock.Verify(r => r.GetDueForTriggeringAsync(It.IsAny<DateTime>()), Times.Once);
            _alarmRepositoryMock.Verify(r => r.GetAllEnabledAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAlarmsDueForTriggeringAsync_Should_Handle_Repository_Calls_Gracefully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var alarm1 = new Alarm(Guid.NewGuid(), "Alarm 1", DateTime.Now, true, userId);
            var alarm2 = new Alarm(Guid.NewGuid(), "Alarm 2", DateTime.Now, true, userId);

            var allEnabledAlarms = new List<Alarm> { alarm1, alarm2 };

            _alarmRepositoryMock.Setup(r => r.GetDueForTriggeringAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Alarm>());

            _alarmRepositoryMock.Setup(r => r.GetAllEnabledAsync())
                .ReturnsAsync(allEnabledAlarms);

            // Act
            var result = await _service.GetAlarmsDueForTriggeringAsync();

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetAlarmsDueForTriggeringAsync_Should_Throw_When_Repository_Throws()
        {
            // Arrange
            _alarmRepositoryMock.Setup(r => r.GetDueForTriggeringAsync(It.IsAny<DateTime>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.GetAlarmsDueForTriggeringAsync());
        }


    }
}
