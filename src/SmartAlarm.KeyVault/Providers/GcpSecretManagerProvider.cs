using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.SecretManager.V1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartAlarm.KeyVault.Abstractions;
using SmartAlarm.KeyVault.Configuration;
using Google.Api.Gax.ResourceNames;
using Grpc.Core;

namespace SmartAlarm.KeyVault.Providers
{
    /// <summary>
    /// Google Cloud Secret Manager secret provider implementation.
    /// </summary>
    public class GcpSecretManagerProvider : ISecretProvider
    {
        private readonly GcpSecretManagerOptions _options;
        private readonly ILogger<GcpSecretManagerProvider> _logger;
        private readonly Lazy<SecretManagerServiceClient> _clientLazy;

        public string ProviderName => "GCP";
        public int Priority => _options.Priority;

        public GcpSecretManagerProvider(IOptions<GcpSecretManagerOptions> options, ILogger<GcpSecretManagerProvider> logger)
        {
            _options = options.Value;
            _logger = logger;
            _clientLazy = new Lazy<SecretManagerServiceClient>(CreateSecretManagerClient);
        }

        public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(_options.ProjectId))
                {
                    _logger.LogDebug("GCP Secret Manager provider not configured properly");
                    return false;
                }

                var client = _clientLazy.Value;
                var projectName = ProjectName.FromProject(_options.ProjectId);
                
                // Try to list secrets to verify connectivity and authentication
                var request = new ListSecretsRequest
                {
                    ParentAsProjectName = projectName,
                    PageSize = 1
                };

                await client.ListSecretsAsync(request).ReadPageAsync(1);

                _logger.LogDebug("GCP Secret Manager provider is available");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "GCP Secret Manager provider availability check failed");
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
                var secretName = SecretVersionName.FromProjectSecretSecretVersion(_options.ProjectId, secretKey, "latest");

                var response = await client.AccessSecretVersionAsync(secretName, cancellationToken);
                return response.Payload.Data.ToStringUtf8();
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                _logger.LogDebug("Secret '{SecretKey}' not found in GCP Secret Manager", secretKey);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve secret '{SecretKey}' from GCP Secret Manager", secretKey);
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
                var projectName = ProjectName.FromProject(_options.ProjectId);

                try
                {
                    // Try to get the secret first to see if it exists
                    var secretName = SecretName.FromProjectSecret(_options.ProjectId, secretKey);
                    await client.GetSecretAsync(secretName, cancellationToken);
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
                {
                    // Secret doesn't exist, create it
                    var createSecretRequest = new CreateSecretRequest
                    {
                        ParentAsProjectName = projectName,
                        SecretId = secretKey,
                        Secret = new Secret
                        {
                            Replication = new Replication
                            {
                                Automatic = new Replication.Types.Automatic()
                            }
                        }
                    };

                    await client.CreateSecretAsync(createSecretRequest, cancellationToken);
                }

                // Add a new version of the secret
                var addVersionRequest = new AddSecretVersionRequest
                {
                    ParentAsSecretName = SecretName.FromProjectSecret(_options.ProjectId, secretKey),
                    Payload = new SecretPayload
                    {
                        Data = Google.Protobuf.ByteString.CopyFromUtf8(secretValue)
                    }
                };

                await client.AddSecretVersionAsync(addVersionRequest, cancellationToken);

                _logger.LogDebug("Successfully set secret '{SecretKey}' in GCP Secret Manager", secretKey);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set secret '{SecretKey}' in GCP Secret Manager", secretKey);
                return false;
            }
        }

        private SecretManagerServiceClient CreateSecretManagerClient()
        {
            try
            {
                if (_options.UseApplicationDefaultCredentials)
                {
                    _logger.LogDebug("Using application default credentials for GCP Secret Manager");
                    return SecretManagerServiceClient.Create();
                }
                else if (!string.IsNullOrEmpty(_options.ServiceAccountKeyPath))
                {
                    _logger.LogDebug("Using service account key file for GCP Secret Manager");
                    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", _options.ServiceAccountKeyPath);
                    return SecretManagerServiceClient.Create();
                }
                else if (!string.IsNullOrEmpty(_options.ServiceAccountKeyJson))
                {
                    _logger.LogDebug("Using service account key JSON for GCP Secret Manager");
                    var credential = Google.Apis.Auth.OAuth2.GoogleCredential.FromJson(_options.ServiceAccountKeyJson);
                    return new SecretManagerServiceClientBuilder
                    {
                        Credential = credential
                    }.Build();
                }
                else
                {
                    throw new InvalidOperationException("No valid authentication method configured for GCP Secret Manager");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create GCP Secret Manager client");
                throw;
            }
        }
    }
}