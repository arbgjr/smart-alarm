using SmartAlarm.Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SmartAlarm.KeyVault.Abstractions;
using SmartAlarm.KeyVault.Configuration;
using SmartAlarm.KeyVault.Services;
using Xunit;

namespace SmartAlarm.KeyVault.Tests.Services
{
    public class KeyVaultServiceTests
    {
        private readonly Mock<IOptions<KeyVaultOptions>> _optionsMock;
        private readonly Mock<ILogger<KeyVaultService>> _loggerMock;
        private readonly KeyVaultOptions _options;

        public KeyVaultServiceTests()
        {
            _options = new KeyVaultOptions
            {
                Enabled = true,
                EnableCaching = false,
                EnableDetailedLogging = true,
                RetryAttempts = 1,
                RetryDelayMs = 100,
                UseExponentialBackoff = false
            };
            
            _optionsMock = new Mock<IOptions<KeyVaultOptions>>();
            _optionsMock.Setup(o => o.Value).Returns(_options);
            
            _loggerMock = new Mock<ILogger<KeyVaultService>>();
        }

        [Fact]
        public async Task GetSecretAsync_WithValidProviders_ShouldReturnSecret()
        {
            // Arrange
            var mockProvider1 = new Mock<ISecretProvider>();
            mockProvider1.Setup(p => p.ProviderName).Returns("Provider1");
            mockProvider1.Setup(p => p.Priority).Returns(2);
            mockProvider1.Setup(p => p.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var mockProvider2 = new Mock<ISecretProvider>();
            mockProvider2.Setup(p => p.ProviderName).Returns("Provider2");
            mockProvider2.Setup(p => p.Priority).Returns(1);
            mockProvider2.Setup(p => p.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            mockProvider2.Setup(p => p.GetSecretAsync("test-key", It.IsAny<CancellationToken>())).ReturnsAsync("test-value");

            var providers = new List<ISecretProvider> { mockProvider1.Object, mockProvider2.Object };
            var service = new KeyVaultService(providers, _optionsMock.Object, _loggerMock.Object);

            // Act
            var result = await service.GetSecretAsync("test-key");

            // Assert
            result.Should().Be("test-value");
            mockProvider2.Verify(p => p.GetSecretAsync("test-key", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetSecretAsync_WithEmptySecretKey_ShouldReturnNull()
        {
            // Arrange
            var providers = new List<ISecretProvider>();
            var service = new KeyVaultService(providers, _optionsMock.Object, _loggerMock.Object);

            // Act
            var result = await service.GetSecretAsync("");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetSecretAsync_WhenServiceDisabled_ShouldReturnNull()
        {
            // Arrange
            _options.Enabled = false;
            var providers = new List<ISecretProvider>();
            var service = new KeyVaultService(providers, _optionsMock.Object, _loggerMock.Object);

            // Act
            var result = await service.GetSecretAsync("test-key");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetSecretAsync_WithNoAvailableProviders_ShouldReturnNull()
        {
            // Arrange
            var mockProvider = new Mock<ISecretProvider>();
            mockProvider.Setup(p => p.ProviderName).Returns("Provider1");
            mockProvider.Setup(p => p.Priority).Returns(1);
            mockProvider.Setup(p => p.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var providers = new List<ISecretProvider> { mockProvider.Object };
            var service = new KeyVaultService(providers, _optionsMock.Object, _loggerMock.Object);

            // Act
            var result = await service.GetSecretAsync("test-key");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task SetSecretAsync_WithValidProvider_ShouldReturnTrue()
        {
            // Arrange
            var mockProvider = new Mock<ISecretProvider>();
            mockProvider.Setup(p => p.ProviderName).Returns("Provider1");
            mockProvider.Setup(p => p.Priority).Returns(1);
            mockProvider.Setup(p => p.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            mockProvider.Setup(p => p.SetSecretAsync("test-key", "test-value", It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var providers = new List<ISecretProvider> { mockProvider.Object };
            var service = new KeyVaultService(providers, _optionsMock.Object, _loggerMock.Object);

            // Act
            var result = await service.SetSecretAsync("test-key", "test-value");

            // Assert
            result.Should().BeTrue();
            mockProvider.Verify(p => p.SetSecretAsync("test-key", "test-value", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAvailableProvidersAsync_ShouldReturnOnlyAvailableProviders()
        {
            // Arrange
            var mockProvider1 = new Mock<ISecretProvider>();
            mockProvider1.Setup(p => p.ProviderName).Returns("Provider1");
            mockProvider1.Setup(p => p.Priority).Returns(1);
            mockProvider1.Setup(p => p.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var mockProvider2 = new Mock<ISecretProvider>();
            mockProvider2.Setup(p => p.ProviderName).Returns("Provider2");
            mockProvider2.Setup(p => p.Priority).Returns(2);
            mockProvider2.Setup(p => p.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var providers = new List<ISecretProvider> { mockProvider1.Object, mockProvider2.Object };
            var service = new KeyVaultService(providers, _optionsMock.Object, _loggerMock.Object);

            // Act
            var result = await service.GetAvailableProvidersAsync();

            // Assert
            result.Should().ContainSingle().Which.Should().Be("Provider1");
        }

        [Fact]
        public async Task GetSecretsAsync_WithMultipleKeys_ShouldReturnAllSecrets()
        {
            // Arrange
            var mockProvider = new Mock<ISecretProvider>();
            mockProvider.Setup(p => p.ProviderName).Returns("Provider1");
            mockProvider.Setup(p => p.Priority).Returns(1);
            mockProvider.Setup(p => p.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            mockProvider.Setup(p => p.GetSecretAsync("key1", It.IsAny<CancellationToken>())).ReturnsAsync("value1");
            mockProvider.Setup(p => p.GetSecretAsync("key2", It.IsAny<CancellationToken>())).ReturnsAsync("value2");

            var providers = new List<ISecretProvider> { mockProvider.Object };
            var service = new KeyVaultService(providers, _optionsMock.Object, _loggerMock.Object);

            // Act
            var result = await service.GetSecretsAsync(new[] { "key1", "key2" });

            // Assert
            result.Should().HaveCount(2);
            result["key1"].Should().Be("value1");
            result["key2"].Should().Be("value2");
        }

        [Fact]
        public async Task GetSecretAsync_WithCachingEnabled_ShouldUseCache()
        {
            // Arrange
            _options.EnableCaching = true;
            _options.CacheExpirationMinutes = 1;

            var mockProvider = new Mock<ISecretProvider>();
            mockProvider.Setup(p => p.ProviderName).Returns("Provider1");
            mockProvider.Setup(p => p.Priority).Returns(1);
            mockProvider.Setup(p => p.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            mockProvider.Setup(p => p.GetSecretAsync("test-key", It.IsAny<CancellationToken>())).ReturnsAsync("test-value");

            var providers = new List<ISecretProvider> { mockProvider.Object };
            var service = new KeyVaultService(providers, _optionsMock.Object, _loggerMock.Object);

            // Act
            var result1 = await service.GetSecretAsync("test-key");
            var result2 = await service.GetSecretAsync("test-key");

            // Assert
            result1.Should().Be("test-value");
            result2.Should().Be("test-value");
            mockProvider.Verify(p => p.GetSecretAsync("test-key", It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}