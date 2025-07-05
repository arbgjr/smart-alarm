using System;
using System.Collections.Generic;
using System.Linq;
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
            _alarmRepositoryMock.Setup(r => r.GetByIdAsync(alarmId)).ReturnsAsync((Alarm)null);

            // Act
            var result = await _service.CanTriggerAlarmAsync(alarmId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CanTriggerAlarmAsync_Should_Return_True_When_Alarm_Enabled_And_ShouldTriggerNow()
        {
            // Arrange
            var alarmId = Guid.NewGuid();
            var alarm = new Alarm(alarmId, "A", DateTime.Now, true, Guid.NewGuid());
            // Forçar o método ShouldTriggerNow a retornar true via herança ou helper, mas aqui vamos criar uma subclasse para teste
            var alarmTest = new AlarmTest(alarm);
            _alarmRepositoryMock.Setup(r => r.GetByIdAsync(alarmId)).ReturnsAsync(alarmTest);

            // Act
            var result = await _service.CanTriggerAlarmAsync(alarmId);

            // Assert
            Assert.True(result);

        }

        // Helper para sobrescrever ShouldTriggerNow
        private class AlarmTest : Alarm
        {
            public AlarmTest(Alarm baseAlarm)
                : base(baseAlarm.Id, baseAlarm.Name, baseAlarm.Time, baseAlarm.Enabled, baseAlarm.UserId) { }
            public override bool ShouldTriggerNow() => true;
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
    }
}
