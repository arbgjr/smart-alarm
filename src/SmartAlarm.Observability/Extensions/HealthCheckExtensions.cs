using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using SmartAlarm.Observability.HealthChecks;
using System;

namespace SmartAlarm.Observability.Extensions
{
    /// <summary>
    /// Extensões para configurar health checks no Smart Alarm
    /// </summary>
    public static class HealthCheckExtensions
    {
        /// <summary>
        /// Adiciona health checks configurados para o Smart Alarm
        /// </summary>
        /// <param name="services">Coleção de serviços</param>
        /// <param name="configuration">Configuração da aplicação</param>
        /// <returns>Coleção de serviços configurada</returns>
        public static IServiceCollection AddSmartAlarmHealthChecks(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var healthChecksBuilder = services.AddHealthChecks();

            // Health check básico sempre habilitado
            healthChecksBuilder.AddCheck<SmartAlarmHealthCheck>(
                "smartalarm_basic",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "basic", "smartalarm" });

            // Health check do banco de dados
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrEmpty(connectionString))
            {
                healthChecksBuilder.AddCheck<DatabaseHealthCheck>(
                    "database",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: new[] { "database", "infrastructure" });
            }

            // Health check do storage (MinIO)
            var storageEnabled = configuration.GetValue<bool>("Storage:Enabled", false);
            if (storageEnabled)
            {
                healthChecksBuilder.AddCheck<StorageHealthCheck>(
                    "storage",
                    failureStatus: HealthStatus.Degraded,
                    tags: new[] { "storage", "infrastructure" });
            }

            // Health check do KeyVault
            var vaultEnabled = configuration.GetValue<bool>("Vault:Enabled", false);
            if (vaultEnabled)
            {
                healthChecksBuilder.AddCheck<KeyVaultHealthCheck>(
                    "keyvault",
                    failureStatus: HealthStatus.Degraded,
                    tags: new[] { "keyvault", "security" });
            }

            // Health check do message queue
            var messagingEnabled = configuration.GetValue<bool>("Messaging:Enabled", false);
            if (messagingEnabled)
            {
                healthChecksBuilder.AddCheck<MessageQueueHealthCheck>(
                    "messagequeue",
                    failureStatus: HealthStatus.Degraded,
                    tags: new[] { "messaging", "infrastructure" });
            }

            return services;
        }

        /// <summary>
        /// Adiciona health checks básicos essenciais
        /// </summary>
        /// <param name="services">Coleção de serviços</param>
        /// <returns>Coleção de serviços configurada</returns>
        public static IServiceCollection AddBasicHealthChecks(this IServiceCollection services)
        {
            return services.AddHealthChecks()
                .AddCheck<SmartAlarmHealthCheck>(
                    "smartalarm_basic",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: new[] { "basic", "ready" })
                .Services;
        }

        /// <summary>
        /// Adiciona health checks para readiness (dependências externas)
        /// </summary>
        /// <param name="services">Coleção de serviços</param>
        /// <param name="configuration">Configuração da aplicação</param>
        /// <returns>Coleção de serviços configurada</returns>
        public static IServiceCollection AddReadinessHealthChecks(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var healthChecksBuilder = services.AddHealthChecks();

            // Adiciona checks de dependências críticas
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrEmpty(connectionString))
            {
                healthChecksBuilder.AddCheck<DatabaseHealthCheck>(
                    "database_readiness",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: new[] { "readiness", "database" });
            }

            return services;
        }
    }
}
