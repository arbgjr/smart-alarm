using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartAlarm.Domain.Abstractions;
using SmartAlarm.KeyVault.Abstractions;
using SmartAlarm.KeyVault.Extensions;
using SmartAlarm.KeyVault.Tests.Mocks;
using Xunit;
using System.Linq;

namespace SmartAlarm.KeyVault.Tests.Integration
{
    public class KeyVaultIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public KeyVaultIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void KeyVaultService_ShouldBeRegisteredInDI()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act & Assert
            using var scope = _factory.Services.CreateScope();
            var keyVaultService = scope.ServiceProvider.GetService<IKeyVaultService>();
            keyVaultService.Should().NotBeNull();
            keyVaultService.Should().BeAssignableTo<IKeyVaultService>();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetAvailableProvidersAsync_ShouldReturnEmptyList_WhenNoProvidersAvailable()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();

            var keyVaultService = scope.ServiceProvider.GetRequiredService<IKeyVaultService>();

            // Act
            var availableProviders = await keyVaultService.GetAvailableProvidersAsync();

            // Assert
            // Quando o KeyVault está ativo, mas sem provedores configurados corretamente
            availableProviders.Should().BeEmpty();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetSecretAsync_ShouldReturnNull_WhenSecretNotFound()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();

            var keyVaultService = scope.ServiceProvider.GetRequiredService<IKeyVaultService>();

            // Act
            var result = await keyVaultService.GetSecretAsync("non-existent-secret");

            // Assert
            result.Should().BeNull(); // Should return null when no providers are available
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task SetSecretAsync_ShouldReturnFalse_WhenNoProvidersAvailable()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();

            var keyVaultService = scope.ServiceProvider.GetRequiredService<IKeyVaultService>();

            // Act
            var result = await keyVaultService.SetSecretAsync("test-secret", "test-value");

            // Assert
            result.Should().BeFalse(); // Should return false when no providers are available
        }
    }
}