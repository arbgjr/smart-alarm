using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Application.Commands.Auth;
using SmartAlarm.Application.DTOs.Auth;
using SmartAlarm.Application.Handlers.Auth;
using SmartAlarm.Application.Validators.Auth;
using SmartAlarm.Domain.Abstractions;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Domain.ValueObjects;
using Xunit;

namespace SmartAlarm.Application.Tests.Handlers.Auth;

public class OAuthHandlersTests
{
    private readonly Mock<IOAuthProviderFactory> _mockOAuthProviderFactory;
    private readonly Mock<IOAuthProvider> _mockOAuthProvider;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IJwtTokenService> _mockTokenService;
    private readonly Mock<ILogger<GetOAuthAuthorizationUrlHandler>> _mockAuthUrlLogger;
    private readonly Mock<ILogger<ProcessOAuthCallbackHandler>> _mockCallbackLogger;
    private readonly Mock<IValidator<GetOAuthAuthorizationUrlCommand>> _mockAuthUrlValidator;
    private readonly Mock<IValidator<ProcessOAuthCallbackCommand>> _mockCallbackValidator;

    public OAuthHandlersTests()
    {
        _mockOAuthProviderFactory = new Mock<IOAuthProviderFactory>();
        _mockOAuthProvider = new Mock<IOAuthProvider>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockTokenService = new Mock<IJwtTokenService>();
        _mockAuthUrlLogger = new Mock<ILogger<GetOAuthAuthorizationUrlHandler>>();
        _mockCallbackLogger = new Mock<ILogger<ProcessOAuthCallbackHandler>>();
        _mockAuthUrlValidator = new Mock<IValidator<GetOAuthAuthorizationUrlCommand>>();
        _mockCallbackValidator = new Mock<IValidator<ProcessOAuthCallbackCommand>>();
    }

    [Fact]
    public async Task GetOAuthAuthorizationUrlHandler_ValidRequest_ReturnsAuthorizationUrl()
    {
        // Arrange
        var command = new GetOAuthAuthorizationUrlCommand("Google", "https://localhost/callback", "state123");
        var expectedUrl = "https://accounts.google.com/oauth/authorize?client_id=test&redirect_uri=https://localhost/callback&state=state123";
        
        _mockAuthUrlValidator.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        
        _mockOAuthProviderFactory.Setup(x => x.CreateProvider("Google"))
            .Returns(_mockOAuthProvider.Object);
        
        _mockOAuthProvider.Setup(x => x.GetAuthorizationUrl("https://localhost/callback", "state123", null))
            .Returns(expectedUrl);

        var handler = new GetOAuthAuthorizationUrlHandler(
            _mockOAuthProviderFactory.Object,
            _mockAuthUrlValidator.Object,
            _mockAuthUrlLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AuthorizationUrl.Should().Be(expectedUrl);
        result.State.Should().Be("state123");
        result.Provider.Should().Be("Google");
    }

    [Fact]
    public async Task ProcessOAuthCallbackHandler_NewUser_CreatesUserAndReturnsToken()
    {
        // Arrange
        var command = new ProcessOAuthCallbackCommand("Google", "auth_code_123", "https://localhost/callback", "state123");
        var externalAuthInfo = new ExternalAuthInfo("Google", "google_user_123", "test@example.com", "Test User");
        
        _mockCallbackValidator.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        
        _mockOAuthProviderFactory.Setup(x => x.CreateProvider("Google"))
            .Returns(_mockOAuthProvider.Object);
        
        _mockOAuthProvider.Setup(x => x.ExchangeCodeForTokenAsync("auth_code_123", "state123", "https://localhost/callback", It.IsAny<CancellationToken>()))
            .ReturnsAsync(externalAuthInfo);

        _mockUserRepository.Setup(x => x.FindByExternalProviderAsync("Google", "google_user_123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User)null);
        
        _mockUserRepository.Setup(x => x.FindByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User)null);

        _mockTokenService.Setup(x => x.GenerateAccessTokenAsync(It.IsAny<User>()))
            .ReturnsAsync("access_token_123");
        
        _mockTokenService.Setup(x => x.GenerateRefreshTokenAsync(It.IsAny<User>()))
            .ReturnsAsync("refresh_token_123");

        var handler = new ProcessOAuthCallbackHandler(
            _mockOAuthProviderFactory.Object,
            _mockUserRepository.Object,
            _mockTokenService.Object,
            _mockCallbackValidator.Object,
            _mockCallbackLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be("access_token_123");
        result.RefreshToken.Should().Be("refresh_token_123");
        result.IsNewUser.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User!.Email.Should().Be("test@example.com");
        result.User.Name.Should().Be("Test User");
        result.Provider.Should().Be("Google");

        _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessOAuthCallbackHandler_ExistingUserWithProvider_ReturnsToken()
    {
        // Arrange
        var command = new ProcessOAuthCallbackCommand("Google", "auth_code_123", "https://localhost/callback", "state123");
        var externalAuthInfo = new ExternalAuthInfo("Google", "google_user_123", "test@example.com", "Test User");
        var existingUser = new User(Guid.NewGuid(), "Test User", "test@example.com");
        existingUser.SetExternalProvider("Google", "google_user_123");
        
        _mockCallbackValidator.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        
        _mockOAuthProviderFactory.Setup(x => x.CreateProvider("Google"))
            .Returns(_mockOAuthProvider.Object);
        
        _mockOAuthProvider.Setup(x => x.ExchangeCodeForTokenAsync("auth_code_123", "state123", "https://localhost/callback", It.IsAny<CancellationToken>()))
            .ReturnsAsync(externalAuthInfo);

        _mockUserRepository.Setup(x => x.FindByExternalProviderAsync("Google", "google_user_123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _mockTokenService.Setup(x => x.GenerateAccessTokenAsync(It.IsAny<User>()))
            .ReturnsAsync("access_token_123");
        
        _mockTokenService.Setup(x => x.GenerateRefreshTokenAsync(It.IsAny<User>()))
            .ReturnsAsync("refresh_token_123");

        var handler = new ProcessOAuthCallbackHandler(
            _mockOAuthProviderFactory.Object,
            _mockUserRepository.Object,
            _mockTokenService.Object,
            _mockCallbackValidator.Object,
            _mockCallbackLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be("access_token_123");
        result.RefreshToken.Should().Be("refresh_token_123");
        result.IsNewUser.Should().BeFalse();
        result.Provider.Should().Be("Google");

        _mockUserRepository.Verify(x => x.UpdateAsync(existingUser, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessOAuthCallbackHandler_ExistingUserByEmail_LinksProvider()
    {
        // Arrange
        var command = new ProcessOAuthCallbackCommand("Google", "auth_code_123", "https://localhost/callback", "state123");
        var externalAuthInfo = new ExternalAuthInfo("Google", "google_user_123", "test@example.com", "Test User");
        var existingUser = new User(Guid.NewGuid(), "Test User", "test@example.com");
        
        _mockCallbackValidator.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        
        _mockOAuthProviderFactory.Setup(x => x.CreateProvider("Google"))
            .Returns(_mockOAuthProvider.Object);
        
        _mockOAuthProvider.Setup(x => x.ExchangeCodeForTokenAsync("auth_code_123", "state123", "https://localhost/callback", It.IsAny<CancellationToken>()))
            .ReturnsAsync(externalAuthInfo);

        _mockUserRepository.Setup(x => x.FindByExternalProviderAsync("Google", "google_user_123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User)null);
        
        _mockUserRepository.Setup(x => x.FindByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _mockTokenService.Setup(x => x.GenerateAccessTokenAsync(It.IsAny<User>()))
            .ReturnsAsync("access_token_123");
        
        _mockTokenService.Setup(x => x.GenerateRefreshTokenAsync(It.IsAny<User>()))
            .ReturnsAsync("refresh_token_123");

        var handler = new ProcessOAuthCallbackHandler(
            _mockOAuthProviderFactory.Object,
            _mockUserRepository.Object,
            _mockTokenService.Object,
            _mockCallbackValidator.Object,
            _mockCallbackLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.IsNewUser.Should().BeFalse();
        
        _mockUserRepository.Verify(x => x.UpdateAsync(It.Is<User>(u => 
            u.ExternalProvider == "Google" && u.ExternalProviderId == "google_user_123"), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessOAuthCallbackHandler_ValidationFailure_ReturnsFailure()
    {
        // Arrange
        var command = new ProcessOAuthCallbackCommand("InvalidProvider", "", "", "");
        var validationResult = new FluentValidation.Results.ValidationResult();
        validationResult.Errors.Add(new FluentValidation.Results.ValidationFailure("Provider", "Provider is not supported"));
        
        _mockCallbackValidator.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var handler = new ProcessOAuthCallbackHandler(
            _mockOAuthProviderFactory.Object,
            _mockUserRepository.Object,
            _mockTokenService.Object,
            _mockCallbackValidator.Object,
            _mockCallbackLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Provider is not supported");
    }

    [Fact]
    public async Task LinkExternalAccountHandler_ValidRequest_LinksAccount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new LinkExternalAccountCommand(userId, "GitHub", "auth_code_123", "https://localhost/callback", "state123");
        var externalAuthInfo = new ExternalAuthInfo("GitHub", "github_user_123", "test@example.com", "Test User");
        var existingUser = new User(userId, "Test User", "test@example.com");
        
        _mockOAuthProviderFactory.Setup(x => x.CreateProvider("GitHub"))
            .Returns(_mockOAuthProvider.Object);
        
        _mockOAuthProvider.Setup(x => x.ExchangeCodeForTokenAsync("auth_code_123", "state123", "https://localhost/callback", It.IsAny<CancellationToken>()))
            .ReturnsAsync(externalAuthInfo);

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        
        _mockUserRepository.Setup(x => x.FindByExternalProviderAsync("GitHub", "github_user_123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User)null);

        var handler = new LinkExternalAccountHandler(
            _mockOAuthProviderFactory.Object,
            _mockUserRepository.Object,
            Mock.Of<ILogger<LinkExternalAccountHandler>>());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _mockUserRepository.Verify(x => x.UpdateAsync(It.Is<User>(u => 
            u.ExternalProvider == "GitHub" && u.ExternalProviderId == "github_user_123"), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnlinkExternalAccountHandler_ValidRequest_UnlinksAccount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new UnlinkExternalAccountCommand(userId, "GitHub");
        var existingUser = new User(userId, "Test User", "test@example.com");
        existingUser.SetPasswordHash("hashed_password"); // User has password, can unlink
        existingUser.SetExternalProvider("GitHub", "github_user_123");
        
        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        var handler = new UnlinkExternalAccountHandler(
            _mockUserRepository.Object,
            Mock.Of<ILogger<UnlinkExternalAccountHandler>>());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _mockUserRepository.Verify(x => x.UpdateAsync(It.Is<User>(u => 
            u.ExternalProvider == null && u.ExternalProviderId == null), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnlinkExternalAccountHandler_ExternalOnlyUser_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new UnlinkExternalAccountCommand(userId, "GitHub");
        var existingUser = new User(userId, "Test User", "test@example.com");
        existingUser.SetExternalProvider("GitHub", "github_user_123");
        // No password set - external only user
        
        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        var handler = new UnlinkExternalAccountHandler(
            _mockUserRepository.Object,
            Mock.Of<ILogger<UnlinkExternalAccountHandler>>());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}