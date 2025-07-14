using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Application.Handlers.Holiday;
using SmartAlarm.Application.Queries.Holiday;
using SmartAlarm.Domain.Abstractions;
using Xunit;
using FluentAssertions;

namespace SmartAlarm.Tests.Application.Handlers.Holiday
{
    /// <summary>
    /// Testes unitários para GetHolidayByIdHandler.
    /// </summary>
    public class GetHolidayByIdHandlerTests
    {
        private readonly Mock<IHolidayRepository> _mockRepository;
        private readonly Mock<ILogger<GetHolidayByIdHandler>> _mockLogger;
        private readonly GetHolidayByIdHandler _handler;

        public GetHolidayByIdHandlerTests()
        {
            _mockRepository = new Mock<IHolidayRepository>();
            _mockLogger = new Mock<ILogger<GetHolidayByIdHandler>>();
            _handler = new GetHolidayByIdHandler(_mockRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_WithExistingHoliday_ShouldReturnHolidayDto()
        {
            // Arrange
            var holidayId = Guid.NewGuid();
            var holiday = new SmartAlarm.Domain.Entities.Holiday(holidayId, new DateTime(2024, 12, 25), "Natal");
            var query = new GetHolidayByIdQuery(holidayId);
            var cancellationToken = CancellationToken.None;

            _mockRepository.Setup(r => r.GetByIdAsync(holidayId, cancellationToken))
                          .ReturnsAsync(holiday);

            // Act
            var result = await _handler.Handle(query, cancellationToken);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(holidayId);
            result.Description.Should().Be("Natal");
            result.Date.Should().Be(new DateTime(2024, 12, 25));
        }

        [Fact]
        public async Task Handle_WithNonExistingHoliday_ShouldReturnNull()
        {
            // Arrange
            var holidayId = Guid.NewGuid();
            var query = new GetHolidayByIdQuery(holidayId);
            var cancellationToken = CancellationToken.None;

            _mockRepository.Setup(r => r.GetByIdAsync(holidayId, cancellationToken))
                          .ReturnsAsync((SmartAlarm.Domain.Entities.Holiday?)null);

            // Act
            var result = await _handler.Handle(query, cancellationToken);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task Handle_WithValidQuery_ShouldLogInformation()
        {
            // Arrange
            var holidayId = Guid.NewGuid();
            var query = new GetHolidayByIdQuery(holidayId);
            var cancellationToken = CancellationToken.None;

            _mockRepository.Setup(r => r.GetByIdAsync(holidayId, cancellationToken))
                          .ReturnsAsync((SmartAlarm.Domain.Entities.Holiday?)null);

            // Act
            await _handler.Handle(query, cancellationToken);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Getting holiday by ID")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithRepositoryException_ShouldPropagateException()
        {
            // Arrange
            var holidayId = Guid.NewGuid();
            var query = new GetHolidayByIdQuery(holidayId);
            var cancellationToken = CancellationToken.None;
            var expectedException = new InvalidOperationException("Database error");

            _mockRepository.Setup(r => r.GetByIdAsync(holidayId, cancellationToken))
                          .ThrowsAsync(expectedException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(query, cancellationToken));
            exception.Should().Be(expectedException);
        }
    }
}
