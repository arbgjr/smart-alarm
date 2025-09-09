using SmartAlarm.Domain.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Application.Commands.Holiday;
using SmartAlarm.Application.Handlers.Holiday;
using SmartAlarm.Domain.Repositories;
using Xunit;
using FluentAssertions;

namespace SmartAlarm.Tests.Application.Handlers.Holiday
{
    /// <summary>
    /// Testes unitários para CreateHolidayHandler.
    /// </summary>
    public class CreateHolidayHandlerTests
    {
        private readonly Mock<IHolidayRepository> _mockRepository;
        private readonly Mock<ILogger<CreateHolidayHandler>> _mockLogger;
        private readonly CreateHolidayHandler _handler;

        public CreateHolidayHandlerTests()
        {
            _mockRepository = new Mock<IHolidayRepository>();
            _mockLogger = new Mock<ILogger<CreateHolidayHandler>>();
            _handler = new CreateHolidayHandler(_mockRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_WithValidCommand_ShouldCreateHolidayAndReturnDto()
        {
            // Arrange
            var command = new CreateHolidayCommand(new DateTime(2024, 12, 25), "Natal");
            var cancellationToken = CancellationToken.None;

            _mockRepository.Setup(r => r.AddAsync(It.IsAny<SmartAlarm.Domain.Entities.Holiday>(), cancellationToken))
                          .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, cancellationToken);

            // Assert
            result.Should().NotBeNull();
            result.Description.Should().Be("Natal");
            result.Date.Should().Be(new DateTime(2024, 12, 25));
            result.Id.Should().NotBe(Guid.Empty);

            _mockRepository.Verify(r => r.AddAsync(It.IsAny<SmartAlarm.Domain.Entities.Holiday>(), cancellationToken), Times.Once);
        }

        [Fact]
        public async Task Handle_WithValidCommand_ShouldLogInformation()
        {
            // Arrange
            var command = new CreateHolidayCommand(new DateTime(2024, 12, 25), "Natal");
            var cancellationToken = CancellationToken.None;

            _mockRepository.Setup(r => r.AddAsync(It.IsAny<SmartAlarm.Domain.Entities.Holiday>(), cancellationToken))
                          .Returns(Task.CompletedTask);

            // Act
            await _handler.Handle(command, cancellationToken);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Creating new holiday")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Holiday created successfully")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public async Task Handle_WithInvalidDescription_ShouldThrowArgumentException(string invalidDescription)
        {
            // Arrange
            var command = new CreateHolidayCommand(new DateTime(2024, 12, 25), invalidDescription);
            var cancellationToken = CancellationToken.None;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(command, cancellationToken));

            _mockRepository.Verify(r => r.AddAsync(It.IsAny<SmartAlarm.Domain.Entities.Holiday>(), cancellationToken), Times.Never);
        }

        [Fact]
        public async Task Handle_WithRepositoryException_ShouldPropagateException()
        {
            // Arrange
            var command = new CreateHolidayCommand(new DateTime(2024, 12, 25), "Natal");
            var cancellationToken = CancellationToken.None;
            var expectedException = new InvalidOperationException("Database error");

            _mockRepository.Setup(r => r.AddAsync(It.IsAny<SmartAlarm.Domain.Entities.Holiday>(), cancellationToken))
                          .ThrowsAsync(expectedException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, cancellationToken));
            exception.Should().Be(expectedException);
        }
    }
}
