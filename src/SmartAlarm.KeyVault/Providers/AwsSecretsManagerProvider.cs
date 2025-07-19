using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartAlarm.KeyVault.Abstractions;
using SmartAlarm.KeyVault.Configuration;

namespace SmartAlarm.KeyVault.Providers
{
    /// <summary>
    /// AWS Secrets Manager secret provider implementation.
    /// </summary>
    public class AwsSecretsManagerProvider : ISecretProvider
    {
        private readonly AwsSecretsManagerOptions _options;
        private readonly ILogger<AwsSecretsManagerProvider> _logger;
        private readonly Lazy<AmazonSecretsManagerClient> _clientLazy;

        public string ProviderName => "AWS";
        public int Priority => _options.Priority;

        public AwsSecretsManagerProvider(IOptions<AwsSecretsManagerOptions> options, ILogger<AwsSecretsManagerProvider> logger)
        {
            _options = options.Value;
            _logger = logger;
            _clientLazy = new Lazy<AmazonSecretsManagerClient>(CreateSecretsManagerClient);
        }

        public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(_options.Region))
                {
                    _logger.LogDebug("AWS Secrets Manager provider not configured properly");
                    return false;
                }

                var client = _clientLazy.Value;
                
                // Try to list secrets to verify connectivity and authentication
                var request = new ListSecretsRequest { MaxResults = 1 };
                await client.ListSecretsAsync(request, cancellationToken);

                _logger.LogDebug("AWS Secrets Manager provider is available");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "AWS Secrets Manager provider availability check failed");
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

                var client = _clientLazy.Value;
                
                var request = new GetSecretValueRequest
                {
                    SecretId = secretKey
                };

                var response = await client.GetSecretValueAsync(request, cancellationToken);
                return response.SecretString;
            }
            catch (ResourceNotFoundException)
            {
                _logger.LogDebug("Secret '{SecretKey}' not found in AWS Secrets Manager", secretKey);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve secret '{SecretKey}' from AWS Secrets Manager", secretKey);
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

                var client = _clientLazy.Value;

                try
                {
                    // Try to update existing secret
                    var updateRequest = new UpdateSecretRequest
                    {
                        SecretId = secretKey,
                        SecretString = secretValue
                    };

                    await client.UpdateSecretAsync(updateRequest, cancellationToken);
                }
                catch (ResourceNotFoundException)
                {
                    // Secret doesn't exist, create it
                    var createRequest = new CreateSecretRequest
                    {
                        Name = secretKey,
                        SecretString = secretValue
                    };

                    await client.CreateSecretAsync(createRequest, cancellationToken);
                }

                _logger.LogDebug("Successfully set secret '{SecretKey}' in AWS Secrets Manager", secretKey);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set secret '{SecretKey}' in AWS Secrets Manager", secretKey);
                return false;
            }
        }

        private AmazonSecretsManagerClient CreateSecretsManagerClient()
        {
            var regionEndpoint = RegionEndpoint.GetBySystemName(_options.Region);
            
            if (_options.UseIamRole || (string.IsNullOrEmpty(_options.AccessKeyId) && string.IsNullOrEmpty(_options.SecretAccessKey)))
            {
                _logger.LogDebug("Using IAM role for AWS Secrets Manager authentication");
                return new AmazonSecretsManagerClient(regionEndpoint);
            }
            else
            {
                _logger.LogDebug("Using access key for AWS Secrets Manager authentication");
                return new AmazonSecretsManagerClient(_options.AccessKeyId, _options.SecretAccessKey, _options.SessionToken, regionEndpoint);
            }
        }
    }
}