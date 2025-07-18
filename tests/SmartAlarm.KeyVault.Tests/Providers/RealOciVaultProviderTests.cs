using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using SmartAlarm.KeyVault.Configuration;
using SmartAlarm.KeyVault.Providers;

namespace SmartAlarm.KeyVault.Tests.Providers
{
    /// <summary>
    /// Testes unitários para RealOciVaultProvider.
    /// Foca na lógica de negócio e comportamento do provider sem dependências externas.
    /// </summary>
    public class RealOciVaultProviderTests : IDisposable
    {
        private readonly Mock<ILogger<RealOciVaultProvider>> _mockLogger;
        private readonly OciVaultOptions _options;
        private readonly RealOciVaultProvider _provider;

        public RealOciVaultProviderTests()
        {
            _mockLogger = new Mock<ILogger<RealOciVaultProvider>>();
            _options = new OciVaultOptions
            {
                CompartmentId = "ocid1.compartment.oc1..test",
                VaultId = "ocid1.vault.oc1..test",
                Region = "us-ashburn-1",
                Priority = 3,
                ConfigFilePath = null // Usar default config
            };

            var optionsWrapper = Options.Create(_options);
            _provider = new RealOciVaultProvider(optionsWrapper, _mockLogger.Object);
        }

        [Fact]
        public void Constructor_ShouldInitializeCorrectly()
        {
            // Arrange & Act - Done in constructor

            // Assert
            Assert.Equal("OCI-Real", _provider.ProviderName);
            Assert.Equal(3, _provider.Priority);
        }

        [Fact]
        public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
        {
            // Arrange
            IOptions<OciVaultOptions>? nullOptions = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new RealOciVaultProvider(nullOptions!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Arrange
            var optionsWrapper = Options.Create(_options);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new RealOciVaultProvider(optionsWrapper, null!));
        }

        [Theory]
        [InlineData("", "VaultId", "us-ashburn-1")]
        [InlineData("CompartmentId", "", "us-ashburn-1")]
        [InlineData("CompartmentId", "VaultId", "")]
        public void Constructor_WithInvalidOptions_ShouldThrowArgumentException(
            string compartmentId, string vaultId, string region)
        {
            // Arrange
            var invalidOptions = new OciVaultOptions
            {
                CompartmentId = compartmentId,
                VaultId = vaultId,
                Region = region,
                Priority = 1
            };
            var optionsWrapper = Options.Create(invalidOptions);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                new RealOciVaultProvider(optionsWrapper, _mockLogger.Object));
        }

        [Fact]
        public async Task IsAvailableAsync_ShouldReturnFalse_WhenOciClientUnavailable()
        {
            // Arrange
            var invalidOptions = new OciVaultOptions
            {
                CompartmentId = "invalid-compartment",
                VaultId = "invalid-vault",
                Region = "invalid-region",
                Priority = 1,
                ConfigFilePath = "invalid-config.txt"
            };
            var optionsWrapper = Options.Create(invalidOptions);
            var provider = new RealOciVaultProvider(optionsWrapper, _mockLogger.Object);

            // Act
            var result = await provider.IsAvailableAsync();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetSecretAsync_WithValidSecretName_ShouldReturnValue()
        {
            // Arrange
            var secretKey = "test-secret";

            // Act
            var result = await _provider.GetSecretAsync(secretKey);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("oci-real", result);
            Assert.Contains("USASHBURN1", result); // Region prefix
        }

        [Fact]
        public async Task GetSecretAsync_WithDatabaseConnection_ShouldReturnFormattedConnectionString()
        {
            // Arrange
            var secretKey = "database-connection";

            // Act
            var result = await _provider.GetSecretAsync(secretKey);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Server=", result);
            Assert.Contains("Database=SmartAlarm", result);
            Assert.Contains("USASHBURN1", result);
        }

        [Fact]
        public async Task GetSecretAsync_WithApiKey_ShouldReturnFormattedApiKey()
        {
            // Arrange
            var secretKey = "api-key";

            // Act
            var result = await _provider.GetSecretAsync(secretKey);

            // Assert
            Assert.NotNull(result);
            Assert.StartsWith("oci-real-USASHBURN1-", result);
        }

        [Fact]
        public async Task GetSecretAsync_WithJwtSecret_ShouldReturnBase64EncodedValue()
        {
            // Arrange
            var secretKey = "jwt-secret";

            // Act
            var result = await _provider.GetSecretAsync(secretKey);

            // Assert
            Assert.NotNull(result);
            // Should be valid base64
            Assert.True(IsValidBase64(result));
        }

        [Fact]
        public async Task GetSecretAsync_WithEncryptionKey_ShouldReturnBase64EncodedValue()
        {
            // Arrange
            var secretKey = "encryption-key";

            // Act
            var result = await _provider.GetSecretAsync(secretKey);

            // Assert
            Assert.NotNull(result);
            // Should be valid base64
            Assert.True(IsValidBase64(result));
        }

        [Fact]
        public async Task GetSecretsAsync_WithMultipleKeys_ShouldReturnAllSecrets()
        {
            // Arrange
            var secretKeys = new[] { "database-connection", "api-key", "jwt-secret" };

            // Act
            var results = await _provider.GetSecretsAsync(secretKeys);

            // Assert
            Assert.NotNull(results);
            Assert.Equal(3, results.Count);
            Assert.All(secretKeys, key => Assert.True(results.ContainsKey(key)));
            Assert.All(results.Values, value => Assert.NotNull(value));
        }

        [Fact]
        public async Task GetSecretsAsync_WithEmptyList_ShouldReturnEmptyDictionary()
        {
            // Arrange
            var secretKeys = new string[0];

            // Act
            var results = await _provider.GetSecretsAsync(secretKeys);

            // Assert
            Assert.NotNull(results);
            Assert.Empty(results);
        }

        [Fact]
        public async Task SetSecretAsync_ShouldReturnTrue()
        {
            // Arrange
            var secretKey = "test-secret";
            var secretValue = "test-value";

            // Act
            var result = await _provider.SetSecretAsync(secretKey, secretValue);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task SetSecretAsync_WithCancellation_ShouldReturnFalse()
        {
            // Arrange
            var secretKey = "test-secret";
            var secretValue = "test-value";
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            var result = await _provider.SetSecretAsync(secretKey, secretValue, cts.Token);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Dispose_ShouldDisposeResourcesCleanly()
        {
            // Arrange & Act
            _provider.Dispose();

            // Assert - Should not throw
            // Subsequent operations may fail, but disposal should be clean
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_ShouldNotThrow()
        {
            // Arrange & Act
            _provider.Dispose();
            _provider.Dispose();
            _provider.Dispose();

            // Assert - Should not throw
        }

        [Theory]
        [InlineData("us-ashburn-1", "USASHBURN1")]
        [InlineData("us-phoenix-1", "USPHOENIX1")]
        [InlineData("uk-london-1", "UKLONDON1")]
        [InlineData("eu-frankfurt-1", "EUFRANKFURT1")]
        public async Task GetSecretAsync_WithDifferentRegions_ShouldIncludeRegionPrefix(
            string region, string expectedPrefix)
        {
            // Arrange
            var options = new OciVaultOptions
            {
                CompartmentId = "ocid1.compartment.oc1..test",
                VaultId = "ocid1.vault.oc1..test",
                Region = region,
                Priority = 1
            };
            var optionsWrapper = Options.Create(options);
            using var provider = new RealOciVaultProvider(optionsWrapper, _mockLogger.Object);

            // Act
            var result = await provider.GetSecretAsync("database-connection");

            // Assert
            Assert.NotNull(result);
            Assert.Contains(expectedPrefix, result);
        }

        [Fact]
        public void ProviderName_ShouldReturnCorrectName()
        {
            // Act & Assert
            Assert.Equal("OCI-Real", _provider.ProviderName);
        }

        [Fact]
        public void Priority_ShouldReturnConfiguredPriority()
        {
            // Act & Assert
            Assert.Equal(3, _provider.Priority);
        }

        private static bool IsValidBase64(string base64String)
        {
            if (string.IsNullOrEmpty(base64String))
                return false;

            try
            {
                Convert.FromBase64String(base64String);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _provider?.Dispose();
        }
    }
}
