using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Api.Controllers;
using SmartAlarm.Application.Services.Interfaces;
using SmartAlarm.Domain.Interfaces;
using Xunit;

namespace SmartAlarm.Api.Tests.Controllers
{
    [Trait("Category", "Unit")]
    public class AlarmControllerUploadTests
    {
        private readonly Mock<IAlarmService> _alarmServiceMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<ILogger<AlarmController>> _loggerMock;
        private readonly AlarmController _controller;

        public AlarmControllerUploadTests()
        {
            _alarmServiceMock = new Mock<IAlarmService>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _loggerMock = new Mock<ILogger<AlarmController>>();
            
            _controller = new AlarmController(
                _alarmServiceMock.Object,
                _currentUserServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact(DisplayName = "ImportAlarms deve retornar BadRequest quando arquivo é nulo")]
        public async Task ImportAlarms_WithNullFile_ShouldReturnBadRequest()
        {
            // Arrange
            SetupAuthenticatedUser();

            // Act
            var result = await _controller.ImportAlarms(null, false);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact(DisplayName = "ImportAlarms deve retornar BadRequest quando arquivo está vazio")]
        public async Task ImportAlarms_WithEmptyFile_ShouldReturnBadRequest()
        {
            // Arrange
            SetupAuthenticatedUser();
            var emptyFile = CreateMockFormFile("test.csv", "");

            // Act
            var result = await _controller.ImportAlarms(emptyFile, false);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact(DisplayName = "ImportAlarms deve retornar Unauthorized para usuário não autenticado")]
        public async Task ImportAlarms_WithUnauthenticatedUser_ShouldReturnUnauthorized()
        {
            // Arrange
            _currentUserServiceMock.Setup(x => x.IsAuthenticated).Returns(false);
            var file = CreateMockFormFile("test.csv", "Name,Time\nTest Alarm,08:00");

            // Act
            var result = await _controller.ImportAlarms(file, false);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact(DisplayName = "ImportAlarms deve processar arquivo CSV válido")]
        public async Task ImportAlarms_WithValidCsvFile_ShouldProcessSuccessfully()
        {
            // Arrange
            SetupAuthenticatedUser();
            var csvContent = "Name,Time,Days\nMorning Alarm,08:00,Monday;Tuesday;Wednesday\nEvening Alarm,18:00,Friday";
            var file = CreateMockFormFile("alarms.csv", csvContent);

            var importResponse = new SmartAlarm.Application.DTOs.ImportAlarmsResponseDto
            {
                TotalProcessed = 2,
                SuccessCount = 2,
                FailureCount = 0,
                Errors = new List<string>()
            };

            _alarmServiceMock
                .Setup(x => x.ImportAlarmsAsync(It.IsAny<Guid>(), It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(importResponse);

            // Act
            var result = await _controller.ImportAlarms(file, false);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<SmartAlarm.Application.DTOs.ImportAlarmsResponseDto>(okResult.Value);
            Assert.Equal(2, response.SuccessCount);
            Assert.Equal(0, response.FailureCount);
        }

        [Theory(DisplayName = "ImportAlarms deve processar diferentes tipos de arquivo")]
        [InlineData("alarms.csv", "text/csv", "Name,Time\nTest,08:00")]
        [InlineData("alarms.txt", "text/plain", "Name,Time\nTest,08:00")]
        [InlineData("data.json", "application/json", "[{\"name\":\"Test\",\"time\":\"08:00\"}]")]
        public async Task ImportAlarms_WithDifferentFileTypes_ShouldProcess(string fileName, string contentType, string content)
        {
            // Arrange
            SetupAuthenticatedUser();
            var file = CreateMockFormFile(fileName, content, contentType);

            var importResponse = new SmartAlarm.Application.DTOs.ImportAlarmsResponseDto
            {
                TotalProcessed = 1,
                SuccessCount = 1,
                FailureCount = 0,
                Errors = new List<string>()
            };

            _alarmServiceMock
                .Setup(x => x.ImportAlarmsAsync(It.IsAny<Guid>(), It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(importResponse);

            // Act
            var result = await _controller.ImportAlarms(file, false);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact(DisplayName = "ImportAlarms deve lidar com arquivo grande")]
        public async Task ImportAlarms_WithLargeFile_ShouldProcess()
        {
            // Arrange
            SetupAuthenticatedUser();
            
            // Criar um arquivo "grande" com muitos alarmes
            var largeContent = new StringBuilder();
            largeContent.AppendLine("Name,Time,Days");
            for (int i = 1; i <= 1000; i++)
            {
                largeContent.AppendLine($"Alarm {i},{i % 24:D2}:00,Monday");
            }

            var file = CreateMockFormFile("large_alarms.csv", largeContent.ToString());

            var importResponse = new SmartAlarm.Application.DTOs.ImportAlarmsResponseDto
            {
                TotalProcessed = 1000,
                SuccessCount = 950,
                FailureCount = 50,
                Errors = new List<string> { "Some validation errors occurred" }
            };

            _alarmServiceMock
                .Setup(x => x.ImportAlarmsAsync(It.IsAny<Guid>(), It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(importResponse);

            // Act
            var result = await _controller.ImportAlarms(file, false);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<SmartAlarm.Application.DTOs.ImportAlarmsResponseDto>(okResult.Value);
            Assert.Equal(1000, response.TotalProcessed);
            Assert.Equal(950, response.SuccessCount);
            Assert.Equal(50, response.FailureCount);
        }

        [Fact(DisplayName = "ImportAlarms deve respeitar parâmetro overwriteExisting")]
        public async Task ImportAlarms_WithOverwriteExisting_ShouldPassParameter()
        {
            // Arrange
            SetupAuthenticatedUser();
            var file = CreateMockFormFile("test.csv", "Name,Time\nTest Alarm,08:00");

            var importResponse = new SmartAlarm.Application.DTOs.ImportAlarmsResponseDto
            {
                TotalProcessed = 1,
                SuccessCount = 1,
                FailureCount = 0,
                Errors = new List<string>()
            };

            _alarmServiceMock
                .Setup(x => x.ImportAlarmsAsync(It.IsAny<Guid>(), It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(importResponse);

            // Act
            var result = await _controller.ImportAlarms(file, overwriteExisting: true);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _alarmServiceMock.Verify(
                x => x.ImportAlarmsAsync(It.IsAny<Guid>(), It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact(DisplayName = "ImportAlarms deve lidar com exceção do serviço")]
        public async Task ImportAlarms_WithServiceException_ShouldReturnError()
        {
            // Arrange
            SetupAuthenticatedUser();
            var file = CreateMockFormFile("test.csv", "Name,Time\nTest Alarm,08:00");

            _alarmServiceMock
                .Setup(x => x.ImportAlarmsAsync(It.IsAny<Guid>(), It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Erro no processamento do arquivo"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _controller.ImportAlarms(file, false));
        }

        [Fact(DisplayName = "ImportAlarms deve aceitar cancellation token")]
        public async Task ImportAlarms_WithCancellationToken_ShouldPassToService()
        {
            // Arrange
            SetupAuthenticatedUser();
            var file = CreateMockFormFile("test.csv", "Name,Time\nTest Alarm,08:00");
            var cancellationToken = new CancellationToken();

            var importResponse = new SmartAlarm.Application.DTOs.ImportAlarmsResponseDto
            {
                TotalProcessed = 1,
                SuccessCount = 1,
                FailureCount = 0,
                Errors = new List<string>()
            };

            _alarmServiceMock
                .Setup(x => x.ImportAlarmsAsync(It.IsAny<Guid>(), It.IsAny<Stream>(), It.IsAny<bool>(), cancellationToken))
                .ReturnsAsync(importResponse);

            // Act
            var result = await _controller.ImportAlarms(file, false, cancellationToken);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _alarmServiceMock.Verify(
                x => x.ImportAlarmsAsync(It.IsAny<Guid>(), It.IsAny<Stream>(), It.IsAny<bool>(), cancellationToken),
                Times.Once);
        }

        private void SetupAuthenticatedUser()
        {
            _currentUserServiceMock.Setup(x => x.IsAuthenticated).Returns(true);
            _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        }

        private static IFormFile CreateMockFormFile(string fileName, string content, string contentType = "text/csv")
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            var stream = new MemoryStream(bytes);
            
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.Length).Returns(bytes.Length);
            mockFile.Setup(f => f.ContentType).Returns(contentType);
            mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                   .Returns((Stream target, CancellationToken token) =>
                   {
                       stream.Position = 0;
                       return stream.CopyToAsync(target, token);
                   });
            
            return mockFile.Object;
        }
    }
}
