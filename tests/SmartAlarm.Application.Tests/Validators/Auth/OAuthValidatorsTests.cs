using FluentAssertions;
using FluentValidation.TestHelper;
using SmartAlarm.Application.Commands.Auth;
using SmartAlarm.Application.DTOs.Auth;
using SmartAlarm.Application.Validators.Auth;
using Xunit;

namespace SmartAlarm.Application.Tests.Validators.Auth;

public class OAuthValidatorsTests
{
    [Theory]
    [InlineData("Google")]
    [InlineData("GitHub")]
    [InlineData("Facebook")]
    [InlineData("Microsoft")]
    public void GetOAuthAuthorizationUrlCommandValidator_ValidProviders_ShouldNotHaveErrors(string provider)
    {
        // Arrange
        var validator = new GetOAuthAuthorizationUrlCommandValidator();
        var command = new GetOAuthAuthorizationUrlCommand(provider, "https://localhost/callback", "state123");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("InvalidProvider")]
    public void GetOAuthAuthorizationUrlCommandValidator_InvalidProviders_ShouldHaveErrors(string provider)
    {
        // Arrange
        var validator = new GetOAuthAuthorizationUrlCommandValidator();
        var command = new GetOAuthAuthorizationUrlCommand(provider, "https://localhost/callback", "state123");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Provider);
    }

    [Theory]
    [InlineData("https://localhost/callback")]
    [InlineData("http://localhost:3000/auth/callback")]
    [InlineData("https://myapp.com/auth/oauth/callback")]
    public void GetOAuthAuthorizationUrlCommandValidator_ValidRedirectUris_ShouldNotHaveErrors(string redirectUri)
    {
        // Arrange
        var validator = new GetOAuthAuthorizationUrlCommandValidator();
        var command = new GetOAuthAuthorizationUrlCommand("Google", redirectUri, "state123");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("not-a-uri")]
    [InlineData("ftp://localhost/callback")]
    public void GetOAuthAuthorizationUrlCommandValidator_InvalidRedirectUris_ShouldHaveErrors(string redirectUri)
    {
        // Arrange
        var validator = new GetOAuthAuthorizationUrlCommandValidator();
        var command = new GetOAuthAuthorizationUrlCommand("Google", redirectUri, "state123");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RedirectUri);
    }

    [Fact]
    public void GetOAuthAuthorizationUrlCommandValidator_LongState_ShouldHaveError()
    {
        // Arrange
        var validator = new GetOAuthAuthorizationUrlCommandValidator();
        var longState = new string('a', 257); // Exceeds 256 character limit
        var command = new GetOAuthAuthorizationUrlCommand("Google", "https://localhost/callback", longState);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.State);
    }

    [Fact]
    public void ProcessOAuthCallbackCommandValidator_ValidRequest_ShouldNotHaveErrors()
    {
        // Arrange
        var validator = new ProcessOAuthCallbackCommandValidator();
        var command = new ProcessOAuthCallbackCommand("Google", "auth_code_123", "https://localhost/callback", "state123");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void ProcessOAuthCallbackCommandValidator_EmptyAuthorizationCode_ShouldHaveError(string authCode)
    {
        // Arrange
        var validator = new ProcessOAuthCallbackCommandValidator();
        var command = new ProcessOAuthCallbackCommand("Google", authCode, "https://localhost/callback", "state123");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AuthorizationCode);
    }

    [Fact]
    public void OAuthCallbackRequestDtoValidator_ValidRequest_ShouldNotHaveErrors()
    {
        // Arrange
        var validator = new OAuthCallbackRequestDtoValidator();
        var dto = new OAuthCallbackRequestDto
        {
            Code = "auth_code_123",
            State = "state123"
        };

        // Act
        var result = validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void OAuthCallbackRequestDtoValidator_ErrorWithoutCode_ShouldNotHaveErrors()
    {
        // Arrange
        var validator = new OAuthCallbackRequestDtoValidator();
        var dto = new OAuthCallbackRequestDto
        {
            Code = "",
            State = "state123",
            Error = "access_denied",
            ErrorDescription = "User denied access"
        };

        // Act
        var result = validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void OAuthCallbackRequestDtoValidator_NoCodeNoError_ShouldHaveError()
    {
        // Arrange
        var validator = new OAuthCallbackRequestDtoValidator();
        var dto = new OAuthCallbackRequestDto
        {
            Code = "",
            State = "state123"
        };

        // Act
        var result = validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void OAuthCallbackRequestDtoValidator_LongState_ShouldHaveError()
    {
        // Arrange
        var validator = new OAuthCallbackRequestDtoValidator();
        var longState = new string('a', 257);
        var dto = new OAuthCallbackRequestDto
        {
            Code = "auth_code_123",
            State = longState
        };

        // Act
        var result = validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.State);
    }
}