using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Infrastructure.Repositories;

namespace SmartAlarm.Infrastructure
{
    /// <summary>
    /// Extension methods for registering infrastructure dependencies.
    /// </summary>
    public static class DependencyInjection
    {
        public static IServiceCollection AddSmartAlarmInfrastructure(this IServiceCollection services)
        {
            // Register Oracle DB implementation for production
            services.AddScoped<IAlarmRepository>(provider =>
            {
                var configuration = provider.GetRequiredService<IConfiguration>();
                var logger = provider.GetRequiredService<ILogger<AlarmRepository>>();
                var connectionString = configuration.GetConnectionString("OracleDb");
                return new AlarmRepository(connectionString, logger);
            });
            // Keep in-memory for tests/dev
            // services.AddSingleton<IAlarmRepository, InMemoryAlarmRepository>();

            services.AddSingleton<IUserRepository, InMemoryUserRepository>();
            services.AddSingleton<IRoutineRepository, InMemoryRoutineRepository>();
            services.AddSingleton<IIntegrationRepository, InMemoryIntegrationRepository>();
            // Add logging, tracing, metrics, and other infrastructure services here
            return services;
        }
    }
}
