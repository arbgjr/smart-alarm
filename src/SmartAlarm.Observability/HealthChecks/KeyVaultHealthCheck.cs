using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using VaultSharp;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SmartAlarm.Observability.HealthChecks
{
    /// <summary>
    /// Health check para verificar a conectividade com o KeyVault (HashiCorp Vault)
    /// </summary>
    public class KeyVaultHealthCheck : IHealthCheck
    {
        private readonly IVaultClient _vaultClient;
        private readonly ILogger<KeyVaultHealthCheck> _logger;

        public KeyVaultHealthCheck(IVaultClient vaultClient, ILogger<KeyVaultHealthCheck> logger)
        {
            _vaultClient = vaultClient ?? throw new ArgumentNullException(nameof(vaultClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Iniciando health check do KeyVault");

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                // Tenta verificar o status do Vault
                var healthStatus = await _vaultClient.V1.System.GetHealthStatusAsync();
                
                stopwatch.Stop();

                var healthData = new Dictionary<string, object>
                {
                    ["IsInitialized"] = healthStatus?.Initialized ?? false,
                    ["IsSealed"] = healthStatus?.Sealed ?? true,
                    ["ServerTimeUtc"] = healthStatus?.ServerTimeUtc ?? DateTimeOffset.UtcNow,
                    ["Version"] = healthStatus?.Version ?? "unknown",
                    ["ResponseTime"] = $"{stopwatch.ElapsedMilliseconds}ms",
                    ["Timestamp"] = DateTime.UtcNow
                };

                _logger.LogInformation("Health check do KeyVault concluído com sucesso: {@HealthData}", healthData);

                // Considera saudável se estiver inicializado e não selado
                if (healthStatus?.Initialized == true && healthStatus?.Sealed == false)
                {
                    return HealthCheckResult.Healthy("KeyVault está acessível e operacional", healthData);
                }
                else
                {
                    return HealthCheckResult.Degraded("KeyVault está acessível mas pode ter problemas", null, healthData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha no health check do KeyVault");
                return HealthCheckResult.Unhealthy("Falha na conectividade com o KeyVault", ex);
            }
        }
    }
}
