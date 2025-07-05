using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Infrastructure.Data;
using SmartAlarm.Infrastructure.Repositories;
using SmartAlarm.Infrastructure.Repositories.EntityFramework;
using SmartAlarm.Infrastructure.Services;

namespace SmartAlarm.Infrastructure
{
    /// <summary>
    /// Extension methods for registering infrastructure dependencies.
    /// Supports both in-memory (for testing) and Entity Framework (for production) implementations.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Adds Smart Alarm infrastructure with Entity Framework Core and Oracle database.
        /// </summary>
        public static IServiceCollection AddSmartAlarmInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Add Entity Framework DbContext
            services.AddDbContext<SmartAlarmDbContext>(options =>
            {
                var connectionString = configuration.GetConnectionString("OracleDb");
                if (!string.IsNullOrEmpty(connectionString))
                {
                    options.UseOracle(connectionString);
                }
                else
                {
                    // Fallback to in-memory database for development/testing
                    options.UseInMemoryDatabase("SmartAlarmInMemory");
                }
            });

            // Register Unit of Work
            services.AddScoped<IUnitOfWork, EfUnitOfWork>();

            // Register EF Core repositories
            services.AddScoped<IAlarmRepository, EfAlarmRepository>();
            services.AddScoped<IUserRepository, EfUserRepository>();
            services.AddScoped<IScheduleRepository, EfScheduleRepository>();
            services.AddScoped<IRoutineRepository, EfRoutineRepository>();
            services.AddScoped<IIntegrationRepository, EfIntegrationRepository>();

            // Register infrastructure services
            services.AddScoped<IEmailService, LoggingEmailService>();
            services.AddScoped<INotificationService, LoggingNotificationService>();


            // Mensageria: RabbitMQ local/dev, Mock para outros ambientes

            var useRabbitMq = Microsoft.Extensions.Configuration.ConfigurationBinder.GetValue<bool>(configuration, "Messaging:UseRabbitMq");
            if (useRabbitMq)
                services.AddSingleton<Messaging.IMessagingService, Messaging.RabbitMqMessagingService>();
            else
                services.AddSingleton<Messaging.IMessagingService, Messaging.MockMessagingService>();

            // Storage: MinIO local/dev, Mock para outros ambientes

            var useMinio = Microsoft.Extensions.Configuration.ConfigurationBinder.GetValue<bool>(configuration, "Storage:UseMinio");
            if (useMinio)
                services.AddSingleton<Storage.IStorageService, Storage.MinioStorageService>();
            else
                services.AddSingleton<Storage.IStorageService, Storage.MockStorageService>();

            // KeyVault: HashiCorp Vault local/dev

            var useVault = Microsoft.Extensions.Configuration.ConfigurationBinder.GetValue<bool>(configuration, "KeyVault:UseHashiCorpVault");
            if (useVault)
            {
                Microsoft.Extensions.DependencyInjection.HttpClientFactoryServiceCollectionExtensions.AddHttpClient<KeyVault.HashiCorpVaultProvider>(services, client =>
                {
                    client.BaseAddress = new System.Uri("http://vault:8200");
                });
                services.AddSingleton<KeyVault.IKeyVaultProvider, KeyVault.HashiCorpVaultProvider>();
            }
            else
            {
                services.AddSingleton<KeyVault.IKeyVaultProvider, KeyVault.MockKeyVaultProvider>();
            }

            // Observability (mantém mock, real via OpenTelemetry no Api)
            services.AddSingleton<Observability.ITracingService, Observability.MockTracingService>();
            services.AddSingleton<Observability.IMetricsService, Observability.MockMetricsService>();

            return services;
        }

        /// <summary>
        /// Adds Smart Alarm infrastructure with in-memory implementations for testing.
        /// </summary>
        public static IServiceCollection AddSmartAlarmInfrastructureInMemory(this IServiceCollection services)
        {
            // Register in-memory repositories
            services.AddSingleton<IAlarmRepository, InMemoryAlarmRepository>();
            services.AddSingleton<IUserRepository, InMemoryUserRepository>();
            services.AddSingleton<IScheduleRepository, InMemoryScheduleRepository>();
            services.AddSingleton<IRoutineRepository, InMemoryRoutineRepository>();
            services.AddSingleton<IIntegrationRepository, InMemoryIntegrationRepository>();

            // Register infrastructure services
            services.AddScoped<IEmailService, LoggingEmailService>();
            services.AddScoped<INotificationService, LoggingNotificationService>();

            // Register messaging, storage, tracing, metrics (mock for now)
            services.AddSingleton<Messaging.IMessagingService, Messaging.MockMessagingService>();
            services.AddSingleton<Storage.IStorageService, Storage.MockStorageService>();
            services.AddSingleton<Observability.ITracingService, Observability.MockTracingService>();
            services.AddSingleton<Observability.IMetricsService, Observability.MockMetricsService>();

            return services;
        }

        /// <summary>
        /// Adds Smart Alarm infrastructure with legacy Dapper implementation for Oracle.
        /// Kept for backward compatibility.
        /// </summary>
        public static IServiceCollection AddSmartAlarmInfrastructureDapper(this IServiceCollection services, IConfiguration configuration)
        {
            // Register Dapper-based Oracle implementation for Alarm only
            services.AddScoped<IAlarmRepository>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<AlarmRepository>>();
                var connectionString = configuration.GetConnectionString("OracleDb");
                return new AlarmRepository(connectionString, logger);
            });

            // Use in-memory for other repositories
            services.AddSingleton<IUserRepository, InMemoryUserRepository>();
            services.AddSingleton<IScheduleRepository, InMemoryScheduleRepository>();
            services.AddSingleton<IRoutineRepository, InMemoryRoutineRepository>();
            services.AddSingleton<IIntegrationRepository, InMemoryIntegrationRepository>();

            // Register infrastructure services
            services.AddScoped<IEmailService, LoggingEmailService>();
            services.AddScoped<INotificationService, LoggingNotificationService>();

            // Register messaging, storage, tracing, metrics (mock for now)
            services.AddSingleton<Messaging.IMessagingService, Messaging.MockMessagingService>();
            services.AddSingleton<Storage.IStorageService, Storage.MockStorageService>();
            services.AddSingleton<Observability.ITracingService, Observability.MockTracingService>();
            services.AddSingleton<Observability.IMetricsService, Observability.MockMetricsService>();

            return services;
        }
    }
}
