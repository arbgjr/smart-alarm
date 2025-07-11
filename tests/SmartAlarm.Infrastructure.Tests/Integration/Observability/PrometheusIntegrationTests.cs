using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests.Integration.Observability
{
    public class PrometheusIntegrationTests
    {
        private readonly HttpClient _client;
        private readonly string _prometheusHost;
        private readonly int _prometheusPort;

        public PrometheusIntegrationTests()
        {
            // Determinar o host do Prometheus com base no ambiente
            _prometheusHost = Environment.GetEnvironmentVariable("PROMETHEUS_HOST") ?? "localhost";
            
            // Tentar obter a porta do Prometheus do ambiente, ou usar o padrão 9090
            if (!int.TryParse(Environment.GetEnvironmentVariable("PROMETHEUS_PORT"), out _prometheusPort))
            {
                _prometheusPort = 9090;
            }
            
            _client = new HttpClient();
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "Observability")]
        public async Task Prometheus_ShouldBeHealthy()
        {
            // Arrange
            var endpoint = $"http://{_prometheusHost}:{_prometheusPort}/-/healthy";
            
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
            var endpoint = $"http://{_prometheusHost}:{_prometheusPort}/api/v1/query?query=up";
            
            // Act
            var response = await _client.GetAsync(endpoint);
            
            // Assert
            Assert.True(response.IsSuccessStatusCode, $"API do Prometheus não está disponível. Status: {response.StatusCode}");
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("\"status\":\"success\"", content); // A resposta da API do Prometheus contém status success
        }
    }
}
