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
            services.AddHttpClient<HashiCorpVaultProvider>(client =>
            {
                client.BaseAddress = new System.Uri("http://localhost:8200");
            });
            var sp = services.BuildServiceProvider();
            _logger = sp.GetRequiredService<ILogger<HashiCorpVaultProvider>>();
            _provider = sp.GetRequiredService<HashiCorpVaultProvider>();
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
