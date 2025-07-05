using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SmartAlarm.Infrastructure.KeyVault
{
    /// <summary>
    /// Mock para IKeyVaultProvider para desenvolvimento/testes.
    /// </summary>
    public class MockKeyVaultProvider : IKeyVaultProvider
    {
        private readonly ILogger<MockKeyVaultProvider> _logger;
        public MockKeyVaultProvider(ILogger<MockKeyVaultProvider> logger)
        {
            _logger = logger;
        }
        public Task<string?> GetSecretAsync(string key)
        {
            _logger.LogInformation("[MockVault] GetSecret {Key}", key);
            return Task.FromResult<string?>("mock-secret");
        }
        public Task<bool> SetSecretAsync(string key, string value)
        {
            _logger.LogInformation("[MockVault] SetSecret {Key}", key);
            return Task.FromResult(true);
        }
    }
}
