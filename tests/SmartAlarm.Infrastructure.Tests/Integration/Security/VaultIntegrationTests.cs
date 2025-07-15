using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace SmartAlarm.Infrastructure.Tests.Integration.Security
{
    public class VaultIntegrationTests : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _vaultEndpoint;
        private readonly ITestOutputHelper _output;
        
        public VaultIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
            
            // Determinar o host do Vault com base no ambiente
            // Se VaultConfig__Address estiver definida (ambiente Docker), use ela
            string vaultAddress = Environment.GetEnvironmentVariable("VaultConfig__Address");
            if (!string.IsNullOrEmpty(vaultAddress))
            {
                _vaultEndpoint = vaultAddress;
            }
            else
            {
                // Fallback para localhost (desenvolvimento local)
                string vaultHost = Environment.GetEnvironmentVariable("VAULT_HOST") ?? "localhost";
                string portStr = Environment.GetEnvironmentVariable("VAULT_PORT") ?? "8200";
                _vaultEndpoint = $"http://{vaultHost}:{portStr}";
            }
            
            _httpClient = new HttpClient();
            
            _output.WriteLine($"Configurado para testar Vault em {_vaultEndpoint}");
        }
        
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
        
        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "Vault")]
        public async Task VaultHealthCheck_ShouldBeOnline()
        {
            _output.WriteLine("Verificando se o serviço Vault está acessível");
            
            // Verificar se o endpoint de saúde do Vault está respondendo
            var response = await _httpClient.GetAsync($"{_vaultEndpoint}/v1/sys/health");
            
            // Verificar se o serviço está online (aceita 200 OK ou 429 Standby)
            response.IsSuccessStatusCode.Should().BeTrue("O serviço Vault deve estar acessível");
            
            _output.WriteLine("Vault está disponível e respondendo!");
        }
    }
}
