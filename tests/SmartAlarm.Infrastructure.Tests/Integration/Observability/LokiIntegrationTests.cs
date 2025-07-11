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
        private readonly string _lokiHost;
        private readonly int _lokiPort;

        public LokiIntegrationTests()
        {
            // Determinar o host do Loki com base no ambiente
            _lokiHost = Environment.GetEnvironmentVariable("LOKI_HOST") ?? "localhost";
            
            // Tentar obter a porta do Loki do ambiente, ou usar o padrão 3100
            if (!int.TryParse(Environment.GetEnvironmentVariable("LOKI_PORT"), out _lokiPort))
            {
                _lokiPort = 3100;
            }
            
            _client = new HttpClient();
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "Observability")]
        public async Task Loki_ShouldBeHealthy()
        {
            // Arrange
            var endpoint = $"http://{_lokiHost}:{_lokiPort}/ready";
            
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
            var endpoint = $"http://{_lokiHost}:{_lokiPort}/loki/api/v1/push";
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
