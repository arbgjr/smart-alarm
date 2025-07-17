using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace SmartAlarm.Infrastructure.KeyVault
{
    /// <summary>
    /// Implementação real do provedor AWS Secrets Manager
    /// </summary>
    public class AwsSecretsManagerProvider : IKeyVaultProvider
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AwsSecretsManagerProvider> _logger;
        private readonly string _region;
        private readonly Lazy<AmazonSecretsManagerClient> _clientLazy;

        public AwsSecretsManagerProvider(
            IConfiguration configuration,
            ILogger<AwsSecretsManagerProvider> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _region = _configuration["AWS:Region"] ?? "us-east-1";
            _clientLazy = new Lazy<AmazonSecretsManagerClient>(CreateClient);
        }

        /// <summary>
        /// Cria o cliente AWS Secrets Manager com as credenciais apropriadas.
        /// </summary>
        private AmazonSecretsManagerClient CreateClient()
        {
            var regionEndpoint = RegionEndpoint.GetBySystemName(_region);
            
            // Verifica se deve usar credenciais específicas ou IAM Role/Instance Profile
            var accessKeyId = _configuration["AWS:AccessKeyId"];
            var secretAccessKey = _configuration["AWS:SecretAccessKey"];
            var sessionToken = _configuration["AWS:SessionToken"];

            if (!string.IsNullOrEmpty(accessKeyId) && !string.IsNullOrEmpty(secretAccessKey))
            {
                _logger.LogDebug("Usando credenciais AWS explícitas para Secrets Manager");
                return new AmazonSecretsManagerClient(accessKeyId, secretAccessKey, sessionToken, regionEndpoint);
            }
            else
            {
                _logger.LogDebug("Usando credenciais AWS padrão (IAM Role/Instance Profile) para Secrets Manager");
                return new AmazonSecretsManagerClient(regionEndpoint);
            }
        }

        public async Task<string?> GetSecretAsync(string key)
        {
            try
            {
                _logger.LogInformation("Retrieving secret from AWS Secrets Manager: {Key}", key);
                
                var client = _clientLazy.Value;
                var request = new GetSecretValueRequest
                {
                    SecretId = key
                };

                var response = await client.GetSecretValueAsync(request);
                
                _logger.LogInformation("Successfully retrieved secret from AWS Secrets Manager: {Key}", key);
                return response.SecretString;
            }
            catch (ResourceNotFoundException)
            {
                _logger.LogWarning("Secret not found in AWS Secrets Manager: {Key}", key);
                return null;
            }
            catch (InvalidParameterException ex)
            {
                _logger.LogWarning(ex, "Invalid parameter for AWS Secrets Manager secret: {Key}", key);
                return null;
            }
            catch (InvalidRequestException ex)
            {
                _logger.LogWarning(ex, "Invalid request for AWS Secrets Manager secret: {Key}", key);
                return null;
            }
            catch (DecryptionFailureException ex)
            {
                _logger.LogError(ex, "Decryption failed for AWS Secrets Manager secret: {Key}", key);
                return null;
            }
            catch (InternalServiceErrorException ex)
            {
                _logger.LogError(ex, "AWS Secrets Manager internal service error for secret: {Key}", key);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get secret from AWS Secrets Manager: {Key}", key);
                return null;
            }
        }

        public async Task<bool> SetSecretAsync(string key, string value)
        {
            try
            {
                _logger.LogInformation("Setting secret in AWS Secrets Manager: {Key}", key);
                
                var client = _clientLazy.Value;
                
                try
                {
                    // Tentar atualizar primeiro
                    var updateRequest = new UpdateSecretRequest
                    {
                        SecretId = key,
                        SecretString = value
                    };
                    await client.UpdateSecretAsync(updateRequest);
                    
                    _logger.LogInformation("Successfully updated secret in AWS Secrets Manager: {Key}", key);
                }
                catch (ResourceNotFoundException)
                {
                    // Se não existir, criar novo
                    var createRequest = new CreateSecretRequest
                    {
                        Name = key,
                        SecretString = value,
                        Description = $"Secret {key} created by SmartAlarm"
                    };
                    await client.CreateSecretAsync(createRequest);
                    
                    _logger.LogInformation("Successfully created secret in AWS Secrets Manager: {Key}", key);
                }
                
                return true;
            }
            catch (InvalidParameterException ex)
            {
                _logger.LogError(ex, "Invalid parameter for AWS Secrets Manager secret: {Key}", key);
                return false;
            }
            catch (InvalidRequestException ex)
            {
                _logger.LogError(ex, "Invalid request for AWS Secrets Manager secret: {Key}", key);
                return false;
            }
            catch (LimitExceededException ex)
            {
                _logger.LogError(ex, "AWS Secrets Manager limit exceeded for secret: {Key}", key);
                return false;
            }
            catch (EncryptionFailureException ex)
            {
                _logger.LogError(ex, "Encryption failed for AWS Secrets Manager secret: {Key}", key);
                return false;
            }
            catch (ResourceExistsException ex)
            {
                _logger.LogError(ex, "Secret already exists in AWS Secrets Manager: {Key}", key);
                return false;
            }
            catch (InternalServiceErrorException ex)
            {
                _logger.LogError(ex, "AWS Secrets Manager internal service error for secret: {Key}", key);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set secret in AWS Secrets Manager: {Key}", key);
                return false;
            }
        }
    }
}
