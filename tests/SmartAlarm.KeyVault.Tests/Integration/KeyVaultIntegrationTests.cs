using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartAlarm.KeyVault.Abstractions;
using SmartAlarm.KeyVault.Extensions;
using Xunit;

namespace SmartAlarm.KeyVault.Tests.Integration
{
    public class KeyVaultIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public KeyVaultIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task KeyVaultService_ShouldBeRegisteredInDI()
        {
            // Arrange
            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new[]
                    {
                        new System.Collections.Generic.KeyValuePair<string, string?>("KeyVault:Enabled", "true"),
                        new System.Collections.Generic.KeyValuePair<string, string?>("HashiCorpVault:ServerAddress", "http://localhost:8200"),
                        new System.Collections.Generic.KeyValuePair<string, string?>("HashiCorpVault:Token", "test-token")
                    });
                });
                
                builder.ConfigureServices(services =>
                {
                    // Remove existing KeyVault registration to avoid conflicts
                    var serviceDescriptor = new ServiceDescriptor(typeof(IKeyVaultService), typeof(IKeyVaultService), ServiceLifetime.Singleton);
                    services.Remove(serviceDescriptor);
                    
                    // Add KeyVault services
                    services.AddKeyVault(services.BuildServiceProvider().GetRequiredService<IConfiguration>());
                });
            }).CreateClient();

            // Act & Assert
            using var scope = _factory.Services.CreateScope();
            var keyVaultService = scope.ServiceProvider.GetService<IKeyVaultService>();
            keyVaultService.Should().NotBeNull();
        }

        [Fact]
        public async Task GetAvailableProvidersAsync_ShouldReturnEmptyList_WhenNoProvidersAvailable()
        {
            // Arrange
            using var scope = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new[]
                    {
                        new System.Collections.Generic.KeyValuePair<string, string?>("KeyVault:Enabled", "true"),
                        new System.Collections.Generic.KeyValuePair<string, string?>("HashiCorpVault:ServerAddress", "http://localhost:8200"),
                        new System.Collections.Generic.KeyValuePair<string, string?>("HashiCorpVault:Token", "test-token")
                    });
                });
                
                builder.ConfigureServices(services =>
                {
                    services.AddKeyVault(services.BuildServiceProvider().GetRequiredService<IConfiguration>());
                });
            }).Services.CreateScope();

            var keyVaultService = scope.ServiceProvider.GetRequiredService<IKeyVaultService>();

            // Act
            var availableProviders = await keyVaultService.GetAvailableProvidersAsync();

            // Assert
            availableProviders.Should().BeEmpty(); // No providers should be available without actual vault instances
        }

        [Fact]
        public async Task GetSecretAsync_ShouldReturnNull_WhenSecretNotFound()
        {
            // Arrange
            using var scope = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new[]
                    {
                        new System.Collections.Generic.KeyValuePair<string, string?>("KeyVault:Enabled", "true"),
                        new System.Collections.Generic.KeyValuePair<string, string?>("HashiCorpVault:ServerAddress", "http://localhost:8200"),
                        new System.Collections.Generic.KeyValuePair<string, string?>("HashiCorpVault:Token", "test-token")
                    });
                });
                
                builder.ConfigureServices(services =>
                {
                    services.AddKeyVault(services.BuildServiceProvider().GetRequiredService<IConfiguration>());
                });
            }).Services.CreateScope();

            var keyVaultService = scope.ServiceProvider.GetRequiredService<IKeyVaultService>();

            // Act
            var result = await keyVaultService.GetSecretAsync("non-existent-secret");

            // Assert
            result.Should().BeNull(); // Should return null when no providers are available
        }

        [Fact]
        public async Task SetSecretAsync_ShouldReturnFalse_WhenNoProvidersAvailable()
        {
            // Arrange
            using var scope = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new[]
                    {
                        new System.Collections.Generic.KeyValuePair<string, string?>("KeyVault:Enabled", "true"),
                        new System.Collections.Generic.KeyValuePair<string, string?>("HashiCorpVault:ServerAddress", "http://localhost:8200"),
                        new System.Collections.Generic.KeyValuePair<string, string?>("HashiCorpVault:Token", "test-token")
                    });
                });
                
                builder.ConfigureServices(services =>
                {
                    services.AddKeyVault(services.BuildServiceProvider().GetRequiredService<IConfiguration>());
                });
            }).Services.CreateScope();

            var keyVaultService = scope.ServiceProvider.GetRequiredService<IKeyVaultService>();

            // Act
            var result = await keyVaultService.SetSecretAsync("test-secret", "test-value");

            // Assert
            result.Should().BeFalse(); // Should return false when no providers are available
        }
    }
}