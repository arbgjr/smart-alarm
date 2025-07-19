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
    public class UserDomainServiceTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IAlarmRepository> _alarmRepositoryMock;
        private readonly Mock<ILogger<UserDomainService>> _loggerMock;
        private readonly UserDomainService _service;

        public UserDomainServiceTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _alarmRepositoryMock = new Mock<IAlarmRepository>();
            _loggerMock = new Mock<ILogger<UserDomainService>>();
            _service = new UserDomainService(_userRepositoryMock.Object, _alarmRepositoryMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task IsEmailAlreadyInUseAsync_Should_Return_True_If_Email_Exists()
        {
            // Arrange
            var user = new User(Guid.NewGuid(), "Test", "test@email.com");
            _userRepositoryMock.Setup(r => r.GetByEmailAsync(user.Email.Address)).ReturnsAsync(user);

            // Act
            var result = await _service.IsEmailAlreadyInUseAsync(user.Email.Address);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsEmailAlreadyInUseAsync_Should_Return_False_If_Email_Not_Exists()
        {
            // Arrange
            _userRepositoryMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User)null);

            // Act
            var result = await _service.IsEmailAlreadyInUseAsync("notfound@email.com");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CanActivateUserAsync_Should_Return_False_If_User_Not_Found()
        {
            // Arrange
            _userRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User)null);

            // Act
            var result = await _service.CanActivateUserAsync(Guid.NewGuid());

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CanActivateUserAsync_Should_Return_True_If_User_Inactive()
        {
            // Arrange
            var user = new User(Guid.NewGuid(), "Test", "test@email.com", false);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

            // Act
            var result = await _service.CanActivateUserAsync(user.Id);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CanDeactivateUserAsync_Should_Return_False_If_User_Not_Found()
        {
            // Arrange
            _userRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User)null);

            // Act
            var result = await _service.CanDeactivateUserAsync(Guid.NewGuid());

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CanDeactivateUserAsync_Should_Return_True_If_User_Active_And_No_Active_Alarms()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User(userId, "Test", "test@email.com", true);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _alarmRepositoryMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<Alarm>());

            // Act
            var result = await _service.CanDeactivateUserAsync(userId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task HasActiveAlarmsAsync_Should_Return_True_If_Any_Alarm_Enabled()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var alarms = new List<Alarm> { new Alarm(Guid.NewGuid(), "A", DateTime.Now, true, userId) };
            _alarmRepositoryMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(alarms);

            // Act
            var result = await _service.HasActiveAlarmsAsync(userId);

            // Assert
            Assert.True(result);
        }
    }
}
