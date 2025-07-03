using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Api.Services;
using Xunit;

namespace SmartAlarm.Tests.Services
{
    /// <summary>
    /// Testes para o serviço de mensagens de erro.
    /// </summary>
    public class ErrorMessageServiceTests
    {
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;
        private readonly Mock<ILogger<ErrorMessageService>> _mockLogger;
        private readonly string _testDirectory;

        public ErrorMessageServiceTests()
        {
            _mockEnvironment = new Mock<IWebHostEnvironment>();
            _mockLogger = new Mock<ILogger<ErrorMessageService>>();
            _testDirectory = Path.Combine(Path.GetTempPath(), "test_error_messages");
            Directory.CreateDirectory(_testDirectory);
            _mockEnvironment.Setup(x => x.ContentRootPath).Returns(_testDirectory);
        }

        [Fact]
        public void GetMessage_ShouldReturnCorrectMessage_WhenKeyExists()
        {
            // Arrange
            var testMessages = @"{
                ""Validation"": {
                    ""Required"": {
                        ""AlarmName"": ""Nome do alarme é obrigatório.""
                    }
                }
            }";

            var resourcesDir = Path.Combine(_testDirectory, "Resources");
            Directory.CreateDirectory(resourcesDir);
            File.WriteAllText(Path.Combine(resourcesDir, "ErrorMessages.json"), testMessages);

            var service = new ErrorMessageService(_mockEnvironment.Object, _mockLogger.Object);

            // Act
            var result = service.GetMessage("Validation.Required.AlarmName");

            // Assert
            Assert.Equal("Nome do alarme é obrigatório.", result);
        }

        [Fact]
        public void GetMessage_ShouldReturnKeyPath_WhenKeyDoesNotExist()
        {
            // Arrange
            var testMessages = @"{
                ""Validation"": {
                    ""Required"": {
                        ""AlarmName"": ""Nome do alarme é obrigatório.""
                    }
                }
            }";

            var resourcesDir = Path.Combine(_testDirectory, "Resources");
            Directory.CreateDirectory(resourcesDir);
            File.WriteAllText(Path.Combine(resourcesDir, "ErrorMessages.json"), testMessages);

            var service = new ErrorMessageService(_mockEnvironment.Object, _mockLogger.Object);

            // Act
            var result = service.GetMessage("Validation.Required.NonExistent");

            // Assert
            Assert.Equal("Erro: Validation.Required.NonExistent", result);
        }

        [Fact]
        public void GetMessage_ShouldFormatMessageWithParameters()
        {
            // Arrange
            var testMessages = @"{
                ""Validation"": {
                    ""Length"": {
                        ""MaxLength"": ""Campo deve ter até {MaxLength} caracteres.""
                    }
                }
            }";

            var resourcesDir = Path.Combine(_testDirectory, "Resources");
            Directory.CreateDirectory(resourcesDir);
            File.WriteAllText(Path.Combine(resourcesDir, "ErrorMessages.json"), testMessages);

            var service = new ErrorMessageService(_mockEnvironment.Object, _mockLogger.Object);

            // Act
            var result = service.GetMessage("Validation.Length.MaxLength", 100);

            // Assert
            Assert.Equal("Campo deve ter até 100 caracteres.", result);
        }

        [Fact]
        public void HasMessage_ShouldReturnTrue_WhenKeyExists()
        {
            // Arrange
            var testMessages = @"{
                ""Validation"": {
                    ""Required"": {
                        ""AlarmName"": ""Nome do alarme é obrigatório.""
                    }
                }
            }";

            var resourcesDir = Path.Combine(_testDirectory, "Resources");
            Directory.CreateDirectory(resourcesDir);
            File.WriteAllText(Path.Combine(resourcesDir, "ErrorMessages.json"), testMessages);

            var service = new ErrorMessageService(_mockEnvironment.Object, _mockLogger.Object);

            // Act
            var result = service.HasMessage("Validation.Required.AlarmName");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasMessage_ShouldReturnFalse_WhenKeyDoesNotExist()
        {
            // Arrange
            var testMessages = @"{
                ""Validation"": {
                    ""Required"": {
                        ""AlarmName"": ""Nome do alarme é obrigatório.""
                    }
                }
            }";

            var resourcesDir = Path.Combine(_testDirectory, "Resources");
            Directory.CreateDirectory(resourcesDir);
            File.WriteAllText(Path.Combine(resourcesDir, "ErrorMessages.json"), testMessages);

            var service = new ErrorMessageService(_mockEnvironment.Object, _mockLogger.Object);

            // Act
            var result = service.HasMessage("Validation.Required.NonExistent");

            // Assert
            Assert.False(result);
        }
    }
}