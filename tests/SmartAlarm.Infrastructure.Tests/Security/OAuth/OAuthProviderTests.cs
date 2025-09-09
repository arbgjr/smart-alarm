using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using SmartAlarm.Domain.Abstractions;
using SmartAlarm.Infrastructure.Security.OAuth;
using SmartAlarm.KeyVault.Abstractions;
using System.Net;
using System.Text.Json;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests.Security.OAuth;

public class GoogleOAuthProviderTests
{
    private readonly Mock<IKeyVaultService> _mockKeyVault;
    private readonly Mock<ILogger<GoogleOAuthProvider>> _mockLogger;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly GoogleOAuthProvider _provider;

    public GoogleOAuthProviderTests()
    {
        _mockKeyVault = new Mock<IKeyVaultService>();
        _mockLogger = new Mock<ILogger<GoogleOAuthProvider>>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _provider = new GoogleOAuthProvider(_httpClient, _mockKeyVault.Object, _mockLogger.Object);
    }

    [Fact]
    public void ProviderName_ShouldReturnGoogle()
    {
        // Act & Assert
        _provider.ProviderName.Should().Be("Google");
    }

    [Fact]
    public void GetAuthorizationUrl_ValidParameters_ShouldReturnValidUrl()
    {
        // Arrange
        _mockKeyVault.Setup(x => x.GetSecretAsync("google-client-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-client-id");

        var redirectUri = "https://localhost/callback";
        var state = "test-state";

        // Act
        var result = _provider.GetAuthorizationUrl(redirectUri, state);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("accounts.google.com/o/oauth2/v2/auth");
        result.Should().Contain("client_id=test-client-id");
        result.Should().Contain($"redirect_uri={Uri.EscapeDataString(redirectUri)}");
        result.Should().Contain($"state={Uri.EscapeDataString(state)}");
        result.Should().Contain("scope=openid%20email%20profile");
        result.Should().Contain("response_type=code");
    }

    [Fact]
    public async Task ExchangeCodeForTokenAsync_ValidCode_ShouldReturnExternalAuthInfo()
    {
        // Arrange
        var authCode = "test-auth-code";
        var redirectUri = "https://localhost/callback";
        var state = "test-state";

        _mockKeyVault.Setup(x => x.GetSecretAsync("google-client-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-client-id");
        _mockKeyVault.Setup(x => x.GetSecretAsync("google-client-secret", It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-client-secret");

        var tokenResponse = new
        {
            access_token = "test-access-token",
            token_type = "Bearer"
        };

        var userResponse = new
        {
            id = "google-user-123",
            email = "test@example.com",
            name = "Test User",
            picture = "https://example.com/avatar.jpg",
            verified_email = true
        };

        SetupHttpResponse("https://oauth2.googleapis.com/token", tokenResponse);
        SetupHttpResponse("https://www.googleapis.com/oauth2/v2/userinfo", userResponse);

        // Act
        var result = await _provider.ExchangeCodeForTokenAsync(authCode, state, redirectUri);

        // Assert
        result.Should().NotBeNull();
        result.Provider.Should().Be("Google");
        result.ProviderId.Should().Be("google-user-123");
        result.Email.Should().Be("test@example.com");
        result.Name.Should().Be("Test User");
        result.AvatarUrl.Should().Be("https://example.com/avatar.jpg");
        result.AdditionalClaims.Should().ContainKey("email_verified");
        result.AdditionalClaims["email_verified"].Should().Be(true);
    }

    [Fact]
    public async Task ValidateTokenAsync_ValidToken_ShouldReturnExternalAuthInfo()
    {
        // Arrange
        var accessToken = "test-access-token";

        var userResponse = new
        {
            id = "google-user-123",
            email = "test@example.com",
            name = "Test User",
            picture = "https://example.com/avatar.jpg"
        };

        SetupHttpResponse("https://www.googleapis.com/oauth2/v2/userinfo", userResponse);

        // Act
        var result = await _provider.ValidateTokenAsync(accessToken);

        // Assert
        result.Should().NotBeNull();
        result.Provider.Should().Be("Google");
        result.ProviderId.Should().Be("google-user-123");
        result.Email.Should().Be("test@example.com");
        result.Name.Should().Be("Test User");
    }

    private void SetupHttpResponse<T>(string url, T responseObject)
    {
        var jsonResponse = JsonSerializer.Serialize(responseObject);
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains(url)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);
    }
}

public class GitHubOAuthProviderTests
{
    private readonly Mock<IKeyVaultService> _mockKeyVault;
    private readonly Mock<ILogger<GitHubOAuthProvider>> _mockLogger;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly GitHubOAuthProvider _provider;

    public GitHubOAuthProviderTests()
    {
        _mockKeyVault = new Mock<IKeyVaultService>();
        _mockLogger = new Mock<ILogger<GitHubOAuthProvider>>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _provider = new GitHubOAuthProvider(_httpClient, _mockKeyVault.Object, _mockLogger.Object);
    }

    [Fact]
    public void ProviderName_ShouldReturnGitHub()
    {
        // Act & Assert
        _provider.ProviderName.Should().Be("GitHub");
    }

    [Fact]
    public void GetAuthorizationUrl_ValidParameters_ShouldReturnValidUrl()
    {
        // Arrange
        _mockKeyVault.Setup(x => x.GetSecretAsync("github-client-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-client-id");

        var redirectUri = "https://localhost/callback";
        var state = "test-state";

        // Act
        var result = _provider.GetAuthorizationUrl(redirectUri, state);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("github.com/login/oauth/authorize");
        result.Should().Contain("client_id=test-client-id");
        result.Should().Contain($"redirect_uri={Uri.EscapeDataString(redirectUri)}");
        result.Should().Contain($"state={Uri.EscapeDataString(state)}");
        result.Should().Contain("scope=user%3Aemail%20read%3Auser");
        result.Should().Contain("response_type=code");
        result.Should().NotContain("access_type"); // GitHub doesn't use this parameter
        result.Should().NotContain("prompt"); // GitHub doesn't use this parameter
    }

    [Fact]
    public async Task ExchangeCodeForTokenAsync_ValidCode_ShouldReturnExternalAuthInfo()
    {
        // Arrange
        var authCode = "test-auth-code";
        var redirectUri = "https://localhost/callback";
        var state = "test-state";

        _mockKeyVault.Setup(x => x.GetSecretAsync("github-client-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-client-id");
        _mockKeyVault.Setup(x => x.GetSecretAsync("github-client-secret", It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-client-secret");

        var tokenResponse = new
        {
            access_token = "test-access-token",
            token_type = "bearer"
        };

        var userResponse = new
        {
            id = "12345",
            login = "testuser",
            name = "Test User",
            avatar_url = "https://github.com/avatar.jpg",
            bio = "Developer",
            location = "Brazil",
            company = "Test Company"
        };

        var emailsResponse = new[]
        {
            new { email = "test@example.com", primary = true, verified = true }
        };

        SetupHttpResponse("https://github.com/login/oauth/access_token", tokenResponse);
        SetupHttpResponse("https://api.github.com/user", userResponse);
        SetupHttpResponse("https://api.github.com/user/emails", emailsResponse);

        // Act
        var result = await _provider.ExchangeCodeForTokenAsync(authCode, state, redirectUri);

        // Assert
        result.Should().NotBeNull();
        result.Provider.Should().Be("GitHub");
        result.ProviderId.Should().Be("12345");
        result.Email.Should().Be("test@example.com");
        result.Name.Should().Be("Test User");
        result.AvatarUrl.Should().Be("https://github.com/avatar.jpg");
        result.AdditionalClaims.Should().ContainKey("username");
        result.AdditionalClaims["username"].Should().Be("testuser");
        result.AdditionalClaims.Should().ContainKey("bio");
        result.AdditionalClaims["bio"].Should().Be("Developer");
    }

    private void SetupHttpResponse<T>(string url, T responseObject)
    {
        var jsonResponse = JsonSerializer.Serialize(responseObject);
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains(url)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);
    }
}

public class OAuthProviderFactoryTests
{
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<ILogger<OAuthProviderFactory>> _mockLogger;
    private readonly OAuthProviderFactory _factory;

    public OAuthProviderFactoryTests()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockLogger = new Mock<ILogger<OAuthProviderFactory>>();
        _factory = new OAuthProviderFactory(_mockServiceProvider.Object, _mockLogger.Object);
    }

    [Theory]
    [InlineData("Google", typeof(GoogleOAuthProvider))]
    [InlineData("GitHub", typeof(GitHubOAuthProvider))]
    [InlineData("Facebook", typeof(FacebookOAuthProvider))]
    [InlineData("Microsoft", typeof(MicrosoftOAuthProvider))]
    public void CreateProvider_SupportedProviders_ShouldReturnCorrectProvider(string providerName, Type expectedType)
    {
        // Arrange
        var mockProvider = Mock.Of<IOAuthProvider>();
        _mockServiceProvider.Setup(x => x.GetService(expectedType))
            .Returns(mockProvider);

        // Act
        var result = _factory.CreateProvider(providerName);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType(expectedType);
    }

    [Theory]
    [InlineData("Twitter")]
    [InlineData("LinkedIn")]
    [InlineData("InvalidProvider")]
    [InlineData("")]
    [InlineData(null)]
    public void CreateProvider_UnsupportedProviders_ShouldThrowException(string providerName)
    {
        // Act & Assert
        _factory.Invoking(f => f.CreateProvider(providerName))
            .Should().Throw<ArgumentException>()
            .WithMessage($"OAuth provider '{providerName}' is not supported*");
    }

    [Fact]
    public void GetSupportedProviders_ShouldReturnAllSupportedProviders()
    {
        // Act
        var result = _factory.GetSupportedProviders();

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("Google");
        result.Should().Contain("GitHub");
        result.Should().Contain("Facebook");
        result.Should().Contain("Microsoft");
        result.Should().HaveCount(4);
    }

    [Theory]
    [InlineData("Google", true)]
    [InlineData("GitHub", true)]
    [InlineData("Facebook", true)]
    [InlineData("Microsoft", true)]
    [InlineData("Twitter", false)]
    [InlineData("InvalidProvider", false)]
    public void IsProviderSupported_ShouldReturnCorrectResult(string provider, bool expected)
    {
        // Act
        var result = _factory.IsProviderSupported(provider);

        // Assert
        result.Should().Be(expected);
    }
}