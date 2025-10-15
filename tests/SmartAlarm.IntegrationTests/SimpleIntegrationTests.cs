using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Domain.Services;
using SmartAlarm.Domain.ValueObjects;
using Xunit;

namespace SmartAlarm.IntegrationTests
{
    public class SimpleIntegrationTests
    {
        [Fact]
        public async Task DomainService_Integration_Should_Work_With_Repository()
        {
            // Arrange
            var mockRepository = new Mock<IAlarmRepository>();
            var mockLogger = new Mock<ILogger<AlarmDomainService>>();
            var service = new AlarmDomainService(mockRepository.Object, mockLogger.Object);

            var userId = Guid.NewGuid();
            var existingAlarms = new List<Alarm>
            {
                new Alarm(Guid.NewGuid(), "Alarm 1", DateTime.Now.AddHours(1), true, userId),
                new Alarm(Guid.NewGuid(), "Alarm 2", DateTime.Now.AddHours(2), true, userId)
            };

            mockRepository.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(existingAlarms);

            // Act
            var canCreate = await service.CanUserCreateAlarmAsync(userId);

            // Assert
            Assert.True(canCreate); // Should be true since user has less than 10 alarms
            mockRepository.Verify(r => r.GetByUserIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task DomainService_Should_Prevent_Creating_Too_Many_Alarms()
        {
            // Arrange
            var mockRepository = new Mock<IAlarmRepository>();
            var mockLogger = new Mock<ILogger<AlarmDomainService>>();
            var service = new AlarmDomainService(mockRepository.Object, mockLogger.Object);

            var userId = Guid.NewGuid();
            var existingAlarms = new List<Alarm>();

            // Create 10 alarms (the limit)
            for (int i = 0; i < 10; i++)
            {
                existingAlarms.Add(new Alarm(Guid.NewGuid(), $"Alarm {i}", DateTime.Now.AddHours(i), true, userId));
            }

            mockRepository.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(existingAlarms);

            // Act
            var canCreate = await service.CanUserCreateAlarmAsync(userId);

            // Assert
            Assert.False(canCreate); // Should be false since user has reached the limit
        }

        [Fact]
        public void ValueObjects_Should_Work_Together()
        {
            // Arrange & Act
            var name = new Name("Test User");
            var email = new Email("test@example.com");
            var user = new User(Guid.NewGuid(), name, email);

            // Assert
            Assert.Equal(name, user.Name);
            Assert.Equal(email, user.Email);
            Assert.Equal("Test User", user.Name.Value);
            Assert.Equal("test@example.com", user.Email.Address);
        }

        [Fact]
        public void Alarm_Should_Manage_Schedules()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var alarm = new Alarm(Guid.NewGuid(), "Test Alarm", DateTime.Now.AddHours(1), true, userId);
            var schedule = new Schedule(
                Guid.NewGuid(),
                TimeOnly.FromDateTime(DateTime.Now.AddHours(1)),
                ScheduleRecurrence.Daily,
                DaysOfWeek.Monday,
                alarm.Id);

            // Act
            alarm.AddSchedule(schedule);

            // Assert
            Assert.Single(alarm.Schedules);
            Assert.Equal(schedule.Id, alarm.Schedules[0].Id);
        }

        [Fact]
        public void Alarm_Should_Manage_Routines()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var alarm = new Alarm(Guid.NewGuid(), "Test Alarm", DateTime.Now.AddHours(1), true, userId);
            var routine = new Routine(Guid.NewGuid(), "Daily Routine", userId);

            // Act
            alarm.AddRoutine(routine);

            // Assert
            Assert.Single(alarm.Routines);
            Assert.Equal(routine.Id, alarm.Routines[0].Id);
        }

        [Fact]
        public void User_Should_Manage_Credentials()
        {
            // Arrange
            var user = new User(Guid.NewGuid(), new Name("Test User"), new Email("test@example.com"));
            var credential = new UserCredential
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CredentialId = "test-credential",
                PublicKey = new byte[] { 1, 2, 3 },
                UserHandle = new byte[] { 4, 5, 6 }
            };

            // Act
            user.AddCredential(credential);

            // Assert
            Assert.Single(user.Credentials);
            Assert.Equal(credential.Id, user.Credentials.First().Id);
        }

        [Fact]
        public async Task Multiple_Domain_Services_Should_Work_Together()
        {
            // Arrange
            var mockAlarmRepository = new Mock<IAlarmRepository>();
            var mockUserRepository = new Mock<IUserRepository>();
            var mockAlarmLogger = new Mock<ILogger<AlarmDomainService>>();
            var mockUserLogger = new Mock<ILogger<UserDomainService>>();

            var alarmService = new AlarmDomainService(mockAlarmRepository.Object, mockAlarmLogger.Object);
            var userService = new UserDomainService(mockUserRepository.Object, mockAlarmRepository.Object, mockUserLogger.Object);

            var userId = Guid.NewGuid();
            var user = new User(userId, new Name("Test User"), new Email("test@example.com"));

            mockUserRepository.Setup(r => r.GetByIdAsync(userId, default))
                .ReturnsAsync(user);
            mockAlarmRepository.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(new List<Alarm>());

            // Act
            var hasActiveAlarms = await userService.HasActiveAlarmsAsync(userId);
            var canCreateAlarm = await alarmService.CanUserCreateAlarmAsync(userId);

            // Assert
            Assert.False(hasActiveAlarms); // No active alarms
            Assert.True(canCreateAlarm); // Can create alarms
        }
    }
}
