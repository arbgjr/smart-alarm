using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Domain.Services;
using SmartAlarm.Domain.ValueObjects;
using Xunit;

namespace SmartAlarm.UnitTests
{
    public class DomainTests
    {
        [Fact]
        public void Alarm_Constructor_Should_Create_Valid_Alarm()
        {
            // Arrange
            var id = Guid.NewGuid();
            var name = "Test Alarm";
            var time = DateTime.Now;
            var userId = Guid.NewGuid();

            // Act
            var alarm = new Alarm(id, name, time, true, userId);

            // Assert
            Assert.Equal(id, alarm.Id);
            Assert.Equal(name, alarm.Name.Value);
            Assert.Equal(time, alarm.Time);
            Assert.True(alarm.Enabled);
            Assert.Equal(userId, alarm.UserId);
        }

        [Fact]
        public void Alarm_Enable_Should_Set_Enabled_To_True()
        {
            // Arrange
            var alarm = new Alarm(Guid.NewGuid(), "Test", DateTime.Now, false, Guid.NewGuid());

            // Act
            alarm.Enable();

            // Assert
            Assert.True(alarm.Enabled);
        }

        [Fact]
        public void Alarm_Disable_Should_Set_Enabled_To_False()
        {
            // Arrange
            var alarm = new Alarm(Guid.NewGuid(), "Test", DateTime.Now, true, Guid.NewGuid());

            // Act
            alarm.Disable();

            // Assert
            Assert.False(alarm.Enabled);
        }

        [Fact]
        public void User_Constructor_Should_Create_Valid_User()
        {
            // Arrange
            var id = Guid.NewGuid();
            var name = new Name("Test User");
            var email = new Email("test@example.com");

            // Act
            var user = new User(id, name, email);

            // Assert
            Assert.Equal(id, user.Id);
            Assert.Equal(name, user.Name);
            Assert.Equal(email, user.Email);
        }

        [Fact]
        public void Name_ValueObject_Should_Validate_Input()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new Name(""));
            Assert.Throws<ArgumentException>(() => new Name(null));

            // Valid name should not throw
            var validName = new Name("Valid Name");
            Assert.Equal("Valid Name", validName.Value);
        }

        [Fact]
        public void Email_ValueObject_Should_Validate_Input()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new Email("invalid-email"));
            Assert.Throws<ArgumentException>(() => new Email(""));
            Assert.Throws<ArgumentException>(() => new Email(null));

            // Valid email should not throw
            var validEmail = new Email("test@example.com");
            Assert.Equal("test@example.com", validEmail.Address);
        }

        [Fact]
        public async Task AlarmDomainService_CanUserCreateAlarmAsync_Should_Return_True_For_New_User()
        {
            // Arrange
            var mockRepository = new Mock<IAlarmRepository>();
            var mockLogger = new Mock<ILogger<AlarmDomainService>>();
            var service = new AlarmDomainService(mockRepository.Object, mockLogger.Object);
            var userId = Guid.NewGuid();

            mockRepository.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(new List<Alarm>());

            // Act
            var result = await service.CanUserCreateAlarmAsync(userId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void AlarmDomainService_IsValidAlarmTime_Should_Return_False_For_Past_Time()
        {
            // Arrange
            var mockRepository = new Mock<IAlarmRepository>();
            var mockLogger = new Mock<ILogger<AlarmDomainService>>();
            var service = new AlarmDomainService(mockRepository.Object, mockLogger.Object);
            var pastTime = DateTime.UtcNow.AddMinutes(-10);

            // Act
            var result = service.IsValidAlarmTime(pastTime);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AlarmDomainService_IsValidAlarmTime_Should_Return_True_For_Future_Time()
        {
            // Arrange
            var mockRepository = new Mock<IAlarmRepository>();
            var mockLogger = new Mock<ILogger<AlarmDomainService>>();
            var service = new AlarmDomainService(mockRepository.Object, mockLogger.Object);
            var futureTime = DateTime.UtcNow.AddMinutes(10);

            // Act
            var result = service.IsValidAlarmTime(futureTime);

            // Assert
            Assert.True(result);
        }
    }
}
