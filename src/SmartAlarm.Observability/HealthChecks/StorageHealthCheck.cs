using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Minio;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SmartAlarm.Observability.HealthChecks
{
    /// <summary>
    /// Health check para verificar a conectividade com o storage (MinIO/OCI Object Storage)
    /// </summary>
    public class StorageHealthCheck : IHealthCheck
    {
        private readonly IMinioClient _minioClient;
        private readonly ILogger<StorageHealthCheck> _logger;

        public StorageHealthCheck(IMinioClient minioClient, ILogger<StorageHealthCheck> logger)
        {
            _minioClient = minioClient ?? throw new ArgumentNullException(nameof(minioClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Iniciando health check do storage");

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                // Tenta listar buckets para verificar conectividade
                var buckets = await _minioClient.ListBucketsAsync(cancellationToken);
                
                stopwatch.Stop();

                var healthData = new Dictionary<string, object>
                {
                    ["BucketsCount"] = buckets?.Buckets?.Count ?? 0,
                    ["ResponseTime"] = $"{stopwatch.ElapsedMilliseconds}ms",
                    ["Endpoint"] = _minioClient.Config?.Endpoint ?? "unknown",
                    ["IsSecure"] = _minioClient.Config?.Secure ?? false,
                    ["Timestamp"] = DateTime.UtcNow
                };

                _logger.LogInformation("Health check do storage concluído com sucesso: {@HealthData}", healthData);

                return HealthCheckResult.Healthy("Storage está acessível", healthData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha no health check do storage");
                return HealthCheckResult.Unhealthy("Falha na conectividade com o storage", ex);
            }
        }
    }
}
