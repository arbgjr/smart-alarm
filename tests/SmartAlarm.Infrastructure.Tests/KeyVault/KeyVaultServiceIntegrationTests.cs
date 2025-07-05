using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartAlarm.Infrastructure.KeyVault;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests.KeyVault
{
    public class KeyVaultServiceIntegrationTests
    {
        private readonly IKeyVaultProvider _provider;
        private readonly ILogger<HashiCorpVaultProvider> _logger;

        public KeyVaultServiceIntegrationTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<HashiCorpVaultProvider>();
            var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:8200") };
            _provider = new HashiCorpVaultProvider(httpClient, _logger);
        }

        [Fact(DisplayName = "Deve ler e escrever segredo no HashiCorp Vault real")]
        public async Task Deve_Ler_Escrever_Segredo_HashicorpVault()
        {
            // Arrange
            var secretKey = $"test-secret-{Guid.NewGuid()}";
            var secretValue = "valor-integração";

            // Act
            var setOk = await _provider.SetSecretAsync(secretKey, secretValue);
            var lido = await _provider.GetSecretAsync(secretKey);

            // Assert
            Assert.True(setOk, "Falha ao gravar segredo no Vault");
            Assert.Equal(secretValue, lido);
        }
    }
}
