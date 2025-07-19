using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Application.Commands.UserHolidayPreference;
using SmartAlarm.Application.Handlers.UserHolidayPreference;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Abstractions;
using Xunit;

namespace SmartAlarm.Application.Tests.Handlers.UserHolidayPreference
{
    public class UpdateUserHolidayPreferenceHandlerTests
    {
        private readonly Mock<IUserHolidayPreferenceRepository> _repositoryMock;
        private readonly Mock<ILogger<UpdateUserHolidayPreferenceHandler>> _loggerMock;
        private readonly UpdateUserHolidayPreferenceHandler _handler;

        private readonly Guid _testPreferenceId = Guid.NewGuid();
        private readonly Guid _testUserId = Guid.NewGuid();
        private readonly Guid _testHolidayId = Guid.NewGuid();
        private readonly Domain.Entities.UserHolidayPreference _testPreference;

        public UpdateUserHolidayPreferenceHandlerTests()
        {
            _repositoryMock = new Mock<IUserHolidayPreferenceRepository>();
            _loggerMock = new Mock<ILogger<UpdateUserHolidayPreferenceHandler>>();
            _handler = new UpdateUserHolidayPreferenceHandler(_repositoryMock.Object, _loggerMock.Object);

            _testPreference = new Domain.Entities.UserHolidayPreference(
                _testPreferenceId,
                _testUserId,
                _testHolidayId,
                true,
                HolidayPreferenceAction.Skip
            );
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldUpdateUserHolidayPreference()
        {
            // Arrange
            var existingPreference = _testPreference;
            var command = new UpdateUserHolidayPreferenceCommand
            {
                Id = _testPreferenceId,
                IsEnabled = false,
                Action = HolidayPreferenceAction.Skip
            };

            _repositoryMock.Setup(x => x.GetByIdAsync(_testPreferenceId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingPreference);

            _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.UserHolidayPreference>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(_testPreferenceId);
            result.IsEnabled.Should().BeFalse();
            result.Action.Should().Be(HolidayPreferenceAction.Skip);

            _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Domain.Entities.UserHolidayPreference>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ReturnNull_When_PreferenceNotFound()
        {
            // Arrange
            var command = new UpdateUserHolidayPreferenceCommand
            {
                Id = _testPreferenceId,
                IsEnabled = false,
                Action = HolidayPreferenceAction.Skip
            };

            _repositoryMock.Setup(x => x.GetByIdAsync(_testPreferenceId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.UserHolidayPreference?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Handle_Should_ThrowArgumentException_When_CommandIsNull()
        {
            // Arrange & Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.Handle(null!, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_Should_UpdatePreference_When_ValidDelayAction()
        {
            // Arrange
            var existingPreference = _testPreference;
            var command = new UpdateUserHolidayPreferenceCommand
            {
                Id = _testPreferenceId,
                IsEnabled = true,
                Action = HolidayPreferenceAction.Delay,
                DelayInMinutes = 30
            };

            _repositoryMock.Setup(x => x.GetByIdAsync(_testPreferenceId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingPreference);

            _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.UserHolidayPreference>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(_testPreferenceId);
            result.IsEnabled.Should().BeTrue();
            result.Action.Should().Be(HolidayPreferenceAction.Delay);
            result.DelayInMinutes.Should().Be(30);

            _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Domain.Entities.UserHolidayPreference>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_LogInformation_When_PreferenceUpdated()
        {
            // Arrange
            var existingPreference = _testPreference;
            var command = new UpdateUserHolidayPreferenceCommand
            {
                Id = _testPreferenceId,
                IsEnabled = false,
                Action = HolidayPreferenceAction.Skip
            };

            _repositoryMock.Setup(x => x.GetByIdAsync(_testPreferenceId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingPreference);

            _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.UserHolidayPreference>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Updated UserHolidayPreference")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Should_HandleException_When_RepositoryThrows()
        {
            // Arrange
            var existingPreference = _testPreference;
            var command = new UpdateUserHolidayPreferenceCommand
            {
                Id = _testPreferenceId,
                IsEnabled = false,
                Action = HolidayPreferenceAction.Skip
            };

            _repositoryMock.Setup(x => x.GetByIdAsync(_testPreferenceId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingPreference);

            _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.UserHolidayPreference>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
        }
    }
}
