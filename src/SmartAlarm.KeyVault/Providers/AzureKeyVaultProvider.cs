using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartAlarm.KeyVault.Abstractions;
using SmartAlarm.KeyVault.Configuration;

namespace SmartAlarm.KeyVault.Providers
{
    /// <summary>
    /// Azure Key Vault secret provider implementation.
    /// </summary>
    public class AzureKeyVaultProvider : ISecretProvider
    {
        private readonly AzureKeyVaultOptions _options;
        private readonly ILogger<AzureKeyVaultProvider> _logger;
        private readonly Lazy<SecretClient> _secretClientLazy;

        public string ProviderName => "Azure";
        public int Priority => _options.Priority;

        public AzureKeyVaultProvider(IOptions<AzureKeyVaultOptions> options, ILogger<AzureKeyVaultProvider> logger)
        {
            _options = options.Value;
            _logger = logger;
            _secretClientLazy = new Lazy<SecretClient>(CreateSecretClient);
        }

        public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(_options.VaultUri))
                {
                    _logger.LogDebug("Azure Key Vault provider not configured properly");
                    return false;
                }

                var client = _secretClientLazy.Value;
                
                // Try to list secrets to verify connectivity and authentication
                var secretsAsyncEnumerable = client.GetPropertiesOfSecretsAsync(cancellationToken);
                var count = 0;
                await foreach (var _ in secretsAsyncEnumerable.WithCancellation(cancellationToken))
                {
                    count++;
                    if (count >= 1) break; // Just need to verify we can connect
                }

                _logger.LogDebug("Azure Key Vault provider is available");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Azure Key Vault provider availability check failed");
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

                var client = _secretClientLazy.Value;
                
                // Azure Key Vault secret names must be alphanumeric and dashes only
                var normalizedKey = NormalizeSecretName(secretKey);
                
                var response = await client.GetSecretAsync(normalizedKey, cancellationToken: cancellationToken);
                return response.Value?.Value;
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogDebug("Secret '{SecretKey}' not found in Azure Key Vault", secretKey);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve secret '{SecretKey}' from Azure Key Vault", secretKey);
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

                var client = _secretClientLazy.Value;
                
                // Azure Key Vault secret names must be alphanumeric and dashes only
                var normalizedKey = NormalizeSecretName(secretKey);
                
                await client.SetSecretAsync(normalizedKey, secretValue, cancellationToken);
                
                _logger.LogDebug("Successfully set secret '{SecretKey}' in Azure Key Vault", secretKey);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set secret '{SecretKey}' in Azure Key Vault", secretKey);
                return false;
            }
        }

        private SecretClient CreateSecretClient()
        {
            var vaultUri = new Uri(_options.VaultUri);
            
            Azure.Core.TokenCredential credential;
            
            if (_options.UseManagedIdentity || (string.IsNullOrEmpty(_options.ClientId) && string.IsNullOrEmpty(_options.ClientSecret)))
            {
                credential = new DefaultAzureCredential();
                _logger.LogDebug("Using managed identity for Azure Key Vault authentication");
            }
            else
            {
                var clientSecretCredentialOptions = new ClientSecretCredentialOptions();
                if (!string.IsNullOrEmpty(_options.TenantId))
                {
                    credential = new ClientSecretCredential(_options.TenantId, _options.ClientId, _options.ClientSecret, clientSecretCredentialOptions);
                }
                else
                {
                    throw new InvalidOperationException("TenantId is required for client secret authentication");
                }
                
                _logger.LogDebug("Using client secret credential for Azure Key Vault authentication");
            }

            return new SecretClient(vaultUri, credential);
        }

        private static string NormalizeSecretName(string secretKey)
        {
            // Azure Key Vault secret names must be 1-127 characters long and contain only alphanumeric characters and dashes
            return secretKey.Replace("_", "-").Replace(".", "-").Replace("/", "-");
        }
    }
}