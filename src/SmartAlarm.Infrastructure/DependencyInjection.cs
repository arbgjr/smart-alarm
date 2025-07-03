using Microsoft.Extensions.DependencyInjection;
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
            services.AddSingleton<IAlarmRepository, InMemoryAlarmRepository>();
            services.AddSingleton<IUserRepository, InMemoryUserRepository>();
            services.AddSingleton<IRoutineRepository, InMemoryRoutineRepository>();
            services.AddSingleton<IIntegrationRepository, InMemoryIntegrationRepository>();
            // Add logging, tracing, metrics, and other infrastructure services here
            return services;
        }
    }
}
