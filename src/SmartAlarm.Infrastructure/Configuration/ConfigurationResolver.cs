using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartAlarm.KeyVault.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace SmartAlarm.Infrastructure.Configuration
{
    /// <summary>
    /// Resolve configurações seguindo a ordem: Environment -> Vault -> appsettings.
    /// </summary>
    public class ConfigurationResolver : IConfigurationResolver
    {
        private readonly IConfiguration _configuration;
        private readonly IKeyVaultService _keyVaultService;
        private readonly ILogger<ConfigurationResolver> _logger;

        public ConfigurationResolver(IConfiguration configuration, IKeyVaultService keyVaultService, ILogger<ConfigurationResolver> logger)
        {
            _configuration = configuration;
            _keyVaultService = keyVaultService;
            _logger = logger;
        }

        /// <summary>
        /// Obtém o valor da configuração seguindo a ordem: Environment -> Vault -> appsettings.
        /// </summary>
        public async Task<string> GetConfigAsync(string key, CancellationToken cancellationToken = default)
        {
            // 1. Environment
            var envValue = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrEmpty(envValue))
            {
                _logger.LogDebug("Configuração '{Key}' lida do Environment", key);
                return envValue;
            }

            // 2. Vault
            var vaultValue = await _keyVaultService.GetSecretAsync(key, cancellationToken);
            if (!string.IsNullOrEmpty(vaultValue))
            {
                _logger.LogDebug("Configuração '{Key}' lida do Vault", key);
                return vaultValue;
            }

            // 3. appsettings
            var appSettingsValue = _configuration[key];
            if (!string.IsNullOrEmpty(appSettingsValue))
            {
                _logger.LogDebug("Configuração '{Key}' lida do appsettings", key);
                return appSettingsValue;
            }

            _logger.LogError("Configuração '{Key}' não encontrada em Environment, Vault ou appsettings", key);
            throw new InvalidOperationException($"Configuração '{key}' não encontrada em Environment, Vault ou appsettings.");
        }
    }
}
