using System.Text;
using Microsoft.Extensions.Logging;
using SmartAlarm.Infrastructure.Storage;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;
using Xunit;
using Moq;

namespace SmartAlarm.Infrastructure.Tests.Integration
{
    [Trait("Category", "Integration")]
    public class MinioStorageServiceIntegrationTests : IAsyncLifetime
    {
        private readonly MinioStorageService _service;
        private readonly ILogger<MinioStorageService> _logger;
        private readonly Mock<SmartAlarm.Infrastructure.Configuration.IConfigurationResolver> _configResolverMock;
        private readonly List<string> _uploadedFiles = new();

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
            
            // Create mock dependencies for observability
            var meterMock = new Mock<SmartAlarmMeter>();
            var correlationContextMock = new Mock<ICorrelationContext>();
            var activitySourceMock = new Mock<SmartAlarmActivitySource>();
            
            _service = new MinioStorageService(_logger, _configResolverMock.Object, meterMock.Object, correlationContextMock.Object, activitySourceMock.Object);
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            // Limpar todos os arquivos criados durante os testes
            foreach (var file in _uploadedFiles)
            {
                try
                {
                    await _service.DeleteAsync(file);
                }
                catch
                {
                    // Ignorar erros ao limpar
                }
            }
        }

        [Fact(DisplayName = "Deve fazer upload, download e delete no MinIO")]
        public async Task Deve_Upload_Download_Delete_Arquivo()
        {
            // Arrange
            var conteudo = "conteudo de teste";
            var path = $"test-file-{Guid.NewGuid()}.txt";
            _uploadedFiles.Add(path);
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

        [Fact(DisplayName = "Deve fazer upload de arquivo grande")]
        public async Task Deve_Upload_Arquivo_Grande()
        {
            // Arrange
            var tamanhoArquivo = 1024 * 1024; // 1MB
            var conteudo = new string('A', tamanhoArquivo);
            var path = $"large-file-{Guid.NewGuid()}.txt";
            _uploadedFiles.Add(path);
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(conteudo));

            // Act
            await _service.UploadAsync(path, ms);
            var stream = await _service.DownloadAsync(path);
            using var reader = new StreamReader(stream);
            var lido = await reader.ReadToEndAsync();

            // Assert
            Assert.Equal(conteudo.Length, lido.Length);
            Assert.Equal(conteudo, lido);
        }

        [Theory(DisplayName = "Deve fazer upload de diferentes tipos de arquivo")]
        [InlineData("document.pdf", "PDF content")]
        [InlineData("image.jpg", "JPEG image data")]
        [InlineData("data.csv", "Name,Age\nJohn,30\nJane,25")]
        [InlineData("config.json", "{\"key\": \"value\", \"number\": 42}")]
        [InlineData("script.sh", "#!/bin/bash\necho 'Hello World'")]
        public async Task Deve_Upload_Diferentes_Tipos_Arquivo(string fileName, string content)
        {
            // Arrange
            var path = $"uploads/{Guid.NewGuid()}-{fileName}";
            _uploadedFiles.Add(path);
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));

            // Act
            await _service.UploadAsync(path, ms);
            var stream = await _service.DownloadAsync(path);
            using var reader = new StreamReader(stream);
            var lido = await reader.ReadToEndAsync();

            // Assert
            Assert.Equal(content, lido);
        }

        [Fact(DisplayName = "Deve fazer upload de arquivo vazio")]
        public async Task Deve_Upload_Arquivo_Vazio()
        {
            // Arrange
            var path = $"empty-file-{Guid.NewGuid()}.txt";
            _uploadedFiles.Add(path);
            using var ms = new MemoryStream();

            // Act
            await _service.UploadAsync(path, ms);
            var stream = await _service.DownloadAsync(path);
            using var reader = new StreamReader(stream);
            var lido = await reader.ReadToEndAsync();

            // Assert
            Assert.Empty(lido);
        }

        [Fact(DisplayName = "Deve fazer upload em diretórios aninhados")]
        public async Task Deve_Upload_Diretorios_Aninhados()
        {
            // Arrange
            var conteudo = "conteudo em diretório aninhado";
            var path = $"nivel1/nivel2/nivel3/arquivo-{Guid.NewGuid()}.txt";
            _uploadedFiles.Add(path);
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(conteudo));

            // Act
            await _service.UploadAsync(path, ms);
            var stream = await _service.DownloadAsync(path);
            using var reader = new StreamReader(stream);
            var lido = await reader.ReadToEndAsync();

            // Assert
            Assert.Equal(conteudo, lido);
        }

        [Fact(DisplayName = "Deve sobrescrever arquivo existente")]
        public async Task Deve_Sobrescrever_Arquivo_Existente()
        {
            // Arrange
            var path = $"overwrite-file-{Guid.NewGuid()}.txt";
            _uploadedFiles.Add(path);
            var conteudoOriginal = "conteúdo original";
            var conteudoNovo = "conteúdo atualizado";

            // Act
            // Upload inicial
            using (var ms1 = new MemoryStream(Encoding.UTF8.GetBytes(conteudoOriginal)))
            {
                await _service.UploadAsync(path, ms1);
            }

            // Sobrescrever
            using (var ms2 = new MemoryStream(Encoding.UTF8.GetBytes(conteudoNovo)))
            {
                await _service.UploadAsync(path, ms2);
            }

            // Verificar conteúdo final
            var stream = await _service.DownloadAsync(path);
            using var reader = new StreamReader(stream);
            var lido = await reader.ReadToEndAsync();

            // Assert
            Assert.Equal(conteudoNovo, lido);
        }

        [Fact(DisplayName = "Deve fazer upload múltiplo simultâneo")]
        public async Task Deve_Upload_Multiplo_Simultaneo()
        {
            // Arrange
            var numeroArquivos = 5;
            var tasks = new List<Task>();
            var arquivos = new List<string>();

            // Act
            for (int i = 0; i < numeroArquivos; i++)
            {
                var path = $"concurrent-file-{i}-{Guid.NewGuid()}.txt";
                var conteudo = $"Conteúdo do arquivo {i}";
                arquivos.Add(path);
                _uploadedFiles.Add(path);

                var task = Task.Run(async () =>
                {
                    using var ms = new MemoryStream(Encoding.UTF8.GetBytes(conteudo));
                    await _service.UploadAsync(path, ms);
                });
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            // Assert - Verificar se todos os arquivos foram criados
            foreach (var arquivo in arquivos)
            {
                var stream = await _service.DownloadAsync(arquivo);
                using var reader = new StreamReader(stream);
                var lido = await reader.ReadToEndAsync();
                Assert.Contains("Conteúdo do arquivo", lido);
            }
        }

        [Fact(DisplayName = "Deve fazer upload com caracteres especiais no nome")]
        public async Task Deve_Upload_Caracteres_Especiais()
        {
            // Arrange
            var conteudo = "Arquivo com acentos: ção, ã, õ, ê";
            var path = $"special-chars/arquivo-àáâãäåæçèéêë-{Guid.NewGuid()}.txt";
            _uploadedFiles.Add(path);
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(conteudo));

            // Act
            await _service.UploadAsync(path, ms);
            var stream = await _service.DownloadAsync(path);
            using var reader = new StreamReader(stream);
            var lido = await reader.ReadToEndAsync();

            // Assert
            Assert.Equal(conteudo, lido);
        }
    }
}
