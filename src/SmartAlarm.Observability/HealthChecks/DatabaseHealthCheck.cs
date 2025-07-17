using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace SmartAlarm.Observability.HealthChecks
{
    /// <summary>
    /// Health check para verificar a conectividade com o banco de dados
    /// </summary>
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;
        private readonly ILogger<DatabaseHealthCheck> _logger;

        public DatabaseHealthCheck(string connectionString, ILogger<DatabaseHealthCheck> logger)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Iniciando health check do banco de dados");

                using var connection = new NpgsqlConnection(_connectionString);
                
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                await connection.OpenAsync(cancellationToken);
                
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                var result = await command.ExecuteScalarAsync(cancellationToken);
                
                stopwatch.Stop();

                var healthData = new Dictionary<string, object>
                {
                    ["ConnectionState"] = connection.State.ToString(),
                    ["ResponseTime"] = $"{stopwatch.ElapsedMilliseconds}ms",
                    ["ServerVersion"] = connection.ServerVersion,
                    ["Database"] = connection.Database,
                    ["Host"] = connection.Host,
                    ["Port"] = connection.Port,
                    ["Timestamp"] = DateTime.UtcNow
                };

                _logger.LogInformation("Health check do banco de dados concluído com sucesso: {@HealthData}", healthData);

                return HealthCheckResult.Healthy("Banco de dados está acessível", healthData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha no health check do banco de dados");
                return HealthCheckResult.Unhealthy("Falha na conectividade com o banco de dados", ex);
            }
        }
    }
}
