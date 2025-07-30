using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Infrastructure.Data;
using SmartAlarm.Infrastructure.Repositories;
using SmartAlarm.Infrastructure.Repositories.EntityFramework;
using SmartAlarm.Infrastructure.Services;
using SmartAlarm.Infrastructure.Security;
using SmartAlarm.Domain.Abstractions;

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

            // Registro condicional de provider do banco
            var dbProvider = configuration.GetValue<string>("Database:Provider");
            if (string.Equals(dbProvider, "PostgreSQL", StringComparison.OrdinalIgnoreCase))
            {
                var pgConn = configuration.GetConnectionString("PostgresDb");
                services.AddDbContext<SmartAlarmDbContext>(options =>
                {
                    if (!string.IsNullOrEmpty(pgConn))
                        options.UseNpgsql(pgConn);
                    else
                        options.UseInMemoryDatabase("SmartAlarmInMemory");
                });
                // UnitOfWork e repositórios específicos para PostgreSQL
                services.AddScoped<IUnitOfWork, EfUnitOfWork>();
                services.AddScoped<IAlarmRepository, EfAlarmRepositoryPostgres>();
                services.AddScoped<IUserRepository, EfUserRepositoryPostgres>();
                services.AddScoped<IScheduleRepository, EfScheduleRepositoryPostgres>();
                services.AddScoped<IRoutineRepository, EfRoutineRepositoryPostgres>();
                services.AddScoped<IIntegrationRepository, EfIntegrationRepositoryPostgres>();
                services.AddScoped<IHolidayRepository, EfHolidayRepository>();
                services.AddScoped<IUserHolidayPreferenceRepository, EfUserHolidayPreferenceRepository>();
                services.AddScoped<IExceptionPeriodRepository, EfExceptionPeriodRepository>();
            }
            else
            {
                var oracleConn = configuration.GetConnectionString("OracleDb");
                services.AddDbContext<SmartAlarmDbContext>(options =>
                {
                    if (!string.IsNullOrEmpty(oracleConn))
                        options.UseOracle(oracleConn);
                    else
                        options.UseInMemoryDatabase("SmartAlarmInMemory");
                });
                // UnitOfWork e repositórios padrão (Oracle)
                services.AddScoped<IUnitOfWork, EfUnitOfWork>();
                services.AddScoped<IAlarmRepository, EfAlarmRepository>();
                services.AddScoped<IUserRepository, EfUserRepository>();
                services.AddScoped<IScheduleRepository, EfScheduleRepository>();
                services.AddScoped<IRoutineRepository, EfRoutineRepository>();
                services.AddScoped<IIntegrationRepository, EfIntegrationRepository>();
                services.AddScoped<IHolidayRepository, EfHolidayRepository>();
                services.AddScoped<IUserHolidayPreferenceRepository, EfUserHolidayPreferenceRepository>();
                services.AddScoped<IExceptionPeriodRepository, EfExceptionPeriodRepository>();
            }

            return services.AddCommonInfrastructureServices();
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

            // Register EntityFramework repositories for complex entities that don't have in-memory implementations
            services.AddScoped<IHolidayRepository, EfHolidayRepository>();
            services.AddScoped<IUserHolidayPreferenceRepository, EfUserHolidayPreferenceRepository>();
            services.AddScoped<IExceptionPeriodRepository, EfExceptionPeriodRepository>();

            return services.AddCommonInfrastructureServices();
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
                var connectionString = configuration.GetConnectionString("OracleDb") ?? throw new InvalidOperationException("OracleDb connection string not found");
                return new AlarmRepository(connectionString, logger);
            });

            // Use in-memory for other repositories
            services.AddSingleton<IUserRepository, InMemoryUserRepository>();
            services.AddSingleton<IScheduleRepository, InMemoryScheduleRepository>();
            services.AddSingleton<IRoutineRepository, InMemoryRoutineRepository>();
            services.AddSingleton<IIntegrationRepository, InMemoryIntegrationRepository>();

            return services.AddCommonInfrastructureServices();
        }

        /// <summary>
        /// Registra serviços comuns da infraestrutura
        /// </summary>
        private static IServiceCollection AddCommonInfrastructureServices(this IServiceCollection services)
        {
            // Register infrastructure services
            services.AddScoped<IEmailService, LoggingEmailService>();
            services.AddScoped<INotificationService, LoggingNotificationService>();
            services.AddScoped<IFileParser, CsvFileParser>();

            // Register repositories
            services.AddSingleton<Domain.Repositories.IWebhookRepository, Repositories.InMemoryWebhookRepository>();

            // Register security services with environment-based configuration
            services.AddScoped<IJwtTokenService>(provider =>
            {
                var keyVault = provider.GetRequiredService<SmartAlarm.KeyVault.Abstractions.IKeyVaultService>();
                var logger = provider.GetRequiredService<ILogger<JwtTokenService>>();
                var tokenStorage = provider.GetRequiredService<Security.ITokenStorage>();

                return new JwtTokenService(keyVault, logger, tokenStorage);
            });

            // Token storage with distributed revocation support
            services.AddScoped<Security.ITokenStorage>(provider =>
            {
                var config = provider.GetRequiredService<IConfiguration>();
                var environment = config["Environment"] ?? config["ASPNETCORE_ENVIRONMENT"] ?? "Development";
                var logger = provider.GetRequiredService<ILogger<Security.InMemoryTokenStorage>>();

                return environment switch
                {
                    "Production" => new Security.DistributedTokenStorage(
                        provider.GetRequiredService<ILogger<Security.DistributedTokenStorage>>(),
                        config.GetConnectionString("Redis") ?? throw new InvalidOperationException("Redis connection string required for production")
                    ),
                    "Staging" => new Security.DistributedTokenStorage(
                        provider.GetRequiredService<ILogger<Security.DistributedTokenStorage>>(),
                        config.GetConnectionString("Redis") ?? "localhost:6379,password=smartalarm123"
                    ),
                    _ => new Security.InMemoryTokenStorage(logger) // Para desenvolvimento e testes
                };
            });

            // JWT Blocklist service with distributed Redis support
            services.AddScoped<Security.IJwtBlocklistService>(provider =>
            {
                var config = provider.GetRequiredService<IConfiguration>();
                var environment = config["Environment"] ?? config["ASPNETCORE_ENVIRONMENT"] ?? "Development";
                var logger = provider.GetRequiredService<ILogger<Security.RedisJwtBlocklistService>>();
                var meter = provider.GetRequiredService<SmartAlarm.Observability.Metrics.SmartAlarmMeter>();
                var correlationContext = provider.GetRequiredService<SmartAlarm.Observability.Context.ICorrelationContext>();
                var activitySource = provider.GetRequiredService<SmartAlarm.Observability.Tracing.SmartAlarmActivitySource>();

                return environment switch
                {
                    "Production" => new Security.RedisJwtBlocklistService(
                        logger, meter, correlationContext, activitySource,
                        config.GetConnectionString("Redis") ?? throw new InvalidOperationException("Redis connection string required for JWT Blocklist in production")
                    ),
                    "Staging" => new Security.RedisJwtBlocklistService(
                        logger, meter, correlationContext, activitySource,
                        config.GetConnectionString("Redis") ?? "localhost:6379,password=smartalarm123"
                    ),
                    _ => new Security.RedisJwtBlocklistService(
                        logger, meter, correlationContext, activitySource,
                        config.GetConnectionString("Redis") ?? "localhost:6379,password=smartalarm123"
                    )
                };
            });

            // Register messaging service with production-ready configuration
            services.AddScoped<Messaging.IMessagingService>(provider =>
            {
                var config = provider.GetRequiredService<IConfiguration>();
                var environment = config["Environment"] ?? config["ASPNETCORE_ENVIRONMENT"] ?? "Development";
                var logger = provider.GetRequiredService<ILogger<Messaging.RabbitMqMessagingService>>();
                var meter = provider.GetRequiredService<SmartAlarm.Observability.Metrics.SmartAlarmMeter>();
                var correlationContext = provider.GetRequiredService<SmartAlarm.Observability.Context.ICorrelationContext>();
                var activitySource = provider.GetRequiredService<SmartAlarm.Observability.Tracing.SmartAlarmActivitySource>();

                // Todos os ambientes usam a mesma implementação RabbitMQ
                // A diferença está na configuração de SSL/clustering via variáveis de ambiente
                return new Messaging.RabbitMqMessagingService(
                    logger, meter, correlationContext, activitySource
                );
            });

            // Register storage service with smart provider that detects MinIO availability
            services.AddScoped<Storage.IStorageService>(provider =>
            {
                var config = provider.GetRequiredService<IConfiguration>();
                var environment = config["Environment"] ?? config["ASPNETCORE_ENVIRONMENT"] ?? "Development";
                var configResolver = provider.GetRequiredService<SmartAlarm.Infrastructure.Configuration.IConfigurationResolver>();
                var meter = provider.GetRequiredService<SmartAlarm.Observability.Metrics.SmartAlarmMeter>();
                var correlationContext = provider.GetRequiredService<SmartAlarm.Observability.Context.ICorrelationContext>();
                var activitySource = provider.GetRequiredService<SmartAlarm.Observability.Tracing.SmartAlarmActivitySource>();

                return environment switch
                {
                    "Production" => new Storage.OciObjectStorageService(
                        config,
                        provider.GetRequiredService<ILogger<Storage.OciObjectStorageService>>(),
                        meter,
                        activitySource,
                        provider.GetRequiredService<HttpClient>()
                    ),
                    // Para Development/Staging, usa SmartStorageService que detecta MinIO automaticamente
                    _ => new Storage.SmartStorageService(
                        provider.GetRequiredService<ILogger<Storage.SmartStorageService>>(),
                        provider.GetRequiredService<ILogger<Storage.MinioStorageService>>(),
                        provider.GetRequiredService<ILogger<Storage.MockStorageService>>(),
                        configResolver,
                        meter,
                        correlationContext,
                        activitySource
                    )
                };
            });

            // Register observability services with environment-based implementations
            services.AddScoped<Observability.ITracingService>(provider =>
            {
                var config = provider.GetRequiredService<IConfiguration>();
                var environment = config["Environment"] ?? config["ASPNETCORE_ENVIRONMENT"] ?? "Development";

                return environment switch
                {
                    "Production" => new Observability.OpenTelemetryTracingService(
                        provider.GetRequiredService<SmartAlarm.Observability.Tracing.SmartAlarmActivitySource>(),
                        provider.GetRequiredService<ILogger<Observability.OpenTelemetryTracingService>>()
                    ),
                    "Staging" => new Observability.OpenTelemetryTracingService(
                        provider.GetRequiredService<SmartAlarm.Observability.Tracing.SmartAlarmActivitySource>(),
                        provider.GetRequiredService<ILogger<Observability.OpenTelemetryTracingService>>()
                    ),
                    _ => new Observability.MockTracingService(
                        provider.GetRequiredService<ILogger<Observability.MockTracingService>>()
                    )
                };
            });

            services.AddScoped<Observability.IMetricsService>(provider =>
            {
                var config = provider.GetRequiredService<IConfiguration>();
                var environment = config["Environment"] ?? config["ASPNETCORE_ENVIRONMENT"] ?? "Development";

                return environment switch
                {
                    "Production" => new Observability.OpenTelemetryMetricsService(
                        provider.GetRequiredService<SmartAlarm.Observability.Metrics.SmartAlarmMeter>(),
                        provider.GetRequiredService<ILogger<Observability.OpenTelemetryMetricsService>>()
                    ),
                    "Staging" => new Observability.OpenTelemetryMetricsService(
                        provider.GetRequiredService<SmartAlarm.Observability.Metrics.SmartAlarmMeter>(),
                        provider.GetRequiredService<ILogger<Observability.OpenTelemetryMetricsService>>()
                    ),
                    _ => new Observability.MockMetricsService(
                        provider.GetRequiredService<ILogger<Observability.MockMetricsService>>()
                    )
                };
            });

            // Note: Observability services (SmartAlarmActivitySource and SmartAlarmMeter) are handled by SmartAlarm.Observability package
            // They are registered via AddSmartAlarmObservability() in the API startup configuration, not here in Infrastructure layer

            return services;
        }
    }
}
