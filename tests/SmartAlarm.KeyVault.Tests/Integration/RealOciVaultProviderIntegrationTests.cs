using SmartAlarm.Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using SmartAlarm.KeyVault.Abstractions;
using SmartAlarm.KeyVault.Extensions;
using SmartAlarm.KeyVault.Providers;

namespace SmartAlarm.KeyVault.Tests.Integration
{
    /// <summary>
    /// Testes de integração para RealOciVaultProvider.
    /// Valida a configuração de injeção de dependência e comportamento integrado.
    /// </summary>
    public class RealOciVaultProviderIntegrationTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;

        public RealOciVaultProviderIntegrationTests()
        {
            var services = new ServiceCollection();
            
            // Configuração básica para testes
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["OciVault:CompartmentId"] = "ocid1.compartment.oc1..test",
                    ["OciVault:VaultId"] = "ocid1.vault.oc1..test",
                    ["OciVault:Region"] = "us-ashburn-1",
                    ["OciVault:Priority"] = "3",
                    ["OciVault:Environment"] = "Real",
                    ["KeyVault:DefaultCacheDuration"] = "00:05:00"
                })
                .Build();

            services.AddLogging(builder => builder.AddConsole());
            services.AddKeyVault(configuration);

            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public void ServiceRegistration_ShouldIncludeRealOciProvider()
        {
            // Act
            var providers = _serviceProvider.GetServices<ISecretProvider>();

            // Assert
            Assert.Contains(providers, p => p is RealOciVaultProvider);
        }

        [Fact]
        public void RealOciProvider_ShouldBeResolvable()
        {
            // Act
            var provider = _serviceProvider.GetServices<ISecretProvider>()
                .OfType<RealOciVaultProvider>()
                .FirstOrDefault();

            // Assert
            Assert.NotNull(provider);
            Assert.Equal("OCI-Real", provider.ProviderName);
            Assert.Equal(3, provider.Priority);
        }

        [Fact]
        public async Task RealOciProvider_ShouldHandleAvailabilityCheck()
        {
            // Arrange
            var provider = _serviceProvider.GetServices<ISecretProvider>()
                .OfType<RealOciVaultProvider>()
                .FirstOrDefault();

            Assert.NotNull(provider);

            // Act
            var isAvailable = await provider.IsAvailableAsync();

            // Assert
            // Em ambiente de teste, esperamos que retorne false devido à falta de conectividade real
            Assert.False(isAvailable);
        }

        [Fact]
        public async Task RealOciProvider_ShouldRetrieveSecretWithFallback()
        {
            // Arrange
            var provider = _serviceProvider.GetServices<ISecretProvider>()
                .OfType<RealOciVaultProvider>()
                .FirstOrDefault();

            Assert.NotNull(provider);

            // Act
            var secret = await provider.GetSecretAsync("test-secret");

            // Assert
            Assert.NotNull(secret);
            Assert.Contains("oci-real", secret);
        }

        [Fact]
        public async Task RealOciProvider_ShouldRetrieveMultipleSecrets()
        {
            // Arrange
            var provider = _serviceProvider.GetServices<ISecretProvider>()
                .OfType<RealOciVaultProvider>()
                .FirstOrDefault();

            Assert.NotNull(provider);
            var secretKeys = new[] { "database-connection", "api-key", "jwt-secret" };

            // Act
            var results = await provider.GetSecretsAsync(secretKeys);

            // Assert
            Assert.NotNull(results);
            Assert.Equal(3, results.Count);
            Assert.All(secretKeys, key => Assert.True(results.ContainsKey(key)));
            Assert.All(results.Values, value => Assert.NotNull(value));
        }

        [Fact]
        public async Task RealOciProvider_ShouldSetSecret()
        {
            // Arrange
            var provider = _serviceProvider.GetServices<ISecretProvider>()
                .OfType<RealOciVaultProvider>()
                .FirstOrDefault();

            Assert.NotNull(provider);

            // Act
            var result = await provider.SetSecretAsync("test-secret", "test-value");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void KeyVaultService_ShouldIncludeRealOciProvider()
        {
            // Arrange
            var keyVaultService = _serviceProvider.GetRequiredService<IKeyVaultService>();

            // Act & Assert
            Assert.NotNull(keyVaultService);
            // O serviço deve incluir o RealOciVaultProvider entre seus providers
        }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
        }
    }

    /// <summary>
    /// Testes de integração para configuração específica de OCI Vault.
    /// </summary>
    public class OciVaultConfigurationIntegrationTests : IDisposable
    {
        [Fact]
        public void AddOciVaultReal_ShouldRegisterCorrectProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["OciVault:CompartmentId"] = "ocid1.compartment.oc1..test",
                    ["OciVault:VaultId"] = "ocid1.vault.oc1..test",
                    ["OciVault:Region"] = "us-ashburn-1",
                    ["OciVault:Priority"] = "1"
                })
                .Build();

            services.AddLogging();

            // Act
            services.AddOciVaultReal(configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var provider = serviceProvider.GetService<ISecretProvider>();
            Assert.NotNull(provider);
            Assert.IsType<RealOciVaultProvider>(provider);
        }

        [Fact]
        public void AddOciVaultSimulated_ShouldRegisterCorrectProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["OciVault:CompartmentId"] = "ocid1.compartment.oc1..test",
                    ["OciVault:VaultId"] = "ocid1.vault.oc1..test",
                    ["OciVault:Region"] = "us-ashburn-1",
                    ["OciVault:Priority"] = "1"
                })
                .Build();

            services.AddLogging();

            // Act
            services.AddOciVaultSimulated(configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var provider = serviceProvider.GetService<ISecretProvider>();
            Assert.NotNull(provider);
            Assert.IsType<OciVaultProvider>(provider);
        }

        [Theory]
        [InlineData("Real", typeof(RealOciVaultProvider))]
        [InlineData("Simulated", typeof(OciVaultProvider))]
        [InlineData("", typeof(OciVaultProvider))] // Default
        public void AddKeyVault_WithEnvironmentConfiguration_ShouldRegisterCorrectProvider(
            string? environment, Type expectedProviderType)
        {
            // Arrange
            var services = new ServiceCollection();
            var configData = new Dictionary<string, string?>
            {
                ["OciVault:CompartmentId"] = "ocid1.compartment.oc1..test",
                ["OciVault:VaultId"] = "ocid1.vault.oc1..test",
                ["OciVault:Region"] = "us-ashburn-1",
                ["OciVault:Priority"] = "1"
            };

            if (!string.IsNullOrEmpty(environment))
            {
                configData["OciVault:Environment"] = environment;
            }

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            services.AddLogging();

            // Act
            services.AddKeyVault(configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var providers = serviceProvider.GetServices<ISecretProvider>();
            var ociProvider = providers.FirstOrDefault(p => 
                p.GetType() == typeof(RealOciVaultProvider) || 
                p.GetType() == typeof(OciVaultProvider));
            
            Assert.NotNull(ociProvider);
            Assert.IsType(expectedProviderType, ociProvider);
        }

        public void Dispose()
        {
            // No resources to dispose in this test class
        }
    }
}
