using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Logging;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;

namespace SmartAlarm.Infrastructure.KeyVault
{
    /// <summary>
    /// Implementação real do provedor Azure Key Vault
    /// </summary>
    public class AzureKeyVaultProvider : IKeyVaultProvider
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AzureKeyVaultProvider> _logger;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly string _keyVaultUri;

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
        }

        public async Task<string?> GetSecretAsync(string key)
        {
            using var activity = _activitySource.StartActivity("AzureKeyVault.GetSecret");
            activity?.SetTag("keyvault.provider", "azure");
            activity?.SetTag("keyvault.operation", "get");
            activity?.SetTag("keyvault.key", key);

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "AzureKeyVaultGetSecret",
                new { Key = key });

            try
            {
                // Implementação real com Azure SDK
                var keyVaultClient = new SecretClient(new Uri(_keyVaultUri), new DefaultAzureCredential());
                var secret = await keyVaultClient.GetSecretAsync(key);
                
                stopwatch.Stop();
                _meter.RecordExternalServiceCallDuration(stopwatch.ElapsedMilliseconds, "AzureKeyVault", "GetSecret", true);

                _logger.LogInformation(LogTemplates.KeyVaultOperationCompleted,
                    "GetSecret",
                    key,
                    stopwatch.ElapsedMilliseconds);

                activity?.SetStatus(ActivityStatusCode.Ok, "Secret retrieved successfully");
                return secret.Value.Value;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("KEYVAULT", "Azure", "GetSecretError");

                _logger.LogError(LogTemplates.QueryFailed,
                    "AzureKeyVaultGetSecret",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                return null;
            }
        }

        public async Task<bool> SetSecretAsync(string key, string value)
        {
            using var activity = _activitySource.StartActivity("AzureKeyVault.SetSecret");
            activity?.SetTag("keyvault.provider", "azure");
            activity?.SetTag("keyvault.operation", "set");
            activity?.SetTag("keyvault.key", key);

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "AzureKeyVaultSetSecret",
                new { Key = key });

            try
            {
                // Implementação real com Azure SDK
                var keyVaultClient = new SecretClient(new Uri(_keyVaultUri), new DefaultAzureCredential());
                await keyVaultClient.SetSecretAsync(key, value);
                
                stopwatch.Stop();
                _meter.RecordExternalServiceCallDuration(stopwatch.ElapsedMilliseconds, "AzureKeyVault", "SetSecret", true);

                _logger.LogInformation(LogTemplates.KeyVaultOperationCompleted,
                    "SetSecret",
                    key,
                    stopwatch.ElapsedMilliseconds);

                activity?.SetStatus(ActivityStatusCode.Ok, "Secret set successfully");
                return true;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("KEYVAULT", "Azure", "SetSecretError");

                _logger.LogError(LogTemplates.QueryFailed,
                    "AzureKeyVaultSetSecret",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                return false;
            }
        }
    }
}
