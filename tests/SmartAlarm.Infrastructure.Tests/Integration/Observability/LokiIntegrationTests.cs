using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests.Integration.Observability
{
    public class LokiIntegrationTests
    {
        private readonly HttpClient _client;
        private readonly string _lokiBaseUrl;

        public LokiIntegrationTests()
        {
            // Usar DockerHelper para resolver configurações do Loki
            _lokiBaseUrl = DockerHelper.GetObservabilityUrl("loki", 3100);
            _client = new HttpClient();
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "Observability")]
        public async Task Loki_ShouldBeHealthy()
        {
            // Arrange
            var endpoint = $"{_lokiBaseUrl}/ready";
            
            // Act
            var response = await _client.GetAsync(endpoint);
            
            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            Assert.True(response.IsSuccessStatusCode, $"Loki não está saudável. Status: {response.StatusCode}");
        }
        
        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "Observability")]
        public async Task Loki_ShouldBeAbleToReceiveLogs()
        {
            // Arrange
            var endpoint = $"{_lokiBaseUrl}/loki/api/v1/push";
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000000; // Nanoseconds
            
            var logEntry = @"
            {
                ""streams"": [
                    {
                        ""stream"": {
                            ""app"": ""smart-alarm-test"",
                            ""env"": ""testing""
                        },
                        ""values"": [
                            [""" + timestamp + @""", ""Smart Alarm Integration Test Log""]
                        ]
                    }
                ]
            }";
            
            var content = new StringContent(logEntry, System.Text.Encoding.UTF8, "application/json");
            
            // Act
            var response = await _client.PostAsync(endpoint, content);
            
            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            Assert.True(response.IsSuccessStatusCode, $"Falha ao enviar log para o Loki. Status: {response.StatusCode}");
        }
    }
}
