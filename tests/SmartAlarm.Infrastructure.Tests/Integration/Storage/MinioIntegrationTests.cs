using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace SmartAlarm.Infrastructure.Tests.Integration.Storage
{
    public class MinioIntegrationTests : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _minioEndpoint;
        private readonly ITestOutputHelper _output;

        public MinioIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
            
            // Determinar o host do MinIO com base no ambiente
            // Se MinIOConfig__Endpoint estiver definida (ambiente Docker), use ela
            string minioEndpoint = Environment.GetEnvironmentVariable("MinIOConfig__Endpoint");
            if (!string.IsNullOrEmpty(minioEndpoint))
            {
                _minioEndpoint = $"http://{minioEndpoint}";
            }
            else
            {
                // Fallback para localhost (desenvolvimento local)
                string minioHost = Environment.GetEnvironmentVariable("MINIO_HOST") ?? "localhost";
                string portStr = Environment.GetEnvironmentVariable("MINIO_PORT") ?? "9000";
                _minioEndpoint = $"http://{minioHost}:{portStr}";
            }
            
            _output.WriteLine($"Tentando conectar ao MinIO em {_minioEndpoint}");
            _httpClient = new HttpClient();
            
            _output.WriteLine($"Configurado para testar MinIO em {_minioEndpoint}");
        }
        
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
        
        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "MinIO")]
        public async Task MinioHealthCheck_ShouldBeOnline()
        {
            _output.WriteLine("Verificando se o serviço MinIO está acessível");
            
            // Verificar se o endpoint de saúde do MinIO está respondendo
            var response = await _httpClient.GetAsync($"{_minioEndpoint}/minio/health/live");
            
            // Verificar se o serviço está online
            response.StatusCode.Should().Be(HttpStatusCode.OK, "O serviço MinIO deve estar acessível");
            
            _output.WriteLine("MinIO está disponível e respondendo!");
        }
    }
}
