using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Application.Commands.UserHolidayPreference;
using SmartAlarm.Application.Handlers.UserHolidayPreference;
using SmartAlarm.Domain.Abstractions;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.ValueObjects;
using Xunit;

namespace SmartAlarm.Application.Tests.Handlers.UserHolidayPreference
{
    public class CreateUserHolidayPreferenceHandlerTests
    {
        private readonly Mock<IUserHolidayPreferenceRepository> _userHolidayPreferenceRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IHolidayRepository> _holidayRepositoryMock;
        private readonly Mock<ILogger<CreateUserHolidayPreferenceHandler>> _loggerMock;
        private readonly CreateUserHolidayPreferenceHandler _handler;

        private readonly Guid _testUserId = Guid.NewGuid();
        private readonly Guid _testHolidayId = Guid.NewGuid();
        private readonly User _testUser;
        private readonly Holiday _testHoliday;

        public CreateUserHolidayPreferenceHandlerTests()
        {
            _userHolidayPreferenceRepositoryMock = new Mock<IUserHolidayPreferenceRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _holidayRepositoryMock = new Mock<IHolidayRepository>();
            _loggerMock = new Mock<ILogger<CreateUserHolidayPreferenceHandler>>();

            _handler = new CreateUserHolidayPreferenceHandler(
                _userHolidayPreferenceRepositoryMock.Object,
                _userRepositoryMock.Object,
                _holidayRepositoryMock.Object,
                _loggerMock.Object);

            _testUser = new User(_testUserId, "Test User", "test@example.com");
            _testHoliday = new Holiday(_testHolidayId, DateTime.Today, "Test Holiday");
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldCreateUserHolidayPreference()
        {
            // Arrange
            var command = new CreateUserHolidayPreferenceCommand
            {
                UserId = _testUserId,
                HolidayId = _testHolidayId,
                Action = HolidayPreferenceAction.Skip,
                IsEnabled = true
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(_testUserId))
                .ReturnsAsync(_testUser);

            _holidayRepositoryMock.Setup(x => x.GetByIdAsync(_testHolidayId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(_testHoliday);

            _userHolidayPreferenceRepositoryMock.Setup(x => x.GetByUserAndHolidayAsync(_testUserId, _testHolidayId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.UserHolidayPreference?)null);

            _userHolidayPreferenceRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Domain.Entities.UserHolidayPreference>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be(_testUserId);
            result.HolidayId.Should().Be(_testHolidayId);
            result.Action.Should().Be(HolidayPreferenceAction.Skip);
            result.IsEnabled.Should().BeTrue();

            _userHolidayPreferenceRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Domain.Entities.UserHolidayPreference>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ThrowArgumentException_When_UserNotFound()
        {
            // Arrange
            var command = new CreateUserHolidayPreferenceCommand
            {
                UserId = _testUserId,
                HolidayId = _testHolidayId,
                Action = HolidayPreferenceAction.Skip,
                IsEnabled = true
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(_testUserId))
                .ReturnsAsync((User?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_Should_ThrowArgumentException_When_HolidayNotFound()
        {
            // Arrange
            var command = new CreateUserHolidayPreferenceCommand
            {
                UserId = _testUserId,
                HolidayId = _testHolidayId,
                Action = HolidayPreferenceAction.Skip,
                IsEnabled = true
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(_testUserId))
                .ReturnsAsync(_testUser);

            _holidayRepositoryMock.Setup(x => x.GetByIdAsync(_testHolidayId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Holiday?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_Should_ThrowArgumentException_When_PreferenceAlreadyExists()
        {
            // Arrange
            var existingPreference = new Domain.Entities.UserHolidayPreference(
                Guid.NewGuid(),
                _testUserId,
                _testHolidayId,
                true,
                HolidayPreferenceAction.Skip
            );

            var command = new CreateUserHolidayPreferenceCommand
            {
                UserId = _testUserId,
                HolidayId = _testHolidayId,
                Action = HolidayPreferenceAction.Skip,
                IsEnabled = true
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(_testUserId))
                .ReturnsAsync(_testUser);

            _holidayRepositoryMock.Setup(x => x.GetByIdAsync(_testHolidayId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(_testHoliday);

            _userHolidayPreferenceRepositoryMock.Setup(x => x.GetByUserAndHolidayAsync(_testUserId, _testHolidayId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingPreference);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_Should_SetDelayInMinutes_When_ActionIsDelay()
        {
            // Arrange
            var command = new CreateUserHolidayPreferenceCommand
            {
                UserId = _testUserId,
                HolidayId = _testHolidayId,
                Action = HolidayPreferenceAction.Delay,
                DelayInMinutes = 30,
                IsEnabled = true
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(_testUserId))
                .ReturnsAsync(_testUser);

            _holidayRepositoryMock.Setup(x => x.GetByIdAsync(_testHolidayId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(_testHoliday);

            _userHolidayPreferenceRepositoryMock.Setup(x => x.GetByUserAndHolidayAsync(_testUserId, _testHolidayId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.UserHolidayPreference?)null);

            _userHolidayPreferenceRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Domain.Entities.UserHolidayPreference>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Action.Should().Be(HolidayPreferenceAction.Delay);
            result.DelayInMinutes.Should().Be(30);
        }

        [Fact]
        public async Task Handle_Should_LogInformation_When_PreferenceCreated()
        {
            // Arrange
            var command = new CreateUserHolidayPreferenceCommand
            {
                UserId = _testUserId,
                HolidayId = _testHolidayId,
                Action = HolidayPreferenceAction.Skip,
                IsEnabled = true
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(_testUserId))
                .ReturnsAsync(_testUser);

            _holidayRepositoryMock.Setup(x => x.GetByIdAsync(_testHolidayId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(_testHoliday);

            _userHolidayPreferenceRepositoryMock.Setup(x => x.GetByUserAndHolidayAsync(_testUserId, _testHolidayId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.UserHolidayPreference?)null);

            _userHolidayPreferenceRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Domain.Entities.UserHolidayPreference>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Created UserHolidayPreference")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Should_HandleException_When_RepositoryThrows()
        {
            // Arrange
            var command = new CreateUserHolidayPreferenceCommand
            {
                UserId = _testUserId,
                HolidayId = _testHolidayId,
                Action = HolidayPreferenceAction.Skip,
                IsEnabled = true
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(_testUserId))
                .ReturnsAsync(_testUser);

            _holidayRepositoryMock.Setup(x => x.GetByIdAsync(_testHolidayId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(_testHoliday);

            _userHolidayPreferenceRepositoryMock.Setup(x => x.GetByUserAndHolidayAsync(_testUserId, _testHolidayId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.UserHolidayPreference?)null);

            _userHolidayPreferenceRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Domain.Entities.UserHolidayPreference>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
        }
    }
}
