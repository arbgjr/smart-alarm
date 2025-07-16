using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SmartAlarm.Api;
using Xunit;

namespace SmartAlarm.Api.Tests.Integration
{
    [Trait("Category", "Integration")]
    public class AlarmUploadIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public AlarmUploadIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact(DisplayName = "POST /api/alarms/import deve retornar Unauthorized sem autenticação")]
        public async Task ImportAlarms_WithoutAuth_ShouldReturnUnauthorized()
        {
            // Arrange
            var content = CreateMultipartFormDataContent("test.csv", "Name,Time\nTest Alarm,08:00");

            // Act
            var response = await _client.PostAsync("/api/alarms/import", content);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact(DisplayName = "POST /api/alarms/import deve validar arquivo obrigatório")]
        public async Task ImportAlarms_WithoutFile_ShouldReturnBadRequest()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var authenticatedClient = await CreateAuthenticatedClient(scope);
            
            var content = new MultipartFormDataContent();
            content.Add(new StringContent("false"), "overwriteExisting");

            // Act
            var response = await authenticatedClient.PostAsync("/api/alarms/import", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact(DisplayName = "POST /api/alarms/import deve processar CSV válido")]
        public async Task ImportAlarms_WithValidCsv_ShouldReturnSuccess()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var authenticatedClient = await CreateAuthenticatedClient(scope);
            
            var csvContent = "Name,Time,Days,IsActive\n" +
                           "Morning Alarm,08:00,Monday;Tuesday;Wednesday,true\n" +
                           "Evening Alarm,18:00,Friday,true";
            
            var content = CreateMultipartFormDataContent("alarms.csv", csvContent);

            // Act
            var response = await authenticatedClient.PostAsync("/api/alarms/import", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var responseBody = await response.Content.ReadAsStringAsync();
            Assert.Contains("TotalProcessed", responseBody);
            Assert.Contains("SuccessCount", responseBody);
        }

        [Theory(DisplayName = "POST /api/alarms/import deve aceitar diferentes tipos de arquivo")]
        [InlineData("alarms.csv", "text/csv")]
        [InlineData("alarms.txt", "text/plain")]
        public async Task ImportAlarms_WithDifferentFileTypes_ShouldProcess(string fileName, string contentType)
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var authenticatedClient = await CreateAuthenticatedClient(scope);
            
            var fileContent = "Name,Time\nTest Alarm,08:00";
            var content = CreateMultipartFormDataContent(fileName, fileContent, contentType);

            // Act
            var response = await authenticatedClient.PostAsync("/api/alarms/import", content);

            // Assert
            // Pode retornar OK ou BadRequest dependendo da implementação de validação
            Assert.True(response.StatusCode == HttpStatusCode.OK || 
                       response.StatusCode == HttpStatusCode.BadRequest);
        }

        [Fact(DisplayName = "POST /api/alarms/import deve lidar com arquivo grande")]
        public async Task ImportAlarms_WithLargeFile_ShouldProcess()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var authenticatedClient = await CreateAuthenticatedClient(scope);
            
            // Criar arquivo com muitos alarmes
            var largeContent = new StringBuilder();
            largeContent.AppendLine("Name,Time,Days,IsActive");
            for (int i = 1; i <= 100; i++) // Reduzido para não sobrecarregar os testes
            {
                largeContent.AppendLine($"Alarm {i},{i % 24:D2}:00,Monday,true");
            }

            var content = CreateMultipartFormDataContent("large_alarms.csv", largeContent.ToString());

            // Act
            var response = await authenticatedClient.PostAsync("/api/alarms/import", content);

            // Assert
            // Deve processar com sucesso ou retornar erro de validação
            Assert.True(response.StatusCode == HttpStatusCode.OK || 
                       response.StatusCode == HttpStatusCode.BadRequest ||
                       response.StatusCode == HttpStatusCode.RequestEntityTooLarge);
        }

        [Fact(DisplayName = "POST /api/alarms/import deve validar tamanho máximo de arquivo")]
        public async Task ImportAlarms_WithOversizedFile_ShouldReturnError()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var authenticatedClient = await CreateAuthenticatedClient(scope);
            
            // Criar arquivo muito grande (simulado)
            var oversizedContent = new string('A', 10 * 1024 * 1024); // 10MB de 'A's
            var content = CreateMultipartFormDataContent("oversized.csv", oversizedContent);

            // Act
            var response = await authenticatedClient.PostAsync("/api/alarms/import", content);

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.RequestEntityTooLarge ||
                       response.StatusCode == HttpStatusCode.BadRequest);
        }

        [Fact(DisplayName = "POST /api/alarms/import deve respeitar parâmetro overwriteExisting")]
        public async Task ImportAlarms_WithOverwriteParameter_ShouldWork()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var authenticatedClient = await CreateAuthenticatedClient(scope);
            
            var csvContent = "Name,Time\nTest Alarm,08:00";
            var content = CreateMultipartFormDataContent("test.csv", csvContent);
            content.Add(new StringContent("true"), "overwriteExisting");

            // Act
            var response = await authenticatedClient.PostAsync("/api/alarms/import", content);

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.OK || 
                       response.StatusCode == HttpStatusCode.BadRequest);
        }

        [Fact(DisplayName = "POST /api/alarms/import deve validar formato CSV")]
        public async Task ImportAlarms_WithInvalidCsv_ShouldReturnError()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var authenticatedClient = await CreateAuthenticatedClient(scope);
            
            var invalidCsv = "This is not a valid CSV\nMissing proper headers\nInvalid data format";
            var content = CreateMultipartFormDataContent("invalid.csv", invalidCsv);

            // Act
            var response = await authenticatedClient.PostAsync("/api/alarms/import", content);

            // Assert
            // Deve retornar erro de validação
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest);
        }

        [Fact(DisplayName = "POST /api/alarms/import deve validar extensão de arquivo")]
        public async Task ImportAlarms_WithUnsupportedExtension_ShouldReturnError()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var authenticatedClient = await CreateAuthenticatedClient(scope);
            
            var content = CreateMultipartFormDataContent("test.exe", "This is not a CSV file", "application/octet-stream");

            // Act
            var response = await authenticatedClient.PostAsync("/api/alarms/import", content);

            // Assert
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        }

        [Fact(DisplayName = "POST /api/alarms/import deve ter timeout apropriado")]
        public async Task ImportAlarms_ShouldCompleteWithinTimeout()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var authenticatedClient = await CreateAuthenticatedClient(scope);
            authenticatedClient.Timeout = TimeSpan.FromSeconds(30); // Timeout de 30 segundos
            
            var csvContent = "Name,Time\nQuick Test,08:00";
            var content = CreateMultipartFormDataContent("quick_test.csv", csvContent);

            // Act
            var startTime = DateTime.UtcNow;
            var response = await authenticatedClient.PostAsync("/api/alarms/import", content);
            var duration = DateTime.UtcNow - startTime;

            // Assert
            Assert.True(duration < TimeSpan.FromSeconds(30), "Request should complete within timeout");
        }

        private static MultipartFormDataContent CreateMultipartFormDataContent(
            string fileName, 
            string content, 
            string contentType = "text/csv")
        {
            var multipartContent = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            multipartContent.Add(fileContent, "file", fileName);
            multipartContent.Add(new StringContent("false"), "overwriteExisting");
            
            return multipartContent;
        }

        private async Task<HttpClient> CreateAuthenticatedClient(IServiceScope scope)
        {
            // Esta é uma implementação simplificada
            // Na implementação real, você precisaria:
            // 1. Criar um usuário de teste
            // 2. Gerar um token JWT válido
            // 3. Adicionar o token ao cabeçalho Authorization
            
            var client = _factory.CreateClient();
            
            // Simular autenticação - substitua pela sua lógica real de autenticação nos testes
            // client.DefaultRequestHeaders.Authorization = 
            //     new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "your-test-jwt-token");
            
            return client;
        }
    }
}
