using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests.Integration.Observability
{
    public class GrafanaIntegrationTests
    {
        private readonly HttpClient _client;
        private readonly string _grafanaHost;
        private readonly int _grafanaPort;

        public GrafanaIntegrationTests()
        {
            // Determinar o host do Grafana com base no ambiente
            _grafanaHost = Environment.GetEnvironmentVariable("GRAFANA_HOST") ?? "localhost";
            
            // Tentar obter a porta do Grafana do ambiente, ou usar o padrão 3000
            // Observação: no docker-compose está mapeado para 3001:3000
            if (!int.TryParse(Environment.GetEnvironmentVariable("GRAFANA_PORT"), out _grafanaPort))
            {
                _grafanaPort = 3001;
            }
            
            _client = new HttpClient();
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "Observability")]
        public async Task Grafana_ShouldBeHealthy()
        {
            // Arrange
            var endpoint = $"http://{_grafanaHost}:{_grafanaPort}/api/health";
            
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
            var endpoint = $"http://{_grafanaHost}:{_grafanaPort}/api/datasources";
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
