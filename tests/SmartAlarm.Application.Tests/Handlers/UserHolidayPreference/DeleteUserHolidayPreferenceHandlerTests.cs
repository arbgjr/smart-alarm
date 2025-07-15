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
    public class DeleteUserHolidayPreferenceHandlerTests
    {
        private readonly Mock<IUserHolidayPreferenceRepository> _repositoryMock;
        private readonly Mock<ILogger<DeleteUserHolidayPreferenceHandler>> _loggerMock;
        private readonly DeleteUserHolidayPreferenceHandler _handler;

        private readonly Guid _testPreferenceId = Guid.NewGuid();
        private readonly Guid _testUserId = Guid.NewGuid();
        private readonly Guid _testHolidayId = Guid.NewGuid();

        public DeleteUserHolidayPreferenceHandlerTests()
        {
            _repositoryMock = new Mock<IUserHolidayPreferenceRepository>();
            _loggerMock = new Mock<ILogger<DeleteUserHolidayPreferenceHandler>>();
            _handler = new DeleteUserHolidayPreferenceHandler(_repositoryMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldDeleteUserHolidayPreference()
        {
            // Arrange
            var existingPreference = new Domain.Entities.UserHolidayPreference(
                _testPreferenceId,
                _testUserId,
                _testHolidayId,
                true,
                HolidayPreferenceAction.Skip
            );

            _repositoryMock.Setup(x => x.GetByIdAsync(_testPreferenceId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingPreference);

            _repositoryMock.Setup(x => x.DeleteAsync(existingPreference, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var command = new DeleteUserHolidayPreferenceCommand { Id = _testPreferenceId };

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _repositoryMock.Verify(x => x.DeleteAsync(existingPreference, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ThrowArgumentException_When_PreferenceNotFound()
        {
            // Arrange
            var command = new DeleteUserHolidayPreferenceCommand { Id = _testPreferenceId };

            _repositoryMock.Setup(x => x.GetByIdAsync(_testPreferenceId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.UserHolidayPreference?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(command, CancellationToken.None));
            _repositoryMock.Verify(x => x.DeleteAsync(It.IsAny<Domain.Entities.UserHolidayPreference>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ThrowArgumentException_When_CommandIsNull()
        {
            // Arrange & Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.Handle(null!, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_Should_ThrowArgumentException_When_IdIsEmpty()
        {
            // Arrange
            var command = new DeleteUserHolidayPreferenceCommand { Id = Guid.Empty };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_Should_LogInformation_When_PreferenceDeleted()
        {
            // Arrange
            var existingPreference = new Domain.Entities.UserHolidayPreference(
                _testPreferenceId,
                _testUserId,
                _testHolidayId,
                true,
                HolidayPreferenceAction.Skip
            );

            _repositoryMock.Setup(x => x.GetByIdAsync(_testPreferenceId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingPreference);

            _repositoryMock.Setup(x => x.DeleteAsync(existingPreference, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var command = new DeleteUserHolidayPreferenceCommand { Id = _testPreferenceId };

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Deleted UserHolidayPreference")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Should_HandleException_When_RepositoryThrows()
        {
            // Arrange
            var existingPreference = new Domain.Entities.UserHolidayPreference(
                _testPreferenceId,
                _testUserId,
                _testHolidayId,
                true,
                HolidayPreferenceAction.Skip
            );

            _repositoryMock.Setup(x => x.GetByIdAsync(_testPreferenceId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingPreference);

            _repositoryMock.Setup(x => x.DeleteAsync(existingPreference, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Database error"));

            var command = new DeleteUserHolidayPreferenceCommand { Id = _testPreferenceId };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
        }
    }
}
