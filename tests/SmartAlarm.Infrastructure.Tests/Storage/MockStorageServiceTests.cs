// Mock utilizado exclusivamente para testes automatizados.
// Não representa lógica de produção.
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Infrastructure.Storage;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests.Storage
{
    public class MockStorageServiceTests
    {
        private readonly Mock<ILogger<MockStorageService>> _loggerMock;
        private readonly MockStorageService _service;

        public MockStorageServiceTests()
        {
            _loggerMock = new Mock<ILogger<MockStorageService>>();
            _service = new MockStorageService(_loggerMock.Object);
        }

        [Fact(DisplayName = "Upload deve logar informação corretamente")]
        [Trait("Category", "Unit")]
        public async Task UploadAsync_Should_LogInformation()
        {
            // AAA: Arrange, Act, Assert - padrão obrigatório para todos os testes.
            // Arrange
            var testPath = "/test.txt";
            using var testStream = new MemoryStream();

            // Act
            await _service.UploadAsync(testPath, testStream);

            // Assert
            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(testPath)),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact(DisplayName = "Upload com conteúdo deve funcionar sem erro")]
        [Trait("Category", "Unit")]
        public async Task UploadAsync_WithContent_Should_NotThrow()
        {
            // Arrange
            var testPath = "/documents/test-file.txt";
            var content = "Conteúdo de teste para upload";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

            // Act & Assert
            var exception = await Record.ExceptionAsync(() => _service.UploadAsync(testPath, stream));
            Assert.Null(exception);
        }

        [Fact(DisplayName = "Download deve logar informação corretamente")]
        [Trait("Category", "Unit")]
        public async Task DownloadAsync_Should_LogInformation()
        {
            // Arrange
            var testPath = "/download/test.txt";

            // Act
            using var result = await _service.DownloadAsync(testPath);

            // Assert
            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(testPath)),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            Assert.NotNull(result);
        }

        [Fact(DisplayName = "Delete deve logar informação corretamente")]
        [Trait("Category", "Unit")]
        public async Task DeleteAsync_Should_LogInformation()
        {
            // Arrange
            var testPath = "/delete/test.txt";

            // Act
            await _service.DeleteAsync(testPath);

            // Assert
            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(testPath)),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Theory(DisplayName = "Upload com diferentes tipos de arquivo deve funcionar")]
        [Trait("Category", "Unit")]
        [InlineData("/uploads/document.pdf")]
        [InlineData("/uploads/image.jpg")]
        [InlineData("/uploads/data.csv")]
        [InlineData("/uploads/config.json")]
        [InlineData("/uploads/archive.zip")]
        public async Task UploadAsync_WithDifferentFileTypes_Should_Work(string filePath)
        {
            // Arrange
            var testContent = $"Conteúdo de teste para {filePath}";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(testContent));

            // Act & Assert
            var exception = await Record.ExceptionAsync(() => _service.UploadAsync(filePath, stream));
            Assert.Null(exception);

            // Verificar se o log foi chamado
            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(filePath)),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact(DisplayName = "Upload com stream vazio deve funcionar")]
        [Trait("Category", "Unit")]
        public async Task UploadAsync_WithEmptyStream_Should_Work()
        {
            // Arrange
            var testPath = "/empty/file.txt";
            using var emptyStream = new MemoryStream();

            // Act & Assert
            var exception = await Record.ExceptionAsync(() => _service.UploadAsync(testPath, emptyStream));
            Assert.Null(exception);
        }

        [Fact(DisplayName = "Operações sequenciais devem funcionar corretamente")]
        [Trait("Category", "Unit")]
        public async Task Sequential_Operations_Should_Work()
        {
            // Arrange
            var testPath = "/sequential/test.txt";
            var content = "Teste de operações sequenciais";
            using var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes(content));

            // Act
            // 1. Upload
            await _service.UploadAsync(testPath, uploadStream);
            
            // 2. Download
            using var downloadStream = await _service.DownloadAsync(testPath);
            
            // 3. Delete
            await _service.DeleteAsync(testPath);

            // Assert
            Assert.NotNull(downloadStream);
            
            // Verificar se todos os logs foram chamados
            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(testPath)),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Exactly(3)); // Upload + Download + Delete
        }
    }
}

