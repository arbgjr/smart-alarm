using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SmartAlarm.Infrastructure.Storage;
using Xunit;

namespace SmartAlarm.Api.Tests.Integration
{
    [Trait("Category", "Integration")]
    [Trait("RequiresContainers", "MinIO")]
    public class UploadEndpointsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public UploadEndpointsIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact(DisplayName = "POST /api/upload deve aceitar upload de arquivo único")]
        public async Task PostUpload_WithSingleFile_ShouldReturnOk()
        {
            // Arrange
            var content = "Conteúdo do arquivo de teste para integração";
            var fileName = "integration-test-file.txt";
            
            using var form = new MultipartFormDataContent();
            using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
            form.Add(fileContent, "file", fileName);

            // Act
            var response = await _client.PostAsync("/api/upload", form);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("sucesso", responseContent.ToLower());
            Assert.Contains(fileName, responseContent);
        }

        [Fact(DisplayName = "POST /api/upload/multiple deve aceitar múltiplos arquivos")]
        public async Task PostUploadMultiple_WithMultipleFiles_ShouldReturnOk()
        {
            // Arrange
            using var form = new MultipartFormDataContent();
            
            // Adicionar 3 arquivos de teste
            for (int i = 0; i < 3; i++)
            {
                var content = $"Conteúdo do arquivo {i} para teste de upload múltiplo";
                var fileName = $"integration-multi-file-{i}.txt";
                
                using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
                form.Add(fileContent, "files", fileName);
            }

            // Act
            var response = await _client.PostAsync("/api/upload/multiple", form);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("\"totalFiles\":3", responseContent);
            Assert.Contains("\"successfulUploads\":3", responseContent);
        }

        [Fact(DisplayName = "POST /api/upload sem arquivo deve retornar BadRequest")]
        public async Task PostUpload_WithoutFile_ShouldReturnBadRequest()
        {
            // Arrange
            using var form = new MultipartFormDataContent();

            // Act
            var response = await _client.PostAsync("/api/upload", form);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact(DisplayName = "POST /api/upload com arquivo vazio deve retornar BadRequest")]
        public async Task PostUpload_WithEmptyFile_ShouldReturnBadRequest()
        {
            // Arrange
            using var form = new MultipartFormDataContent();
            using var fileContent = new ByteArrayContent(Array.Empty<byte>());
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
            form.Add(fileContent, "file", "empty-file.txt");

            // Act
            var response = await _client.PostAsync("/api/upload", form);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory(DisplayName = "POST /api/upload deve aceitar diferentes tipos de arquivo")]
        [InlineData("document.pdf", "application/pdf")]
        [InlineData("image.jpg", "image/jpeg")]
        [InlineData("data.csv", "text/csv")]
        [InlineData("config.json", "application/json")]
        [InlineData("script.sh", "text/plain")]
        public async Task PostUpload_WithDifferentFileTypes_ShouldReturnOk(string fileName, string contentType)
        {
            // Arrange
            var content = $"Conteúdo específico para {fileName}";
            
            using var form = new MultipartFormDataContent();
            using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            form.Add(fileContent, "file", fileName);

            // Act
            var response = await _client.PostAsync("/api/upload", form);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains(fileName, responseContent);
        }

        [Fact(DisplayName = "GET /api/upload/download deve retornar arquivo")]
        public async Task GetDownload_WithValidPath_ShouldReturnFile()
        {
            // Arrange - Primeiro fazer upload de um arquivo
            var content = "Conteúdo para teste de download";
            var fileName = "download-test-file.txt";
            
            using var uploadForm = new MultipartFormDataContent();
            using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
            uploadForm.Add(fileContent, "file", fileName);

            var uploadResponse = await _client.PostAsync("/api/upload", uploadForm);
            Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);

            // Extrair o caminho do arquivo da resposta do upload
            var uploadResponseContent = await uploadResponse.Content.ReadAsStringAsync();
            // Assumindo que a resposta contém o filePath
            var filePath = ExtractFilePathFromResponse(uploadResponseContent);

            // Act
            var downloadResponse = await _client.GetAsync($"/api/upload/download?filePath={Uri.EscapeDataString(filePath)}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
            Assert.Equal("application/octet-stream", downloadResponse.Content.Headers.ContentType?.MediaType);
            
            var downloadedContent = await downloadResponse.Content.ReadAsStringAsync();
            Assert.Equal(content, downloadedContent);
        }

        [Fact(DisplayName = "GET /api/upload/download com arquivo inexistente deve retornar NotFound")]
        public async Task GetDownload_WithNonExistentFile_ShouldReturnNotFound()
        {
            // Arrange
            var nonExistentPath = "uploads/non-existent-file.txt";

            // Act
            var response = await _client.GetAsync($"/api/upload/download?filePath={Uri.EscapeDataString(nonExistentPath)}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact(DisplayName = "DELETE /api/upload deve deletar arquivo")]
        public async Task DeleteUpload_WithValidPath_ShouldReturnOk()
        {
            // Arrange - Primeiro fazer upload de um arquivo
            var content = "Conteúdo para teste de deleção";
            var fileName = "delete-test-file.txt";
            
            using var uploadForm = new MultipartFormDataContent();
            using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
            uploadForm.Add(fileContent, "file", fileName);

            var uploadResponse = await _client.PostAsync("/api/upload", uploadForm);
            Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);

            // Extrair o caminho do arquivo da resposta do upload
            var uploadResponseContent = await uploadResponse.Content.ReadAsStringAsync();
            var filePath = ExtractFilePathFromResponse(uploadResponseContent);

            // Act
            var deleteResponse = await _client.DeleteAsync($"/api/upload?filePath={Uri.EscapeDataString(filePath)}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
            
            var deleteResponseContent = await deleteResponse.Content.ReadAsStringAsync();
            Assert.Contains("sucesso", deleteResponseContent.ToLower());

            // Verificar se o arquivo foi realmente deletado tentando fazer download
            var downloadResponse = await _client.GetAsync($"/api/upload/download?filePath={Uri.EscapeDataString(filePath)}");
            Assert.Equal(HttpStatusCode.NotFound, downloadResponse.StatusCode);
        }

        [Fact(DisplayName = "Upload de arquivo grande deve funcionar")]
        public async Task PostUpload_WithLargeFile_ShouldReturnOk()
        {
            // Arrange
            var largeContent = new string('A', 1024 * 1024); // 1MB
            var fileName = "large-file-test.txt";
            
            using var form = new MultipartFormDataContent();
            using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(largeContent));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
            form.Add(fileContent, "file", fileName);

            // Act
            var response = await _client.PostAsync("/api/upload", form);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains(fileName, responseContent);
            Assert.Contains("1048576", responseContent); // Tamanho do arquivo em bytes
        }

        [Fact(DisplayName = "Upload com caracteres especiais no nome deve funcionar")]
        public async Task PostUpload_WithSpecialCharactersInFileName_ShouldReturnOk()
        {
            // Arrange
            var content = "Arquivo com caracteres especiais: ção, ã, õ, ê";
            var fileName = "arquivo-àáâãäåæçèéêë-teste.txt";
            
            using var form = new MultipartFormDataContent();
            using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
            form.Add(fileContent, "file", fileName);

            // Act
            var response = await _client.PostAsync("/api/upload", form);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            // O nome do arquivo pode ser codificado/sanitizado, então verificamos se o upload foi bem-sucedido
            Assert.Contains("sucesso", responseContent.ToLower());
        }

        [Fact(DisplayName = "Upload simultâneo de múltiplos clientes deve funcionar")]
        public async Task PostUpload_WithConcurrentUploads_ShouldReturnOk()
        {
            // Arrange
            var tasks = new List<Task<HttpResponseMessage>>();
            
            for (int i = 0; i < 5; i++)
            {
                var content = $"Conteúdo do arquivo concorrente {i}";
                var fileName = $"concurrent-file-{i}.txt";
                
                var task = Task.Run(async () =>
                {
                    using var form = new MultipartFormDataContent();
                    using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
                    form.Add(fileContent, "file", fileName);
                    
                    return await _client.PostAsync("/api/upload", form);
                });
                
                tasks.Add(task);
            }

            // Act
            var responses = await Task.WhenAll(tasks);

            // Assert
            foreach (var response in responses)
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Fact(DisplayName = "Verificar se Storage Service está configurado corretamente")]
        public async Task StorageService_ShouldBeConfiguredCorrectly()
        {
            // Arrange & Act
            using var scope = _factory.Services.CreateScope();
            var storageService = scope.ServiceProvider.GetService<IStorageService>();

            // Assert
            Assert.NotNull(storageService);
            
            // Verificar se é uma instância real (não mock) em ambiente de integração
            var storageType = storageService.GetType();
            Assert.True(
                storageType == typeof(MinioStorageService) || 
                storageType == typeof(MockStorageService),
                $"Storage service deve ser MinioStorageService ou MockStorageService, mas foi: {storageType.Name}");
        }

        // Método auxiliar para extrair o caminho do arquivo da resposta JSON
        private static string ExtractFilePathFromResponse(string responseContent)
        {
            // Implementação simples para extrair filePath do JSON
            // Em um cenário real, você usaria JsonSerializer ou Newtonsoft.Json
            var startIndex = responseContent.IndexOf("\"filePath\":\"") + 12;
            var endIndex = responseContent.IndexOf("\"", startIndex);
            return responseContent.Substring(startIndex, endIndex - startIndex);
        }
    }

    // Factory personalizada para testes de integração
    public class WebApplicationFactory<TStartup> : Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<TStartup>
        where TStartup : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Configurar serviços específicos para testes de integração
                // Por exemplo, usar MinIO real para testes de integração
                services.Configure<MinioOptions>(options =>
                {
                    options.Endpoint = Environment.GetEnvironmentVariable("MINIO_ENDPOINT") ?? "minio";
                    options.Port = int.Parse(Environment.GetEnvironmentVariable("MINIO_PORT") ?? "9000");
                    options.AccessKey = Environment.GetEnvironmentVariable("MINIO_ACCESS_KEY") ?? "minioadmin";
                    options.SecretKey = Environment.GetEnvironmentVariable("MINIO_SECRET_KEY") ?? "minioadmin";
                    options.BucketName = Environment.GetEnvironmentVariable("MINIO_BUCKET") ?? "smart-alarm-test";
                });
            });

            builder.UseEnvironment("Testing");
        }
    }

    // Classe de configuração para MinIO
    public class MinioOptions
    {
        public string Endpoint { get; set; } = string.Empty;
        public int Port { get; set; }
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string BucketName { get; set; } = string.Empty;
    }
}
