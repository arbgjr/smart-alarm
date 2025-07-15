using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartAlarm.Infrastructure.KeyVault;
using Xunit;

namespace SmartAlarm.KeyVault.Tests.Providers
{
    public class HashiCorpVaultProviderIntegrationTests
    {
        private readonly HashiCorpVaultProvider _provider;
        private readonly ILogger<HashiCorpVaultProvider> _logger;

        public HashiCorpVaultProviderIntegrationTests()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            
            // Usar variável de ambiente para o endereço do Vault
            var vaultAddress = System.Environment.GetEnvironmentVariable("HashiCorpVault__ServerAddress") 
                              ?? "http://localhost:8200";
            
            services.AddHttpClient<HashiCorpVaultProvider>(client =>
            {
                client.BaseAddress = new System.Uri(vaultAddress);
            });
            
            var sp = services.BuildServiceProvider();
            var httpClientFactory = sp.GetRequiredService<System.Net.Http.IHttpClientFactory>();
            _logger = sp.GetRequiredService<ILogger<HashiCorpVaultProvider>>();
            
            // Criar o provider com o HttpClient configurado
            var httpClient = httpClientFactory.CreateClient(nameof(HashiCorpVaultProvider));
            _provider = new HashiCorpVaultProvider(httpClient, _logger);
        }

        [Fact(DisplayName = "Deve escrever e ler segredo no HashiCorp Vault")]
        public async Task Deve_Escrever_Ler_Segredo()
        {
            // Arrange
            var key = $"test-secret-{System.Guid.NewGuid()}";
            var valor = "valor-teste";

            // Act
            var set = await _provider.SetSecretAsync(key, valor);
            var lido = await _provider.GetSecretAsync(key);

            // Assert
            Assert.True(set, "Falha ao gravar segredo");
            Assert.Equal(valor, lido);
        }
    }
}
