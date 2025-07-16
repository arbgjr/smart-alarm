using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using SmartAlarm.Application.Handlers;
using SmartAlarm.Application.DTOs;
using SmartAlarm.Application.Queries;
using SmartAlarm.Application.Commands;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace SmartAlarm.Application.Tests.Handlers
{
    public class AlarmHandlerIntegrationTests
    {
        [Fact]
        public async Task GetAlarmById_Should_Respect_ExceptionPeriod()
        {
            // Arrange
            var alarmId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var alarm = new TestAlarm(alarmId, "Alarme Teste", DateTime.Now, true, userId)
            {
                ExceptionPeriodActive = true
            };
            var repoMock = new Mock<IAlarmRepository>();
            repoMock.Setup(r => r.GetByIdAsync(alarmId)).ReturnsAsync(alarm);
            var loggerMock = new Mock<ILogger<GetAlarmByIdHandler>>();
            var handler = new GetAlarmByIdHandler(repoMock.Object, loggerMock.Object);
            var query = new GetAlarmByIdQuery(alarmId);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.CanTriggerNow); // Deve respeitar ExceptionPeriod
        }

        [Fact]
        public async Task GetAlarmById_Should_Respect_HolidayPreference_Disable()
        {
            // Arrange
            var alarmId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var alarm = new TestAlarm(alarmId, "Alarme Teste", DateTime.Now, true, userId)
            {
                HolidayActive = true,
                HolidayPreference = new UserHolidayPreference(Guid.NewGuid(), userId, Guid.NewGuid(), true, HolidayPreferenceAction.Disable)
            };
            var repoMock = new Mock<IAlarmRepository>();
            repoMock.Setup(r => r.GetByIdAsync(alarmId)).ReturnsAsync(alarm);
            var loggerMock = new Mock<ILogger<GetAlarmByIdHandler>>();
            var handler = new GetAlarmByIdHandler(repoMock.Object, loggerMock.Object);
            var query = new GetAlarmByIdQuery(alarmId);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.CanTriggerNow); // Deve respeitar HolidayPreference Disable
        }

        [Fact]
        public async Task ListAlarms_Should_Respect_All_Exceptions()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var alarm1 = new TestAlarm(Guid.NewGuid(), "Alarme 1", DateTime.Now, true, userId)
            {
                ExceptionPeriodActive = true
            };
            var alarm2 = new TestAlarm(Guid.NewGuid(), "Alarme 2", DateTime.Now, true, userId)
            {
                HolidayActive = true,
                HolidayPreference = new UserHolidayPreference(Guid.NewGuid(), userId, Guid.NewGuid(), true, HolidayPreferenceAction.Disable)
            };
            var repoMock = new Mock<IAlarmRepository>();
            repoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Alarm> { alarm1, alarm2 });
            var loggerMock = new Mock<ILogger<ListAlarmsHandler>>();
            var handler = new ListAlarmsHandler(repoMock.Object, loggerMock.Object);
            var query = new ListAlarmsQuery(userId);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.All(result, dto => Assert.False(dto.CanTriggerNow));
        }

        // Helper para simular Alarm com lógica customizada
        private class TestAlarm : Alarm
        {
            public bool ExceptionPeriodActive { get; set; }
            public bool HolidayActive { get; set; }
            public UserHolidayPreference? HolidayPreference { get; set; }

            public TestAlarm(Guid id, string name, DateTime time, bool enabled, Guid userId)
                : base(id, name, time, enabled, userId) { }

            protected override bool ExceptionPeriodIsActiveForUser(Guid userId, DateTime date)
                => ExceptionPeriodActive;

            protected override Holiday? GetHolidayForDate(DateTime date)
                => HolidayActive ? new Holiday(date, "Feriado Teste") : null;

            protected override UserHolidayPreference? GetUserHolidayPreference(Guid userId, Guid holidayId)
                => HolidayPreference;
        }
    }
}
