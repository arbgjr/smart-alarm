using System.Text;
using Microsoft.Extensions.Logging;
using SmartAlarm.Infrastructure.Storage;
using Xunit;
using Moq;

namespace SmartAlarm.Infrastructure.Tests.Integration
{
    public class MinioStorageServiceIntegrationTests
    {
        private readonly MinioStorageService _service;
        private readonly ILogger<MinioStorageService> _logger;
        private readonly Mock<SmartAlarm.Infrastructure.Configuration.IConfigurationResolver> _configResolverMock;

        public MinioStorageServiceIntegrationTests()
        {
            // Usar DockerHelper para resolver configurações do MinIO
            var host = DockerHelper.ResolveServiceHostname("minio");
            var port = DockerHelper.ResolveServicePort("minio", 9000);
            
            Environment.SetEnvironmentVariable("MINIO_ENDPOINT", host);
            Environment.SetEnvironmentVariable("MINIO_PORT", port.ToString());
            
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<MinioStorageService>();
            _configResolverMock = new Mock<SmartAlarm.Infrastructure.Configuration.IConfigurationResolver>();
            _configResolverMock.Setup(x => x.GetConfigAsync("MINIO_ENDPOINT", It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(host);
            _configResolverMock.Setup(x => x.GetConfigAsync("MINIO_PORT", It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(port.ToString());
            _service = new MinioStorageService(_logger, _configResolverMock.Object);
        }

        [Fact(DisplayName = "Deve fazer upload, download e delete no MinIO")]
        [Trait("Category", "Integration")]
        public async Task Deve_Upload_Download_Delete_Arquivo()
        {
            // Arrange
            var conteudo = "conteudo de teste";
            var path = $"test-file-{Guid.NewGuid()}.txt";
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(conteudo));

            // Act
            await _service.UploadAsync(path, ms);
            var stream = await _service.DownloadAsync(path);
            using var reader = new StreamReader(stream);
            var lido = await reader.ReadToEndAsync();
            await _service.DeleteAsync(path);

            // Assert
            Assert.Equal(conteudo, lido);
        }
    }
}
