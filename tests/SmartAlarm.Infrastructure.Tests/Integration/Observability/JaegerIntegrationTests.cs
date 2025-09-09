using SmartAlarm.Domain.Abstractions;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests.Integration.Observability
{
    public class JaegerIntegrationTests
    {
        private readonly HttpClient _client;
        private readonly string _jaegerBaseUrl;

        public JaegerIntegrationTests()
        {
            // Usar DockerHelper para resolver configurações do Jaeger
            _jaegerBaseUrl = DockerHelper.GetObservabilityUrl("jaeger", 16686);
            _client = new HttpClient();
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "Observability")]
        public async Task Jaeger_ShouldBeHealthy()
        {
            // Arrange
            var endpoint = $"{_jaegerBaseUrl}/";
            
            // Act
            var response = await _client.GetAsync(endpoint);
            
            // Assert
            Assert.True(response.IsSuccessStatusCode, $"Jaeger não está saudável. Status: {response.StatusCode}");
        }
        
        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "Observability")]
        public async Task Jaeger_ShouldHaveApiEndpointAvailable()
        {
            // Arrange
            var endpoint = $"{_jaegerBaseUrl}/api/services";
            
            // Act
            var response = await _client.GetAsync(endpoint);
            
            // Assert
            Assert.True(response.IsSuccessStatusCode, $"API do Jaeger não está disponível. Status: {response.StatusCode}");
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("data", content); // A resposta da API do Jaeger sempre contém um campo "data"
        }
    }
}
