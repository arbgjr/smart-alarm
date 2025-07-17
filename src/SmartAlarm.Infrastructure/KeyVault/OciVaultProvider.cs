using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SmartAlarm.Infrastructure.KeyVault
{
    // STUB DE INTEGRAÇÃO
    // Integração real com o serviço cloud ainda não implementada.
    // TODO: Substituir por implementação real antes do deploy em produção.
    /// <summary>
    /// Stub para integração futura com OCI Vault (produção).
    /// </summary>
    public class OciVaultProvider : IKeyVaultProvider
    {
        private readonly ILogger<OciVaultProvider> _logger;
        public OciVaultProvider(ILogger<OciVaultProvider> logger)
        {
            _logger = logger;
        }
        public Task<string?> GetSecretAsync(string key)
        {
            _logger.LogInformation("[OCI Vault] GetSecret {Key}", key);
            // TODO: Implementar integração real com OCI SDK
            return Task.FromResult<string?>(null);
        }
        public Task<bool> SetSecretAsync(string key, string value)
        {
            _logger.LogInformation("[OCI Vault] SetSecret {Key}", key);
            // TODO: Implementar integração real com OCI SDK
            return Task.FromResult(false);
        }
    }
}
