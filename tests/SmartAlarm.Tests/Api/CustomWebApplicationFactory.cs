using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Domain.Abstractions;
using SmartAlarm.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.TestHost;
using System.Linq;
using SmartAlarm.Tests.Mocks;

namespace SmartAlarm.Tests.Api
{
    public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                var dict = new System.Collections.Generic.Dictionary<string, string?>
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
                var alarmRepoDescriptors = services.Where(d => d.ServiceType == typeof(IAlarmRepository)).ToList();
                foreach (var descriptor in alarmRepoDescriptors)
                {
                    services.Remove(descriptor);
                }
                services.AddSingleton<IAlarmRepository, InMemoryAlarmRepository>();

                var userRepoDescriptors = services.Where(d => d.ServiceType == typeof(IUserRepository)).ToList();
                foreach (var descriptor in userRepoDescriptors)
                {
                    services.Remove(descriptor);
                }
                services.AddSingleton<IUserRepository, InMemoryUserRepository>();

                var routineRepoDescriptors = services.Where(d => d.ServiceType == typeof(IRoutineRepository)).ToList();
                foreach (var descriptor in routineRepoDescriptors)
                {
                    services.Remove(descriptor);
                }
                services.AddSingleton<IRoutineRepository, InMemoryRoutineRepository>();

                var integrationRepoDescriptors = services.Where(d => d.ServiceType == typeof(IIntegrationRepository)).ToList();
                foreach (var descriptor in integrationRepoDescriptors)
                {
                    services.Remove(descriptor);
                }
                services.AddSingleton<IIntegrationRepository, InMemoryIntegrationRepository>();
                
                // Remover e adicionar o serviço JWT
                var jwtTokenDescriptors = services.Where(d => d.ServiceType == typeof(IJwtTokenService)).ToList();
                foreach (var descriptor in jwtTokenDescriptors)
                {
                    services.Remove(descriptor);
                }
                services.AddSingleton<IJwtTokenService, MockJwtTokenService>();

                // Não remova ou sobrescreva MediatR, Application, Domain, etc.
                // Certifique-se de que o projeto API registra MediatR normalmente.
            });
        }
    }
}
