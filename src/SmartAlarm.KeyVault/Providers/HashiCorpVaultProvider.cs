using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartAlarm.KeyVault.Abstractions;
using SmartAlarm.KeyVault.Configuration;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.Commons;

namespace SmartAlarm.KeyVault.Providers
{
    /// <summary>
    /// HashiCorp Vault secret provider implementation.
    /// </summary>
    public class HashiCorpVaultProvider : ISecretProvider
    {
        private readonly HashiCorpVaultOptions _options;
        private readonly ILogger<HashiCorpVaultProvider> _logger;
        private readonly Lazy<IVaultClient> _vaultClientLazy;

        public string ProviderName => "HashiCorp";
        public int Priority => _options.Priority;

        public HashiCorpVaultProvider(IOptions<HashiCorpVaultOptions> options, ILogger<HashiCorpVaultProvider> logger)
        {
            _options = options.Value;
            _logger = logger;
            _vaultClientLazy = new Lazy<IVaultClient>(CreateVaultClient);
        }

        public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(_options.ServerAddress) || string.IsNullOrEmpty(_options.Token))
                {
                    _logger.LogDebug("HashiCorp Vault provider not configured properly");
                    return false;
                }

                var client = _vaultClientLazy.Value;
                var healthStatus = await client.V1.System.GetHealthStatusAsync();
                
                _logger.LogDebug("HashiCorp Vault health check: Initialized={Initialized}, Sealed={Sealed}", 
                    healthStatus.Initialized, healthStatus.Sealed);
                
                return healthStatus.Initialized && !healthStatus.Sealed;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "HashiCorp Vault provider availability check failed");
                return false;
            }
        }

        public async Task<string?> GetSecretAsync(string secretKey, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(secretKey))
                {
                    _logger.LogWarning("Secret key cannot be null or empty");
                    return null;
                }

                var client = _vaultClientLazy.Value;
                
                if (_options.KvVersion == 2)
                {
                    var kv2Secret = await client.V1.Secrets.KeyValue.V2.ReadSecretAsync(
                        path: secretKey, 
                        mountPoint: _options.MountPath);
                    
                    if (kv2Secret?.Data?.Data != null && kv2Secret.Data.Data.ContainsKey("value"))
                    {
                        return kv2Secret.Data.Data["value"]?.ToString();
                    }
                    
                    // Try the secret key as both path and value key
                    return kv2Secret?.Data?.Data?.Values?.FirstOrDefault()?.ToString();
                }
                else
                {
                    var kv1Secret = await client.V1.Secrets.KeyValue.V1.ReadSecretAsync(
                        path: secretKey, 
                        mountPoint: _options.MountPath);
                    
                    if (kv1Secret?.Data != null && kv1Secret.Data.ContainsKey("value"))
                    {
                        return kv1Secret.Data["value"]?.ToString();
                    }
                    
                    // Try the secret key as both path and value key
                    return kv1Secret?.Data?.Values?.FirstOrDefault()?.ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve secret '{SecretKey}' from HashiCorp Vault", secretKey);
                return null;
            }
        }

        public async Task<Dictionary<string, string?>> GetSecretsAsync(IEnumerable<string> secretKeys, CancellationToken cancellationToken = default)
        {
            var results = new Dictionary<string, string?>();
            
            foreach (var secretKey in secretKeys)
            {
                results[secretKey] = await GetSecretAsync(secretKey, cancellationToken);
            }
            
            return results;
        }

        public async Task<bool> SetSecretAsync(string secretKey, string secretValue, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(secretKey) || secretValue == null)
                {
                    _logger.LogWarning("Secret key and value cannot be null or empty");
                    return false;
                }

                var client = _vaultClientLazy.Value;
                var secretData = new Dictionary<string, object> { ["value"] = secretValue };

                if (_options.KvVersion == 2)
                {
                    await client.V1.Secrets.KeyValue.V2.WriteSecretAsync(
                        path: secretKey,
                        data: secretData,
                        mountPoint: _options.MountPath);
                }
                else
                {
                    await client.V1.Secrets.KeyValue.V1.WriteSecretAsync(
                        path: secretKey,
                        values: secretData,
                        mountPoint: _options.MountPath);
                }

                _logger.LogDebug("Successfully set secret '{SecretKey}' in HashiCorp Vault", secretKey);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set secret '{SecretKey}' in HashiCorp Vault", secretKey);
                return false;
            }
        }

        private IVaultClient CreateVaultClient()
        {
            var authMethod = new TokenAuthMethodInfo(_options.Token);
            var vaultClientSettings = new VaultClientSettings(_options.ServerAddress, authMethod);
            
            if (_options.SkipTlsVerification)
            {
                _logger.LogWarning("TLS verification is disabled for HashiCorp Vault - use only in development");
                vaultClientSettings.MyHttpClientProviderFunc = handler =>
                {
                    if (handler is HttpClientHandler httpHandler)
                    {
                        httpHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
                    }
                    return new HttpClient(handler);
                };
            }

            return new VaultClient(vaultClientSettings);
        }
    }
}