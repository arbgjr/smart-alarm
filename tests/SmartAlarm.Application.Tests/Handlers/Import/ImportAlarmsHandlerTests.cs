using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Application.Commands.Import;
using SmartAlarm.Application.DTOs.Import;
using SmartAlarm.Application.Handlers.Import;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Domain.ValueObjects;
using SmartAlarm.Infrastructure.Services;
using Xunit;

namespace SmartAlarm.Application.Tests.Handlers.Import;

public class ImportAlarmsHandlerTests
{
    private readonly Mock<IFileParser> _fileParserMock;
    private readonly Mock<IAlarmRepository> _alarmRepositoryMock;
    private readonly Mock<ILogger<ImportAlarmsHandler>> _loggerMock;
    private readonly ImportAlarmsHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();

    public ImportAlarmsHandlerTests()
    {
        _fileParserMock = new Mock<IFileParser>();
        _alarmRepositoryMock = new Mock<IAlarmRepository>();
        _loggerMock = new Mock<ILogger<ImportAlarmsHandler>>();
        
        _handler = new ImportAlarmsHandler(
            _fileParserMock.Object,
            _alarmRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCsvFile_ShouldImportAlarmsSuccessfully()
    {
        // Arrange
        var fileName = "alarms.csv";
        var fileContent = "Name,Time,Enabled\nMorning Alarm,07:00,true\nEvening Alarm,19:00,false";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
        
        var alarm1 = new Alarm(Guid.NewGuid(), new Name("Morning Alarm"), DateTime.Today.Add(TimeSpan.FromHours(7)), true, _userId);
        var alarm2 = new Alarm(Guid.NewGuid(), new Name("Evening Alarm"), DateTime.Today.Add(TimeSpan.FromHours(19)), false, _userId);
        var parsedAlarms = new List<Alarm> { alarm1, alarm2 };
        
        var command = new ImportAlarmsCommand(stream, fileName, _userId, false);

        _fileParserMock.Setup(x => x.IsFormatSupported(fileName)).Returns(true);
        _fileParserMock.Setup(x => x.ParseAsync(stream, fileName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parsedAlarms);
        _alarmRepositoryMock.Setup(x => x.GetByUserIdAsync(_userId))
            .ReturnsAsync(new List<Alarm>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalRecords.Should().Be(2);
        result.SuccessfulImports.Should().Be(2);
        result.FailedImports.Should().Be(0);
        result.UpdatedImports.Should().Be(0);
        result.Errors.Should().BeEmpty();
        result.ImportedAlarms.Should().HaveCount(2);
        
        _alarmRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Alarm>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_WithUnsupportedFileFormat_ShouldReturnError()
    {
        // Arrange
        var fileName = "alarms.txt";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("test content"));
        var command = new ImportAlarmsCommand(stream, fileName, _userId, false);

        _fileParserMock.Setup(x => x.IsFormatSupported(fileName)).Returns(false);
        _fileParserMock.Setup(x => x.GetSupportedFormats()).Returns(new[] { "csv" });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalRecords.Should().Be(0);
        result.SuccessfulImports.Should().Be(0);
        result.FailedImports.Should().Be(0);
        result.Errors.Should().HaveCount(1);
        result.Errors.First().ErrorCode.Should().Be("UNSUPPORTED_FORMAT");
        result.Errors.First().ErrorType.Should().Be("Validation");
        
        _alarmRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Alarm>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithParsingError_ShouldReturnError()
    {
        // Arrange
        var fileName = "alarms.csv";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("invalid content"));
        var command = new ImportAlarmsCommand(stream, fileName, _userId, false);

        _fileParserMock.Setup(x => x.IsFormatSupported(fileName)).Returns(true);
        _fileParserMock.Setup(x => x.ParseAsync(stream, fileName, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Invalid CSV format"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalRecords.Should().Be(0);
        result.SuccessfulImports.Should().Be(0);
        result.FailedImports.Should().Be(0);
        result.Errors.Should().HaveCount(1);
        result.Errors.First().ErrorCode.Should().Be("PARSING_ERROR");
        result.Errors.First().ErrorType.Should().Be("Parsing");
        
        _alarmRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Alarm>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithDuplicateAlarms_ShouldHandleDuplicates()
    {
        // Arrange
        var fileName = "alarms.csv";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("test"));
        
        var existingAlarm = new Alarm(Guid.NewGuid(), new Name("Morning Alarm"), DateTime.Today.Add(TimeSpan.FromHours(6)), true, _userId);
        var newAlarm = new Alarm(Guid.NewGuid(), new Name("Morning Alarm"), DateTime.Today.Add(TimeSpan.FromHours(7)), true, _userId);
        
        var command = new ImportAlarmsCommand(stream, fileName, _userId, false);

        _fileParserMock.Setup(x => x.IsFormatSupported(fileName)).Returns(true);
        _fileParserMock.Setup(x => x.ParseAsync(stream, fileName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Alarm> { newAlarm });
        _alarmRepositoryMock.Setup(x => x.GetByUserIdAsync(_userId))
            .ReturnsAsync(new List<Alarm> { existingAlarm });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalRecords.Should().Be(1);
        result.SuccessfulImports.Should().Be(0);
        result.FailedImports.Should().Be(1);
        result.UpdatedImports.Should().Be(0);
        result.Errors.Should().HaveCount(1);
        result.Errors.First().ErrorCode.Should().Be("DUPLICATE_ALARM");
        result.Errors.First().ErrorType.Should().Be("Business");
        
        _alarmRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Alarm>()), Times.Never);
        _alarmRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Alarm>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithDuplicateAlarmsAndOverwrite_ShouldUpdateExisting()
    {
        // Arrange
        var fileName = "alarms.csv";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("test"));
        
        var existingAlarm = new Alarm(Guid.NewGuid(), new Name("Morning Alarm"), DateTime.Today.Add(TimeSpan.FromHours(6)), true, _userId);
        var newAlarm = new Alarm(Guid.NewGuid(), new Name("Morning Alarm"), DateTime.Today.Add(TimeSpan.FromHours(7)), false, _userId);
        
        var command = new ImportAlarmsCommand(stream, fileName, _userId, true); // overwrite = true

        _fileParserMock.Setup(x => x.IsFormatSupported(fileName)).Returns(true);
        _fileParserMock.Setup(x => x.ParseAsync(stream, fileName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Alarm> { newAlarm });
        _alarmRepositoryMock.Setup(x => x.GetByUserIdAsync(_userId))
            .ReturnsAsync(new List<Alarm> { existingAlarm });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalRecords.Should().Be(1);
        result.SuccessfulImports.Should().Be(0);
        result.FailedImports.Should().Be(0);
        result.UpdatedImports.Should().Be(1);
        result.Errors.Should().BeEmpty();
        result.ImportedAlarms.Should().HaveCount(1);
        
        _alarmRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Alarm>()), Times.Once);
        _alarmRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Alarm>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithMixedResults_ShouldProcessAllRecords()
    {
        // Arrange
        var fileName = "alarms.csv";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("test"));
        
        var existingAlarm = new Alarm(Guid.NewGuid(), new Name("Existing Alarm"), DateTime.Today.Add(TimeSpan.FromHours(6)), true, _userId);
        var newAlarm1 = new Alarm(Guid.NewGuid(), new Name("New Alarm"), DateTime.Today.Add(TimeSpan.FromHours(7)), true, _userId);
        var duplicateAlarm = new Alarm(Guid.NewGuid(), new Name("Existing Alarm"), DateTime.Today.Add(TimeSpan.FromHours(8)), true, _userId);
        
        var command = new ImportAlarmsCommand(stream, fileName, _userId, false);

        _fileParserMock.Setup(x => x.IsFormatSupported(fileName)).Returns(true);
        _fileParserMock.Setup(x => x.ParseAsync(stream, fileName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Alarm> { newAlarm1, duplicateAlarm });
        _alarmRepositoryMock.Setup(x => x.GetByUserIdAsync(_userId))
            .ReturnsAsync(new List<Alarm> { existingAlarm });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalRecords.Should().Be(2);
        result.SuccessfulImports.Should().Be(1);
        result.FailedImports.Should().Be(1);
        result.UpdatedImports.Should().Be(0);
        result.Errors.Should().HaveCount(1);
        result.ImportedAlarms.Should().HaveCount(1);
        
        _alarmRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Alarm>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithRepositoryError_ShouldHandleException()
    {
        // Arrange
        var fileName = "alarms.csv";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("test"));
        
        var alarm = new Alarm(Guid.NewGuid(), new Name("Morning Alarm"), DateTime.Today.Add(TimeSpan.FromHours(7)), true, _userId);
        var command = new ImportAlarmsCommand(stream, fileName, _userId, false);

        _fileParserMock.Setup(x => x.IsFormatSupported(fileName)).Returns(true);
        _fileParserMock.Setup(x => x.ParseAsync(stream, fileName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Alarm> { alarm });
        _alarmRepositoryMock.Setup(x => x.GetByUserIdAsync(_userId))
            .ReturnsAsync(new List<Alarm>());
        _alarmRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Alarm>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalRecords.Should().Be(1);
        result.SuccessfulImports.Should().Be(0);
        result.FailedImports.Should().Be(1);
        result.Errors.Should().HaveCount(1);
        result.Errors.First().ErrorCode.Should().Be("PROCESSING_ERROR");
        result.Errors.First().ErrorType.Should().Be("Processing");
    }

    [Fact]
    public async Task Handle_WithEmptyFile_ShouldReturnEmptyResult()
    {
        // Arrange
        var fileName = "alarms.csv";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("test"));
        var command = new ImportAlarmsCommand(stream, fileName, _userId, false);

        _fileParserMock.Setup(x => x.IsFormatSupported(fileName)).Returns(true);
        _fileParserMock.Setup(x => x.ParseAsync(stream, fileName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Alarm>());
        _alarmRepositoryMock.Setup(x => x.GetByUserIdAsync(_userId))
            .ReturnsAsync(new List<Alarm>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalRecords.Should().Be(0);
        result.SuccessfulImports.Should().Be(0);
        result.FailedImports.Should().Be(0);
        result.UpdatedImports.Should().Be(0);
        result.Errors.Should().BeEmpty();
        result.ImportedAlarms.Should().BeEmpty();
        
        _alarmRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Alarm>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldLogProgress()
    {
        // Arrange
        var fileName = "alarms.csv";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("test"));
        var alarm = new Alarm(Guid.NewGuid(), new Name("Morning Alarm"), DateTime.Today.Add(TimeSpan.FromHours(7)), true, _userId);
        var command = new ImportAlarmsCommand(stream, fileName, _userId, false);

        _fileParserMock.Setup(x => x.IsFormatSupported(fileName)).Returns(true);
        _fileParserMock.Setup(x => x.ParseAsync(stream, fileName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Alarm> { alarm });
        _alarmRepositoryMock.Setup(x => x.GetByUserIdAsync(_userId))
            .ReturnsAsync(new List<Alarm>());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Iniciando importação")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Importação concluída")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
