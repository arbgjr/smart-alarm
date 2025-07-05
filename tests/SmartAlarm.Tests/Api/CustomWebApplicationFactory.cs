using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.TestHost;
using System.Linq;

namespace SmartAlarm.Tests.Api
{
    public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                var dict = new System.Collections.Generic.Dictionary<string, string>
                {
                    {"Jwt:Key", "REPLACE_WITH_A_STRONG_SECRET_KEY_32CHARS"},
                    {"Jwt:Issuer", "SmartAlarmIssuer"},
                    {"Jwt:Audience", "SmartAlarmAudience"}
                };
                config.AddInMemoryCollection(dict);
            });

            builder.ConfigureTestServices(services =>
            {
                // Sobrescreva apenas os repositórios de infraestrutura
                var alarmRepoDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAlarmRepository));
                if (alarmRepoDescriptor != null)
                    services.Remove(alarmRepoDescriptor);
                services.AddSingleton<IAlarmRepository, InMemoryAlarmRepository>();

                var userRepoDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IUserRepository));
                if (userRepoDescriptor != null)
                    services.Remove(userRepoDescriptor);
                services.AddSingleton<IUserRepository, InMemoryUserRepository>();

                var routineRepoDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IRoutineRepository));
                if (routineRepoDescriptor != null)
                    services.Remove(routineRepoDescriptor);
                services.AddSingleton<IRoutineRepository, InMemoryRoutineRepository>();

                var integrationRepoDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IIntegrationRepository));
                if (integrationRepoDescriptor != null)
                    services.Remove(integrationRepoDescriptor);
                services.AddSingleton<IIntegrationRepository, InMemoryIntegrationRepository>();

                // Não remova ou sobrescreva MediatR, Application, Domain, etc.
                // Certifique-se de que o projeto API registra MediatR normalmente.
            });
        }
    }
}
