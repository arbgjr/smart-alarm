using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SmartAlarm.Observability.HealthChecks
{
    /// <summary>
    /// Health check para verificar a conectividade com o message queue (RabbitMQ)
    /// </summary>
    public class MessageQueueHealthCheck : IHealthCheck
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly ILogger<MessageQueueHealthCheck> _logger;

        public MessageQueueHealthCheck(IConnectionFactory connectionFactory, ILogger<MessageQueueHealthCheck> logger)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Iniciando health check do message queue");

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                using var connection = _connectionFactory.CreateConnection();
                using var channel = connection.CreateModel();
                
                // Verifica se consegue declarar uma queue temporária
                var queueName = $"health-check-{Guid.NewGuid()}";
                channel.QueueDeclare(queueName, false, false, true, null);
                channel.QueueDelete(queueName);
                
                stopwatch.Stop();

                var healthData = new Dictionary<string, object>
                {
                    ["IsOpen"] = connection.IsOpen,
                    ["HostName"] = _connectionFactory.Uri?.Host ?? "unknown",
                    ["Port"] = _connectionFactory.Uri?.Port ?? 0,
                    ["VirtualHost"] = _connectionFactory.VirtualHost,
                    ["UserName"] = _connectionFactory.UserName,
                    ["ResponseTime"] = $"{stopwatch.ElapsedMilliseconds}ms",
                    ["LocalPort"] = connection.LocalPort,
                    ["RemotePort"] = connection.RemotePort,
                    ["Timestamp"] = DateTime.UtcNow
                };

                _logger.LogInformation("Health check do message queue concluído com sucesso: {@HealthData}", healthData);

                return HealthCheckResult.Healthy("Message queue está acessível", healthData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha no health check do message queue");
                return HealthCheckResult.Unhealthy("Falha na conectividade com o message queue", ex);
            }
        }
    }
}
