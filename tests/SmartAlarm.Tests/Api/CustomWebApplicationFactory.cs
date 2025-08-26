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
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.ValueObjects;
using SmartAlarm.KeyVault.Abstractions;
using SmartAlarm.Infrastructure.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

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
                    {"Jwt:Secret", "REPLACE_WITH_A_STRONG_SECRET_KEY_32CHARS"},
                    {"Jwt:Issuer", "SmartAlarmIssuer"},
                    {"Jwt:Audience", "SmartAlarmAudience"},
                    {"Environment", "Testing"},
                    {"ConnectionStrings:DefaultConnection", "InMemory"},
                    {"KeyVault:UseKeyVault", "false"},
                    {"Observability:UseTracing", "false"},
                    {"Observability:UseMetrics", "false"}
                };
                config.AddInMemoryCollection(dict);
            });

            builder.ConfigureTestServices(services =>
            {
                // Remove external dependencies that require external services
                RemoveService<IKeyVaultService>(services);
                services.AddSingleton<IKeyVaultService>(provider =>
                {
                    var mock = new Mock<IKeyVaultService>();
                    mock.Setup(x => x.GetSecretAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((string?)null);
                    return mock.Object;
                });

                RemoveService<IConfigurationResolver>(services);
                services.AddSingleton<IConfigurationResolver>(provider =>
                {
                    var config = provider.GetRequiredService<IConfiguration>();
                    var keyVault = provider.GetRequiredService<IKeyVaultService>();
                    var logger = provider.GetRequiredService<ILogger<ConfigurationResolver>>();
                    return new ConfigurationResolver(config, keyVault, logger);
                });

                // Add in-memory cache instead of Redis
                RemoveService<IDistributedCache>(services);
                services.AddSingleton<IDistributedCache, MemoryDistributedCache>();

                // Replace repositories with in-memory versions
                RemoveService<IAlarmRepository>(services);
                services.AddSingleton<IAlarmRepository, InMemoryAlarmRepository>();

                RemoveService<IUserRepository>(services);
                var userRepository = new InMemoryUserRepository();
                // Seed test data
                SeedTestUsers(userRepository);
                services.AddSingleton<IUserRepository>(userRepository);

                RemoveService<IRoutineRepository>(services);
                services.AddSingleton<IRoutineRepository, InMemoryRoutineRepository>();

                RemoveService<IIntegrationRepository>(services);
                services.AddSingleton<IIntegrationRepository, InMemoryIntegrationRepository>();
                
                // Mock JWT service
                RemoveService<IJwtTokenService>(services);
                services.AddSingleton<IJwtTokenService, MockJwtTokenService>();

                // Mock FIDO2 service
                RemoveService<IFido2Service>(services);
                services.AddSingleton<IFido2Service>(provider =>
                {
                    var mock = new Mock<IFido2Service>();
                    mock.Setup(x => x.CreateCredentialRequestAsync(It.IsAny<User>(), It.IsAny<string>()))
                        .ReturnsAsync("test-challenge");
                    mock.Setup(x => x.CompleteCredentialRegistrationAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
                        .ReturnsAsync(true);
                    mock.Setup(x => x.CreateAuthenticationRequestAsync(It.IsAny<string>()))
                        .ReturnsAsync("test-challenge");
                    mock.Setup(x => x.CompleteAuthenticationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                        .ReturnsAsync((User?)null);
                    mock.Setup(x => x.GetUserCredentialsAsync(It.IsAny<Guid>()))
                        .ReturnsAsync(new List<UserCredential>());
                    mock.Setup(x => x.RemoveCredentialAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                        .ReturnsAsync(true);
                    return mock.Object;
                });
            });
        }

        private void RemoveService<T>(IServiceCollection services)
        {
            var descriptors = services.Where(d => d.ServiceType == typeof(T)).ToList();
            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }
        }

        private void SeedTestUsers(InMemoryUserRepository userRepository)
        {
            // Create test user for authentication tests
            var testUser = new User(
                Guid.Parse("12345678-1234-1234-1234-123456789012"),
                new Name("Test Admin"),
                new Email("admin@smartalarm.com")
            );

            // Create test user with standard credentials for testing
            var standardUser = new User(
                Guid.Parse("12345678-1234-1234-1234-123456789013"),
                new Name("Test User"),
                new Email("user@smartalarm.com")
            );

            userRepository.AddAsync(testUser).Wait();
            userRepository.AddAsync(standardUser).Wait();
        }
    }
}
