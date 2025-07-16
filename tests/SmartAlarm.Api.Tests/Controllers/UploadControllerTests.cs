using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Infrastructure.Storage;
using Xunit;

namespace SmartAlarm.Api.Tests.Controllers
{
    public class UploadControllerTests
    {
        private readonly Mock<IStorageService> _storageServiceMock;
        private readonly Mock<ILogger<UploadController>> _loggerMock;
        private readonly UploadController _controller;

        public UploadControllerTests()
        {
            _storageServiceMock = new Mock<IStorageService>();
            _loggerMock = new Mock<ILogger<UploadController>>();
            _controller = new UploadController(_storageServiceMock.Object, _loggerMock.Object);
        }

        [Fact(DisplayName = "Upload de arquivo único deve retornar sucesso")]
        [Trait("Category", "Unit")]
        [Trait("Controller", "Upload")]
        public async Task UploadFile_WithValidFile_ShouldReturnOk()
        {
            // Arrange
            var content = "conteúdo do arquivo de teste";
            var fileName = "test-file.txt";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var formFile = new FormFile(stream, 0, stream.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/plain"
            };

            _storageServiceMock
                .Setup(x => x.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UploadFile(formFile);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<UploadResponse>(okResult.Value);
            Assert.Equal(fileName, response.FileName);
            Assert.True(response.Success);
            Assert.Contains("sucesso", response.Message.ToLower());

            _storageServiceMock.Verify(
                x => x.UploadAsync(It.Is<string>(path => path.Contains(fileName)), It.IsAny<Stream>()),
                Times.Once);
        }

        [Fact(DisplayName = "Upload sem arquivo deve retornar BadRequest")]
        [Trait("Category", "Unit")]
        [Trait("Controller", "Upload")]
        public async Task UploadFile_WithNullFile_ShouldReturnBadRequest()
        {
            // Act
            var result = await _controller.UploadFile(null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<UploadResponse>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Contains("arquivo", response.Message.ToLower());

            _storageServiceMock.Verify(
                x => x.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>()),
                Times.Never);
        }

        [Fact(DisplayName = "Upload de arquivo vazio deve retornar BadRequest")]
        [Trait("Category", "Unit")]
        [Trait("Controller", "Upload")]
        public async Task UploadFile_WithEmptyFile_ShouldReturnBadRequest()
        {
            // Arrange
            var fileName = "empty-file.txt";
            var stream = new MemoryStream();
            var formFile = new FormFile(stream, 0, 0, "file", fileName);

            // Act
            var result = await _controller.UploadFile(formFile);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<UploadResponse>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Contains("vazio", response.Message.ToLower());

            _storageServiceMock.Verify(
                x => x.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>()),
                Times.Never);
        }

        [Fact(DisplayName = "Upload com erro no storage deve retornar InternalServerError")]
        [Trait("Category", "Unit")]
        [Trait("Controller", "Upload")]
        public async Task UploadFile_WithStorageError_ShouldReturnInternalServerError()
        {
            // Arrange
            var content = "conteúdo do arquivo";
            var fileName = "test-file.txt";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var formFile = new FormFile(stream, 0, stream.Length, "file", fileName);

            _storageServiceMock
                .Setup(x => x.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>()))
                .ThrowsAsync(new Exception("Erro no storage"));

            // Act
            var result = await _controller.UploadFile(formFile);

            // Assert
            var errorResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, errorResult.StatusCode);
            var response = Assert.IsType<UploadResponse>(errorResult.Value);
            Assert.False(response.Success);
            Assert.Contains("erro", response.Message.ToLower());
        }

        [Theory(DisplayName = "Upload de diferentes tipos de arquivo deve funcionar")]
        [Trait("Category", "Unit")]
        [Trait("Controller", "Upload")]
        [InlineData("document.pdf", "application/pdf")]
        [InlineData("image.jpg", "image/jpeg")]
        [InlineData("data.csv", "text/csv")]
        [InlineData("config.json", "application/json")]
        [InlineData("archive.zip", "application/zip")]
        public async Task UploadFile_WithDifferentFileTypes_ShouldWork(string fileName, string contentType)
        {
            // Arrange
            var content = $"Conteúdo do arquivo {fileName}";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var formFile = new FormFile(stream, 0, stream.Length, "file", fileName)
            {
                ContentType = contentType
            };

            _storageServiceMock
                .Setup(x => x.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UploadFile(formFile);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<UploadResponse>(okResult.Value);
            Assert.Equal(fileName, response.FileName);
            Assert.True(response.Success);
        }

        [Fact(DisplayName = "Upload múltiplo deve processar todos os arquivos")]
        [Trait("Category", "Unit")]
        [Trait("Controller", "Upload")]
        public async Task UploadMultipleFiles_WithValidFiles_ShouldProcessAll()
        {
            // Arrange
            var files = new List<IFormFile>();
            for (int i = 0; i < 3; i++)
            {
                var content = $"Conteúdo do arquivo {i}";
                var fileName = $"file-{i}.txt";
                var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
                var formFile = new FormFile(stream, 0, stream.Length, "files", fileName);
                files.Add(formFile);
            }

            _storageServiceMock
                .Setup(x => x.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UploadMultipleFiles(files);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<MultipleUploadResponse>(okResult.Value);
            Assert.Equal(3, response.TotalFiles);
            Assert.Equal(3, response.SuccessfulUploads);
            Assert.Empty(response.Errors);

            _storageServiceMock.Verify(
                x => x.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>()),
                Times.Exactly(3));
        }

        [Fact(DisplayName = "Upload múltiplo com alguns erros deve reportar corretamente")]
        [Trait("Category", "Unit")]
        [Trait("Controller", "Upload")]
        public async Task UploadMultipleFiles_WithSomeErrors_ShouldReportCorrectly()
        {
            // Arrange
            var files = new List<IFormFile>();
            for (int i = 0; i < 3; i++)
            {
                var content = $"Conteúdo do arquivo {i}";
                var fileName = $"file-{i}.txt";
                var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
                var formFile = new FormFile(stream, 0, stream.Length, "files", fileName);
                files.Add(formFile);
            }

            // Configurar para o segundo arquivo falhar
            _storageServiceMock
                .SetupSequence(x => x.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>()))
                .Returns(Task.CompletedTask) // Primeiro arquivo: sucesso
                .ThrowsAsync(new Exception("Erro no segundo arquivo")) // Segundo arquivo: erro
                .Returns(Task.CompletedTask); // Terceiro arquivo: sucesso

            // Act
            var result = await _controller.UploadMultipleFiles(files);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<MultipleUploadResponse>(okResult.Value);
            Assert.Equal(3, response.TotalFiles);
            Assert.Equal(2, response.SuccessfulUploads);
            Assert.Single(response.Errors);
            Assert.Contains("file-1.txt", response.Errors.First());
        }

        [Fact(DisplayName = "Download de arquivo deve retornar stream correto")]
        [Trait("Category", "Unit")]
        [Trait("Controller", "Upload")]
        public async Task DownloadFile_WithValidPath_ShouldReturnFile()
        {
            // Arrange
            var filePath = "uploads/test-file.txt";
            var content = "Conteúdo do arquivo para download";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

            _storageServiceMock
                .Setup(x => x.DownloadAsync(filePath))
                .ReturnsAsync(stream);

            // Act
            var result = await _controller.DownloadFile(filePath);

            // Assert
            var fileResult = Assert.IsType<FileStreamResult>(result);
            Assert.Equal("application/octet-stream", fileResult.ContentType);
            Assert.Equal("test-file.txt", fileResult.FileDownloadName);

            _storageServiceMock.Verify(
                x => x.DownloadAsync(filePath),
                Times.Once);
        }

        [Fact(DisplayName = "Download de arquivo inexistente deve retornar NotFound")]
        [Trait("Category", "Unit")]
        [Trait("Controller", "Upload")]
        public async Task DownloadFile_WithNonExistentFile_ShouldReturnNotFound()
        {
            // Arrange
            var filePath = "uploads/non-existent.txt";

            _storageServiceMock
                .Setup(x => x.DownloadAsync(filePath))
                .ThrowsAsync(new FileNotFoundException());

            // Act
            var result = await _controller.DownloadFile(filePath);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact(DisplayName = "Delete de arquivo deve retornar sucesso")]
        [Trait("Category", "Unit")]
        [Trait("Controller", "Upload")]
        public async Task DeleteFile_WithValidPath_ShouldReturnOk()
        {
            // Arrange
            var filePath = "uploads/test-file.txt";

            _storageServiceMock
                .Setup(x => x.DeleteAsync(filePath))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteFile(filePath);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<DeleteResponse>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(filePath, response.FilePath);

            _storageServiceMock.Verify(
                x => x.DeleteAsync(filePath),
                Times.Once);
        }
    }

    // Classes de resposta para os testes
    public class UploadResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    public class MultipleUploadResponse
    {
        public int TotalFiles { get; set; }
        public int SuccessfulUploads { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<UploadResponse> Results { get; set; } = new();
    }

    public class DeleteResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime DeletedAt { get; set; }
    }

    // Mock do controller para os testes
    public class UploadController : ControllerBase
    {
        private readonly IStorageService _storageService;
        private readonly ILogger<UploadController> _logger;

        public UploadController(IStorageService storageService, ILogger<UploadController> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        public async Task<IActionResult> UploadFile(IFormFile? file)
        {
            try
            {
                if (file == null)
                {
                    return BadRequest(new UploadResponse
                    {
                        Success = false,
                        Message = "Nenhum arquivo foi fornecido"
                    });
                }

                if (file.Length == 0)
                {
                    return BadRequest(new UploadResponse
                    {
                        Success = false,
                        Message = "O arquivo está vazio"
                    });
                }

                var fileName = file.FileName;
                var filePath = $"uploads/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}-{fileName}";

                using var stream = file.OpenReadStream();
                await _storageService.UploadAsync(filePath, stream);

                _logger.LogInformation("Arquivo {FileName} enviado com sucesso para {FilePath}", fileName, filePath);

                return Ok(new UploadResponse
                {
                    Success = true,
                    Message = "Arquivo enviado com sucesso",
                    FileName = fileName,
                    FilePath = filePath,
                    FileSize = file.Length,
                    UploadedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fazer upload do arquivo");
                return StatusCode(500, new UploadResponse
                {
                    Success = false,
                    Message = "Erro interno do servidor ao processar upload"
                });
            }
        }

        public async Task<IActionResult> UploadMultipleFiles(IEnumerable<IFormFile> files)
        {
            var results = new List<UploadResponse>();
            var errors = new List<string>();
            var successCount = 0;

            foreach (var file in files)
            {
                try
                {
                    if (file.Length > 0)
                    {
                        var fileName = file.FileName;
                        var filePath = $"uploads/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}-{fileName}";

                        using var stream = file.OpenReadStream();
                        await _storageService.UploadAsync(filePath, stream);

                        results.Add(new UploadResponse
                        {
                            Success = true,
                            Message = "Sucesso",
                            FileName = fileName,
                            FilePath = filePath,
                            FileSize = file.Length,
                            UploadedAt = DateTime.UtcNow
                        });
                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Erro no arquivo {file.FileName}: {ex.Message}");
                    results.Add(new UploadResponse
                    {
                        Success = false,
                        Message = ex.Message,
                        FileName = file.FileName
                    });
                }
            }

            return Ok(new MultipleUploadResponse
            {
                TotalFiles = files.Count(),
                SuccessfulUploads = successCount,
                Errors = errors,
                Results = results
            });
        }

        public async Task<IActionResult> DownloadFile(string filePath)
        {
            try
            {
                var stream = await _storageService.DownloadAsync(filePath);
                var fileName = Path.GetFileName(filePath);
                return File(stream, "application/octet-stream", fileName);
            }
            catch (FileNotFoundException)
            {
                return NotFound(new { Message = "Arquivo não encontrado" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fazer download do arquivo {FilePath}", filePath);
                return StatusCode(500, new { Message = "Erro interno do servidor" });
            }
        }

        public async Task<IActionResult> DeleteFile(string filePath)
        {
            try
            {
                await _storageService.DeleteAsync(filePath);

                _logger.LogInformation("Arquivo {FilePath} deletado com sucesso", filePath);

                return Ok(new DeleteResponse
                {
                    Success = true,
                    Message = "Arquivo deletado com sucesso",
                    FilePath = filePath,
                    DeletedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deletar arquivo {FilePath}", filePath);
                return StatusCode(500, new DeleteResponse
                {
                    Success = false,
                    Message = "Erro interno do servidor ao deletar arquivo"
                });
            }
        }
    }
}
