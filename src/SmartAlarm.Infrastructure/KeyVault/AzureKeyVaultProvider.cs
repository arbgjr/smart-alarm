using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SmartAlarm.Infrastructure.KeyVault
{
    /// <summary>
    /// Stub para integração futura com Azure Key Vault (opcional).
    /// </summary>
    public class AzureKeyVaultProvider : IKeyVaultProvider
    {
        private readonly ILogger<AzureKeyVaultProvider> _logger;
        public AzureKeyVaultProvider(ILogger<AzureKeyVaultProvider> logger)
        {
            _logger = logger;
        }
        public Task<string?> GetSecretAsync(string key)
        {
            _logger.LogInformation("[Azure Key Vault] GetSecret {Key}", key);
            // TODO: Implementar integração real com Azure SDK
            return Task.FromResult<string?>(null);
        }
        public Task<bool> SetSecretAsync(string key, string value)
        {
            _logger.LogInformation("[Azure Key Vault] SetSecret {Key}", key);
            // TODO: Implementar integração real com Azure SDK
            return Task.FromResult(false);
        }
    }
}
