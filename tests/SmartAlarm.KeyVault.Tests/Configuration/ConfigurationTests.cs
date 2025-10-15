using SmartAlarm.Domain.Abstractions;
using FluentAssertions;
using SmartAlarm.KeyVault.Configuration;
using Xunit;

namespace SmartAlarm.KeyVault.Tests.Configuration
{
    public class KeyVaultOptionsTests
    {
        [Fact]
        public void DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var options = new KeyVaultOptions();

            // Assert
            options.Enabled.Should().BeTrue();
            options.GlobalTimeoutSeconds.Should().Be(30);
            options.RetryAttempts.Should().Be(3);
            options.RetryDelayMs.Should().Be(1000);
            options.UseExponentialBackoff.Should().BeTrue();
            options.EnableCaching.Should().BeTrue();
            options.CacheExpirationMinutes.Should().Be(15);
            options.EnableDetailedLogging.Should().BeFalse();
            options.DisabledProviders.Should().NotBeNull().And.BeEmpty();
            options.ProviderPriorities.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void ConfigSection_ShouldBeCorrect()
        {
            // Act & Assert
            KeyVaultOptions.ConfigSection.Should().Be("KeyVault");
        }
    }

    public class HashiCorpVaultOptionsTests
    {
        [Fact]
        public void DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var options = new HashiCorpVaultOptions();

            // Assert
            options.ServerAddress.Should().BeEmpty();
            options.Token.Should().BeEmpty();
            options.MountPath.Should().Be("secret");
            options.KvVersion.Should().Be(2);
            options.SkipTlsVerification.Should().BeFalse();
            options.TimeoutSeconds.Should().Be(30);
            options.Priority.Should().Be(1);
        }

        [Fact]
        public void ConfigSection_ShouldBeCorrect()
        {
            // Act & Assert
            HashiCorpVaultOptions.ConfigSection.Should().Be("HashiCorpVault");
        }
    }

    public class AzureKeyVaultOptionsTests
    {
        [Fact]
        public void DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var options = new AzureKeyVaultOptions();

            // Assert
            options.VaultUri.Should().BeEmpty();
            options.TenantId.Should().BeNull();
            options.ClientId.Should().BeNull();
            options.ClientSecret.Should().BeNull();
            options.UseManagedIdentity.Should().BeTrue();
            options.TimeoutSeconds.Should().Be(30);
            options.Priority.Should().Be(2);
        }

        [Fact]
        public void ConfigSection_ShouldBeCorrect()
        {
            // Act & Assert
            AzureKeyVaultOptions.ConfigSection.Should().Be("AzureKeyVault");
        }
    }

    public class AwsSecretsManagerOptionsTests
    {
        [Fact]
        public void DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var options = new AwsSecretsManagerOptions();

            // Assert
            options.Region.Should().BeEmpty();
            options.AccessKeyId.Should().BeNull();
            options.SecretAccessKey.Should().BeNull();
            options.SessionToken.Should().BeNull();
            options.UseIamRole.Should().BeTrue();
            options.TimeoutSeconds.Should().Be(30);
            options.Priority.Should().Be(3);
        }

        [Fact]
        public void ConfigSection_ShouldBeCorrect()
        {
            // Act & Assert
            AwsSecretsManagerOptions.ConfigSection.Should().Be("AwsSecretsManager");
        }
    }
}