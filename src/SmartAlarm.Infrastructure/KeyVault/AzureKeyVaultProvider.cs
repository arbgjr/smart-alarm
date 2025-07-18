using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;
using Azure.Core;
using Azure;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Logging;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;

namespace SmartAlarm.Infrastructure.KeyVault
{
    /// <summary>
    /// Implementação enterprise do provedor Azure Key Vault com retry policies e error handling robusto
    /// </summary>
    public class AzureKeyVaultProvider : IKeyVaultProvider, IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AzureKeyVaultProvider> _logger;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly Lazy<SecretClient> _secretClient;
        private readonly string _keyVaultUri;
        private bool _disposed = false;

        public AzureKeyVaultProvider(
            IConfiguration configuration,
            ILogger<AzureKeyVaultProvider> logger,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            SmartAlarmActivitySource activitySource)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _meter = meter ?? throw new ArgumentNullException(nameof(meter));
            _correlationContext = correlationContext ?? throw new ArgumentNullException(nameof(correlationContext));
            _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
            
            _keyVaultUri = _configuration["Azure:KeyVault:Uri"] 
                ?? throw new InvalidOperationException("Azure KeyVault Uri não configurado");

            // Lazy initialization para otimizar performance
            _secretClient = new Lazy<SecretClient>(() => CreateSecretClient());

            _logger.LogInformation("Azure KeyVault Provider initialized for vault: {VaultUri}", _keyVaultUri);
        }

        private SecretClient CreateSecretClient()
        {
            try
            {
                var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
                {
                    ExcludeEnvironmentCredential = false,
                    ExcludeInteractiveBrowserCredential = true,
                    ExcludeAzureCliCredential = false,
                    ExcludeManagedIdentityCredential = false,
                    ExcludeSharedTokenCacheCredential = true,
                    ExcludeVisualStudioCredential = true,
                    ExcludeVisualStudioCodeCredential = true,
                    ExcludeAzurePowerShellCredential = true
                });

                var clientOptions = new SecretClientOptions();

                var client = new SecretClient(new Uri(_keyVaultUri), credential, clientOptions);
                
                _logger.LogInformation("Azure KeyVault SecretClient created successfully for vault: {VaultUri}", _keyVaultUri);
                return client;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Azure KeyVault SecretClient for vault: {VaultUri}", _keyVaultUri);
                throw new InvalidOperationException($"Failed to initialize Azure KeyVault client: {ex.Message}", ex);
            }
        }

        public async Task<string?> GetSecretAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                _logger.LogWarning("GetSecretAsync called with empty or null key");
                return null;
            }

            using var activity = _activitySource.StartActivity("AzureKeyVault.GetSecret");
            activity?.SetTag("keyvault.provider", "azure");
            activity?.SetTag("keyvault.operation", "get");
            activity?.SetTag("keyvault.key", key);
            activity?.SetTag("keyvault.vault_uri", _keyVaultUri);

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "AzureKeyVaultGetSecret",
                new { Key = key, VaultUri = _keyVaultUri });

            var retryCount = 0;
            const int maxRetries = 3;

            while (retryCount <= maxRetries)
            {
                try
                {
                    var response = await _secretClient.Value.GetSecretAsync(key);
                    
                    stopwatch.Stop();
                    _meter.RecordExternalServiceCallDuration(stopwatch.ElapsedMilliseconds, "AzureKeyVault", "GetSecret", true);

                    _logger.LogInformation(LogTemplates.KeyVaultOperationCompleted,
                        "GetSecret",
                        key,
                        stopwatch.ElapsedMilliseconds);

                    activity?.SetStatus(ActivityStatusCode.Ok, "Secret retrieved successfully");
                    activity?.SetTag("keyvault.secret_version", response.Value.Properties.Version);
                    
                    return response.Value.Value;
                }
                catch (RequestFailedException ex) when (ex.Status == 404)
                {
                    stopwatch.Stop();
                    activity?.SetStatus(ActivityStatusCode.Error, $"Secret not found: {key}");
                    _meter.IncrementErrorCount("KEYVAULT", "Azure", "SecretNotFound");

                    _logger.LogWarning("Secret {Key} not found in Azure KeyVault {VaultUri}", key, _keyVaultUri);
                    return null;
                }
                catch (RequestFailedException ex) when (IsRetryableError(ex) && retryCount < maxRetries)
                {
                    retryCount++;
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                    
                    _logger.LogWarning(ex, 
                        "Retryable error occurred accessing Azure KeyVault. Attempt {Attempt}/{MaxAttempts}. Retrying in {Delay}ms. Error: {ErrorCode}", 
                        retryCount, maxRetries + 1, delay.TotalMilliseconds, ex.ErrorCode);
                    
                    await Task.Delay(delay);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    _meter.IncrementErrorCount("KEYVAULT", "Azure", "GetSecretError");

                    _logger.LogError(ex, LogTemplates.QueryFailed,
                        "AzureKeyVaultGetSecret",
                        stopwatch.ElapsedMilliseconds,
                        ex.Message);

                    throw new InvalidOperationException($"Failed to retrieve secret '{key}' from Azure KeyVault: {ex.Message}", ex);
                }
            }

            // Se chegou aqui, todas as tentativas falharam
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, "Max retries exceeded");
            _meter.IncrementErrorCount("KEYVAULT", "Azure", "MaxRetriesExceeded");
            
            var errorMessage = $"Failed to retrieve secret '{key}' from Azure KeyVault after {maxRetries + 1} attempts";
            _logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        public async Task<bool> SetSecretAsync(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                _logger.LogWarning("SetSecretAsync called with empty or null key");
                return false;
            }

            if (value == null)
            {
                _logger.LogWarning("SetSecretAsync called with null value for key: {Key}", key);
                return false;
            }

            using var activity = _activitySource.StartActivity("AzureKeyVault.SetSecret");
            activity?.SetTag("keyvault.provider", "azure");
            activity?.SetTag("keyvault.operation", "set");
            activity?.SetTag("keyvault.key", key);
            activity?.SetTag("keyvault.vault_uri", _keyVaultUri);

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "AzureKeyVaultSetSecret",
                new { Key = key, VaultUri = _keyVaultUri });

            var retryCount = 0;
            const int maxRetries = 3;

            while (retryCount <= maxRetries)
            {
                try
                {
                    var response = await _secretClient.Value.SetSecretAsync(key, value);
                    
                    stopwatch.Stop();
                    _meter.RecordExternalServiceCallDuration(stopwatch.ElapsedMilliseconds, "AzureKeyVault", "SetSecret", true);

                    _logger.LogInformation(LogTemplates.KeyVaultOperationCompleted,
                        "SetSecret",
                        key,
                        stopwatch.ElapsedMilliseconds);

                    activity?.SetStatus(ActivityStatusCode.Ok, "Secret set successfully");
                    activity?.SetTag("keyvault.secret_version", response.Value.Properties.Version);
                    
                    return true;
                }
                catch (RequestFailedException ex) when (IsRetryableError(ex) && retryCount < maxRetries)
                {
                    retryCount++;
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                    
                    _logger.LogWarning(ex, 
                        "Retryable error occurred setting secret in Azure KeyVault. Attempt {Attempt}/{MaxAttempts}. Retrying in {Delay}ms. Error: {ErrorCode}", 
                        retryCount, maxRetries + 1, delay.TotalMilliseconds, ex.ErrorCode);
                    
                    await Task.Delay(delay);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    _meter.IncrementErrorCount("KEYVAULT", "Azure", "SetSecretError");

                    _logger.LogError(ex, LogTemplates.QueryFailed,
                        "AzureKeyVaultSetSecret",
                        stopwatch.ElapsedMilliseconds,
                        ex.Message);

                    throw new InvalidOperationException($"Failed to set secret '{key}' in Azure KeyVault: {ex.Message}", ex);
                }
            }

            // Se chegou aqui, todas as tentativas falharam
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, "Max retries exceeded");
            _meter.IncrementErrorCount("KEYVAULT", "Azure", "MaxRetriesExceeded");
            
            var errorMessage = $"Failed to set secret '{key}' in Azure KeyVault after {maxRetries + 1} attempts";
            _logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        /// <summary>
        /// Determina se um erro é recuperável e pode ser reprocessado
        /// </summary>
        private static bool IsRetryableError(RequestFailedException ex)
        {
            return ex.Status switch
            {
                429 => true, // Too Many Requests
                500 => true, // Internal Server Error
                502 => true, // Bad Gateway
                503 => true, // Service Unavailable
                504 => true, // Gateway Timeout
                _ => false
            };
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                // SecretClient não implementa IDisposable, mas podemos limpar recursos se necessário
                _logger.LogInformation("Azure KeyVault Provider disposed");
                _disposed = true;
            }
        }
    }
}
