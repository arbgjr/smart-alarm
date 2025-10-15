using SmartAlarm.Domain.Abstractions;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.KeyVault.Tests.Mocks;

namespace SmartAlarm.KeyVault.Tests.Integration
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                var dict = new System.Collections.Generic.Dictionary<string, string?>
                {
                    {"KeyVault:Enabled", "true"},
                    {"Jwt:Key", "REPLACE_WITH_A_STRONG_SECRET_KEY_32CHARS"},
                    {"Jwt:Issuer", "SmartAlarmIssuer"},
                    {"Jwt:Audience", "SmartAlarmAudience"}
                };

                config.AddInMemoryCollection(dict);
            });

            builder.ConfigureServices(services =>
            {
                // Remover e adicionar o serviço JWT mock
                var jwtTokenDescriptors = services.Where(d => d.ServiceType == typeof(IJwtTokenService)).ToList();
                foreach (var descriptor in jwtTokenDescriptors)
                {
                    services.Remove(descriptor);
                }
                services.AddSingleton<IJwtTokenService, MockJwtTokenService>();
            });
        }
    }
}
