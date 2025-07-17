using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SmartAlarm.Infrastructure.KeyVault
{
    // STUB DE INTEGRAÇÃO
    // Integração real com o serviço cloud ainda não implementada.
    // TODO: Substituir por implementação real antes do deploy em produção.
    /// <summary>
    /// Stub para integração futura com AWS Secrets Manager (opcional).
    /// </summary>
    public class AwsSecretsManagerProvider : IKeyVaultProvider
    {
        private readonly ILogger<AwsSecretsManagerProvider> _logger;
        public AwsSecretsManagerProvider(ILogger<AwsSecretsManagerProvider> logger)
        {
            _logger = logger;
        }
        public Task<string?> GetSecretAsync(string key)
        {
            _logger.LogInformation("[AWS Secrets Manager] GetSecret {Key}", key);
            // TODO: Implementar integração real com AWS SDK
            return Task.FromResult<string?>(null);
        }
        public Task<bool> SetSecretAsync(string key, string value)
        {
            _logger.LogInformation("[AWS Secrets Manager] SetSecret {Key}", key);
            // TODO: Implementar integração real com AWS SDK
            return Task.FromResult(false);
        }
    }
}
