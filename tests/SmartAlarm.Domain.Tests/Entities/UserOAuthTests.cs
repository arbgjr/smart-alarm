using FluentAssertions;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.ValueObjects;
using Xunit;

namespace SmartAlarm.Domain.Tests.Entities;

public class UserOAuthTests
{
    [Fact]
    public void SetExternalProvider_ValidData_ShouldSetProviderInfo()
    {
        // Arrange
        var user = new User(Guid.NewGuid(), "Test User", "test@example.com");
        var provider = "Google";
        var providerId = "google_user_123";

        // Act
        user.SetExternalProvider(provider, providerId);

        // Assert
        user.ExternalProvider.Should().Be(provider);
        user.ExternalProviderId.Should().Be(providerId);
        user.HasExternalProvider.Should().BeTrue();
        user.UpdatedAt.Should().NotBeNull();
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("", "provider_id")]
    [InlineData(null, "provider_id")]
    [InlineData("   ", "provider_id")]
    [InlineData("Google", "")]
    [InlineData("Google", null)]
    [InlineData("Google", "   ")]
    public void SetExternalProvider_InvalidData_ShouldThrowException(string provider, string providerId)
    {
        // Arrange
        var user = new User(Guid.NewGuid(), "Test User", "test@example.com");

        // Act & Assert
        user.Invoking(u => u.SetExternalProvider(provider, providerId))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ClearExternalProvider_WithExistingProvider_ShouldClearProviderInfo()
    {
        // Arrange
        var user = new User(Guid.NewGuid(), "Test User", "test@example.com");
        user.SetExternalProvider("Google", "google_user_123");

        // Act
        user.ClearExternalProvider();

        // Assert
        user.ExternalProvider.Should().BeNull();
        user.ExternalProviderId.Should().BeNull();
        user.HasExternalProvider.Should().BeFalse();
        user.UpdatedAt.Should().NotBeNull();
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void HasExternalProvider_WithoutProvider_ShouldReturnFalse()
    {
        // Arrange
        var user = new User(Guid.NewGuid(), "Test User", "test@example.com");

        // Act & Assert
        user.HasExternalProvider.Should().BeFalse();
    }

    [Fact]
    public void HasExternalProvider_WithProvider_ShouldReturnTrue()
    {
        // Arrange
        var user = new User(Guid.NewGuid(), "Test User", "test@example.com");
        user.SetExternalProvider("GitHub", "github_user_456");

        // Act & Assert
        user.HasExternalProvider.Should().BeTrue();
    }

    [Fact]
    public void IsExternalUser_WithProviderAndNoPassword_ShouldReturnTrue()
    {
        // Arrange
        var user = new User(Guid.NewGuid(), "Test User", "test@example.com");
        user.SetExternalProvider("Facebook", "facebook_user_789");

        // Act & Assert
        user.IsExternalUser.Should().BeTrue();
    }

    [Fact]
    public void IsExternalUser_WithProviderAndPassword_ShouldReturnFalse()
    {
        // Arrange
        var user = new User(Guid.NewGuid(), "Test User", "test@example.com");
        user.SetExternalProvider("Microsoft", "microsoft_user_101");
        user.SetPasswordHash("hashed_password");

        // Act & Assert
        user.IsExternalUser.Should().BeFalse();
    }

    [Fact]
    public void IsExternalUser_WithoutProvider_ShouldReturnFalse()
    {
        // Arrange
        var user = new User(Guid.NewGuid(), "Test User", "test@example.com");

        // Act & Assert
        user.IsExternalUser.Should().BeFalse();
    }
}

public class ExternalAuthInfoTests
{
    [Fact]
    public void Constructor_ValidData_ShouldCreateInstance()
    {
        // Arrange & Act
        var authInfo = new ExternalAuthInfo(
            "Google",
            "google_user_123",
            "test@example.com",
            "Test User",
            "https://example.com/avatar.jpg");

        // Assert
        authInfo.Provider.Should().Be("Google");
        authInfo.ProviderId.Should().Be("google_user_123");
        authInfo.Email.Should().Be("test@example.com");
        authInfo.Name.Should().Be("Test User");
        authInfo.AvatarUrl.Should().Be("https://example.com/avatar.jpg");
        authInfo.AdditionalClaims.Should().NotBeNull();
        authInfo.AdditionalClaims.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithAdditionalClaims_ShouldCreateInstance()
    {
        // Arrange
        var additionalClaims = new Dictionary<string, object>
        {
            ["locale"] = "pt-BR",
            ["verified"] = true
        };

        // Act
        var authInfo = new ExternalAuthInfo(
            "GitHub",
            "github_user_456",
            "dev@example.com",
            "Dev User",
            additionalClaims: additionalClaims);

        // Assert
        authInfo.AdditionalClaims.Should().NotBeNull();
        authInfo.AdditionalClaims.Should().HaveCount(2);
        authInfo.AdditionalClaims["locale"].Should().Be("pt-BR");
        authInfo.AdditionalClaims["verified"].Should().Be(true);
    }

    [Theory]
    [InlineData("", "provider_id", "test@example.com", "Test User")]
    [InlineData(null, "provider_id", "test@example.com", "Test User")]
    [InlineData("Google", "", "test@example.com", "Test User")]
    [InlineData("Google", null, "test@example.com", "Test User")]
    [InlineData("Google", "provider_id", "", "Test User")]
    [InlineData("Google", "provider_id", null, "Test User")]
    [InlineData("Google", "provider_id", "test@example.com", "")]
    [InlineData("Google", "provider_id", "test@example.com", null)]
    public void Constructor_InvalidData_ShouldThrowException(string provider, string providerId, string email, string name)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ExternalAuthInfo(provider, providerId, email, name));
    }

    [Theory]
    [InlineData("Google")]
    [InlineData("GitHub")]
    [InlineData("Facebook")]
    [InlineData("Microsoft")]
    public void SupportedProviders_ValidProviders_ShouldBeSupported(string provider)
    {
        // Act & Assert
        ExternalAuthInfo.SupportedProviders.IsSupported(provider).Should().BeTrue();
        ExternalAuthInfo.SupportedProviders.All.Should().Contain(provider);
    }

    [Theory]
    [InlineData("Twitter")]
    [InlineData("LinkedIn")]
    [InlineData("InvalidProvider")]
    [InlineData("")]
    [InlineData(null)]
    public void SupportedProviders_InvalidProviders_ShouldNotBeSupported(string provider)
    {
        // Act & Assert
        ExternalAuthInfo.SupportedProviders.IsSupported(provider).Should().BeFalse();
    }
}