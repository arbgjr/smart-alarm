using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests.Integration.Observability
{
    public class PrometheusIntegrationTests
    {
        private readonly HttpClient _client;
        private readonly string _prometheusBaseUrl;

        public PrometheusIntegrationTests()
        {
            // Usar DockerHelper para resolver configurações do Prometheus
            _prometheusBaseUrl = DockerHelper.GetObservabilityUrl("prometheus", 9090);
            _client = new HttpClient();
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "Observability")]
        public async Task Prometheus_ShouldBeHealthy()
        {
            // Arrange
            var endpoint = $"{_prometheusBaseUrl}/-/healthy";
            
            // Act
            var response = await _client.GetAsync(endpoint);
            
            // Assert
            Assert.True(response.IsSuccessStatusCode, $"Prometheus não está saudável. Status: {response.StatusCode}");
        }
        
        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "Observability")]
        public async Task Prometheus_ShouldBeAbleToQueryMetrics()
        {
            // Arrange
            var endpoint = $"{_prometheusBaseUrl}/api/v1/query?query=up";
            
            // Act
            var response = await _client.GetAsync(endpoint);
            
            // Assert
            Assert.True(response.IsSuccessStatusCode, $"API do Prometheus não está disponível. Status: {response.StatusCode}");
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("\"status\":\"success\"", content); // A resposta da API do Prometheus contém status success
        }
    }
}
