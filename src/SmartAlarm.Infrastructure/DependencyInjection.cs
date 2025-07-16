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
            
            // Register security services
            services.AddScoped<IJwtTokenService, SimpleJwtTokenService>();

            // Register messaging, storage, tracing, metrics (mock for now)
            services.AddSingleton<Messaging.IMessagingService, Messaging.MockMessagingService>();
            services.AddSingleton<Storage.IStorageService, Storage.MockStorageService>();
            services.AddSingleton<Observability.ITracingService, Observability.MockTracingService>();
            services.AddSingleton<Observability.IMetricsService, Observability.MockMetricsService>();

            return services;
        }
    }
}
