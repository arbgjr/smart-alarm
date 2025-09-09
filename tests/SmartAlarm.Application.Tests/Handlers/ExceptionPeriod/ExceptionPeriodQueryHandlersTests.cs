using SmartAlarm.Domain.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Application.Handlers.ExceptionPeriod;
using SmartAlarm.Application.Queries.ExceptionPeriod;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using Xunit;

namespace SmartAlarm.Application.Tests.Handlers.ExceptionPeriod;

public class ExceptionPeriodQueryHandlersTests
{
    private readonly Mock<IExceptionPeriodRepository> _repositoryMock;
    private readonly Mock<ILogger<ExceptionPeriodQueryHandlers>> _loggerMock;
    private readonly ExceptionPeriodQueryHandlers _handler;
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _testPeriodId = Guid.NewGuid();

    public ExceptionPeriodQueryHandlersTests()
    {
        _repositoryMock = new Mock<IExceptionPeriodRepository>();
        _loggerMock = new Mock<ILogger<ExceptionPeriodQueryHandlers>>();
        _handler = new ExceptionPeriodQueryHandlers(_repositoryMock.Object, _loggerMock.Object);
    }

    #region GetExceptionPeriodByIdQuery Tests

    [Fact]
    public async Task Handle_GetById_Should_ReturnExceptionPeriod_When_Found()
    {
        // Arrange
        var existingPeriod = new Domain.Entities.ExceptionPeriod(
            _testPeriodId,
            "Test Period",
            DateTime.Today.AddDays(1),
            DateTime.Today.AddDays(5),
            ExceptionPeriodType.Vacation,
            _testUserId,
            "Description");

        var query = new GetExceptionPeriodByIdQuery(_testPeriodId, _testUserId);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(_testPeriodId))
            .ReturnsAsync(existingPeriod);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(_testPeriodId);
        result.Name.Should().Be("Test Period");
        result.UserId.Should().Be(_testUserId);

        _repositoryMock.Verify(x => x.GetByIdAsync(_testPeriodId), Times.Once);
    }

    [Fact]
    public async Task Handle_GetById_Should_ReturnNull_When_NotFound()
    {
        // Arrange
        var query = new GetExceptionPeriodByIdQuery(_testPeriodId, _testUserId);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(_testPeriodId))
            .ReturnsAsync((Domain.Entities.ExceptionPeriod?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();

        _repositoryMock.Verify(x => x.GetByIdAsync(_testPeriodId), Times.Once);
    }

    [Fact]
    public async Task Handle_GetById_Should_ReturnNull_When_UserNotOwner()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        var existingPeriod = new Domain.Entities.ExceptionPeriod(
            _testPeriodId,
            "Test Period",
            DateTime.Today.AddDays(1),
            DateTime.Today.AddDays(5),
            ExceptionPeriodType.Vacation,
            otherUserId, // Diferente do usuário da query
            "Description");

        var query = new GetExceptionPeriodByIdQuery(_testPeriodId, _testUserId);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(_testPeriodId))
            .ReturnsAsync(existingPeriod);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();

        _repositoryMock.Verify(x => x.GetByIdAsync(_testPeriodId), Times.Once);
    }

    #endregion

    #region ListExceptionPeriodsQuery Tests

    [Fact]
    public async Task Handle_List_Should_ReturnAllUserPeriods_When_NoFilters()
    {
        // Arrange
        var periods = new List<Domain.Entities.ExceptionPeriod>
        {
            new(_testPeriodId, "Period 1", DateTime.Today.AddDays(1), DateTime.Today.AddDays(5),
                ExceptionPeriodType.Vacation, _testUserId, "Desc 1"),
            new(Guid.NewGuid(), "Period 2", DateTime.Today.AddDays(10), DateTime.Today.AddDays(15),
                ExceptionPeriodType.Travel, _testUserId, "Desc 2")
        };

        var query = new ListExceptionPeriodsQuery(_testUserId);

        _repositoryMock
            .Setup(x => x.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(periods);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.UserId == _testUserId);
        result.Should().OnlyContain(p => p.IsActive);

        _repositoryMock.Verify(x => x.GetByUserIdAsync(_testUserId), Times.Once);
    }

    [Fact]
    public async Task Handle_List_Should_FilterByType_When_TypeSpecified()
    {
        // Arrange
        var periods = new List<Domain.Entities.ExceptionPeriod>
        {
            new(_testPeriodId, "Vacation Period", DateTime.Today.AddDays(1), DateTime.Today.AddDays(5),
                ExceptionPeriodType.Vacation, _testUserId, "Desc 1"),
            new(Guid.NewGuid(), "Travel Period", DateTime.Today.AddDays(10), DateTime.Today.AddDays(15),
                ExceptionPeriodType.Travel, _testUserId, "Desc 2")
        };

        var query = new ListExceptionPeriodsQuery(_testUserId)
        {
            Type = ExceptionPeriodType.Vacation
        };

        _repositoryMock
            .Setup(x => x.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(periods);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().Type.Should().Be(ExceptionPeriodType.Vacation);

        _repositoryMock.Verify(x => x.GetByUserIdAsync(_testUserId), Times.Once);
    }

    [Fact]
    public async Task Handle_List_Should_IncludeInactivePeriods_When_OnlyActiveIsFalse()
    {
        // Arrange
        var activePeriod = new Domain.Entities.ExceptionPeriod(
            _testPeriodId, "Active Period", DateTime.Today.AddDays(1), DateTime.Today.AddDays(5),
            ExceptionPeriodType.Vacation, _testUserId, "Desc 1");

        var inactivePeriod = new Domain.Entities.ExceptionPeriod(
            Guid.NewGuid(), "Inactive Period", DateTime.Today.AddDays(10), DateTime.Today.AddDays(15),
            ExceptionPeriodType.Travel, _testUserId, "Desc 2");
        inactivePeriod.Deactivate();

        var periods = new List<Domain.Entities.ExceptionPeriod> { activePeriod, inactivePeriod };

        var query = new ListExceptionPeriodsQuery(_testUserId)
        {
            OnlyActive = false
        };

        _repositoryMock
            .Setup(x => x.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(periods);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(p => p.IsActive);
        result.Should().Contain(p => !p.IsActive);

        _repositoryMock.Verify(x => x.GetByUserIdAsync(_testUserId), Times.Once);
    }

    [Fact]
    public async Task Handle_List_Should_FilterByActiveOnDate_When_DateSpecified()
    {
        // Arrange
        var targetDate = DateTime.Today.AddDays(3);
        
        var period1 = new Domain.Entities.ExceptionPeriod(
            _testPeriodId, "Period 1", DateTime.Today.AddDays(1), DateTime.Today.AddDays(5),
            ExceptionPeriodType.Vacation, _testUserId, "Desc 1"); // Ativo na data

        var period2 = new Domain.Entities.ExceptionPeriod(
            Guid.NewGuid(), "Period 2", DateTime.Today.AddDays(10), DateTime.Today.AddDays(15),
            ExceptionPeriodType.Travel, _testUserId, "Desc 2"); // Não ativo na data

        var periods = new List<Domain.Entities.ExceptionPeriod> { period1, period2 };

        var query = new ListExceptionPeriodsQuery(_testUserId)
        {
            ActiveOnDate = targetDate
        };

        _repositoryMock
            .Setup(x => x.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(periods);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().Id.Should().Be(_testPeriodId);

        _repositoryMock.Verify(x => x.GetByUserIdAsync(_testUserId), Times.Once);
    }

    #endregion

    #region GetActiveExceptionPeriodsOnDateQuery Tests

    [Fact]
    public async Task Handle_GetActiveOnDate_Should_ReturnActivePeriods()
    {
        // Arrange
        var targetDate = DateTime.Today.AddDays(3);
        var periods = new List<Domain.Entities.ExceptionPeriod>
        {
            new(_testPeriodId, "Active Period", DateTime.Today.AddDays(1), DateTime.Today.AddDays(5),
                ExceptionPeriodType.Vacation, _testUserId, "Desc 1"),
            new(Guid.NewGuid(), "Another Active Period", DateTime.Today.AddDays(2), DateTime.Today.AddDays(8),
                ExceptionPeriodType.Travel, _testUserId, "Desc 2")
        };

        var query = new GetActiveExceptionPeriodsOnDateQuery(_testUserId, targetDate);

        _repositoryMock
            .Setup(x => x.GetActivePeriodsOnDateAsync(_testUserId, targetDate))
            .ReturnsAsync(periods);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.UserId == _testUserId);

        _repositoryMock.Verify(x => x.GetActivePeriodsOnDateAsync(_testUserId, targetDate), Times.Once);
    }

    [Fact]
    public async Task Handle_GetActiveOnDate_Should_ReturnEmptyList_When_NoActivePeriods()
    {
        // Arrange
        var targetDate = DateTime.Today.AddDays(3);
        var query = new GetActiveExceptionPeriodsOnDateQuery(_testUserId, targetDate);

        _repositoryMock
            .Setup(x => x.GetActivePeriodsOnDateAsync(_testUserId, targetDate))
            .ReturnsAsync(new List<Domain.Entities.ExceptionPeriod>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();

        _repositoryMock.Verify(x => x.GetActivePeriodsOnDateAsync(_testUserId, targetDate), Times.Once);
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task Handle_List_Should_LogInformation()
    {
        // Arrange
        var query = new ListExceptionPeriodsQuery(_testUserId);
        _repositoryMock
            .Setup(x => x.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(new List<Domain.Entities.ExceptionPeriod>());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Listing exception periods")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}
