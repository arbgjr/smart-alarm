using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using SmartAlarm.Application.Handlers.Routine;
using SmartAlarm.Application.Queries.Routine;
using SmartAlarm.Application.DTOs.Routine;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.ValueObjects;
using SmartAlarm.Observability.Metrics;

namespace SmartAlarm.Application.Tests.Handlers.Routine
{
    public class ListRoutinesHandlerTests : IDisposable
    {
        private readonly Mock<IRoutineRepository> _mockRoutineRepository;
        private readonly Mock<IAlarmRepository> _mockAlarmRepository;
        private readonly ActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly BusinessMetrics _businessMetrics;
        private readonly Mock<ILogger<ListRoutinesHandler>> _mockLogger;
        private readonly ListRoutinesHandler _handler;

        public ListRoutinesHandlerTests()
        {
            _mockRoutineRepository = new Mock<IRoutineRepository>();
            _mockAlarmRepository = new Mock<IAlarmRepository>();
            _activitySource = new ActivitySource("SmartAlarm.Test");
            _meter = new SmartAlarmMeter();
            _businessMetrics = new BusinessMetrics();
            _mockLogger = new Mock<ILogger<ListRoutinesHandler>>();

            _handler = new ListRoutinesHandler(
                _mockRoutineRepository.Object,
                _mockAlarmRepository.Object,
                _activitySource,
                _meter,
                _businessMetrics,
                _mockLogger.Object);
        }

        public void Dispose()
        {
            _activitySource?.Dispose();
            _meter?.Dispose();
        }

        [Fact]
        public async Task Handle_WithAlarmId_ShouldReturnRoutinesForSpecificAlarm()
        {
            // Arrange
            var alarmId = Guid.NewGuid();
            var query = new ListRoutinesQuery { AlarmId = alarmId };

            var routines = new List<Domain.Entities.Routine>
            {
                CreateTestRoutine(alarmId),
                CreateTestRoutine(alarmId)
            };

            _mockRoutineRepository.Setup(x => x.GetByAlarmIdAsync(alarmId))
                .ReturnsAsync(routines);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            _mockRoutineRepository.Verify(x => x.GetByAlarmIdAsync(alarmId), Times.Once);
            _mockRoutineRepository.Verify(x => x.GetAllAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_WithoutAlarmId_ShouldReturnAllRoutines()
        {
            // Arrange
            var query = new ListRoutinesQuery();

            var routines = new List<Domain.Entities.Routine>
            {
                CreateTestRoutine(Guid.NewGuid()),
                CreateTestRoutine(Guid.NewGuid()),
                CreateTestRoutine(Guid.NewGuid())
            };

            _mockRoutineRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(routines);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            _mockRoutineRepository.Verify(x => x.GetAllAsync(), Times.Once);
            _mockRoutineRepository.Verify(x => x.GetByAlarmIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WithUserId_ShouldFilterByUserAlarms()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userAlarmId1 = Guid.NewGuid();
            var userAlarmId2 = Guid.NewGuid();
            var otherAlarmId = Guid.NewGuid();

            var query = new ListRoutinesQuery { UserId = userId };

            var userAlarms = new List<Alarm>
            {
                CreateTestAlarm(userAlarmId1, userId),
                CreateTestAlarm(userAlarmId2, userId)
            };

            var allRoutines = new List<Domain.Entities.Routine>
            {
                CreateTestRoutine(userAlarmId1),
                CreateTestRoutine(userAlarmId2),
                CreateTestRoutine(otherAlarmId) // Esta não deve aparecer no resultado
            };

            _mockAlarmRepository.Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(userAlarms);
            _mockRoutineRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(allRoutines);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.True(
                r.AlarmId == userAlarmId1 || r.AlarmId == userAlarmId2));
        }

        [Fact]
        public async Task Handle_WithAlarmIdAndUserId_ShouldFilterCorrectly()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userAlarmId = Guid.NewGuid();
            var query = new ListRoutinesQuery { AlarmId = userAlarmId, UserId = userId };

            var userAlarms = new List<Alarm>
            {
                CreateTestAlarm(userAlarmId, userId)
            };

            var alarmRoutines = new List<Domain.Entities.Routine>
            {
                CreateTestRoutine(userAlarmId),
                CreateTestRoutine(userAlarmId)
            };

            _mockAlarmRepository.Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(userAlarms);
            _mockRoutineRepository.Setup(x => x.GetByAlarmIdAsync(userAlarmId))
                .ReturnsAsync(alarmRoutines);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.Equal(userAlarmId, r.AlarmId));
        }

        [Fact]
        public async Task Handle_NoRoutinesFound_ShouldReturnEmptyList()
        {
            // Arrange
            var query = new ListRoutinesQuery();

            _mockRoutineRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<Domain.Entities.Routine>());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        private static Domain.Entities.Routine CreateTestRoutine(Guid alarmId)
        {
            return new Domain.Entities.Routine(
                Guid.NewGuid(),
                new Name("Test Routine"),
                alarmId,
                new List<string> { "action1", "action2" }
            );
        }

        private static Alarm CreateTestAlarm(Guid alarmId, Guid userId)
        {
            var alarm = new Alarm(
                alarmId,
                new Name("Test Alarm"),
                DateTime.Now,
                true,
                userId
            );

            return alarm;
        }
    }
}
