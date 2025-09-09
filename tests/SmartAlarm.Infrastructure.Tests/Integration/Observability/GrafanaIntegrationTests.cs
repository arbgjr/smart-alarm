using SmartAlarm.Domain.Abstractions;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests.Integration.Observability
{
    public class GrafanaIntegrationTests
    {
        private readonly HttpClient _client;
        private readonly string _grafanaBaseUrl;

        public GrafanaIntegrationTests()
        {
            // Usar DockerHelper para resolver configurações do Grafana
            _grafanaBaseUrl = DockerHelper.GetObservabilityUrl("grafana", 3001);
            _client = new HttpClient();
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "Observability")]
        public async Task Grafana_ShouldBeHealthy()
        {
            // Arrange
            var endpoint = $"{_grafanaBaseUrl}/api/health";
            
            // Act
            var response = await _client.GetAsync(endpoint);
            
            // Assert
            Assert.True(response.IsSuccessStatusCode, $"Grafana não está saudável. Status: {response.StatusCode}");
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("database", content); // A resposta da API de saúde do Grafana contém "database"
        }
        
        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "Observability")]
        public async Task Grafana_ShouldHaveAPIAccessible()
        {
            // Arrange
            var endpoint = $"{_grafanaBaseUrl}/api/datasources";
            var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            
            // Adicionar a autenticação básica (admin/admin é o padrão)
            var authValue = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("admin:admin"));
            request.Headers.Add("Authorization", $"Basic {authValue}");
            
            // Act
            var response = await _client.SendAsync(request);
            
            // Assert
            Assert.True(response.IsSuccessStatusCode, $"API do Grafana não está acessível. Status: {response.StatusCode}");
        }
    }
}
