using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Commands.Auth;
using SmartAlarm.Application.DTOs.Auth;
using SmartAlarm.Domain.Abstractions;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Domain.ValueObjects;

namespace SmartAlarm.Application.Handlers.Auth;

/// <summary>
/// Handler para obter URL de autorização OAuth2
/// </summary>
public class GetOAuthAuthorizationUrlHandler : IRequestHandler<GetOAuthAuthorizationUrlCommand, OAuthAuthorizationResponseDto>
{
    private readonly IOAuthProviderFactory _oauthProviderFactory;
    private readonly IValidator<GetOAuthAuthorizationUrlCommand> _validator;
    private readonly ILogger<GetOAuthAuthorizationUrlHandler> _logger;

    public GetOAuthAuthorizationUrlHandler(
        IOAuthProviderFactory oauthProviderFactory,
        IValidator<GetOAuthAuthorizationUrlCommand> validator,
        ILogger<GetOAuthAuthorizationUrlHandler> logger)
    {
        _oauthProviderFactory = oauthProviderFactory;
        _validator = validator;
        _logger = logger;
    }

    public async Task<OAuthAuthorizationResponseDto> Handle(GetOAuthAuthorizationUrlCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating OAuth2 authorization URL for provider: {Provider}", request.Provider);

        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            _logger.LogWarning("OAuth2 authorization URL request validation failed: {Errors}", errors);
            throw new ValidationException(validationResult.Errors);
        }

        try
        {
            var provider = _oauthProviderFactory.CreateProvider(request.Provider);
            
            // Generate state if not provided for CSRF protection
            var state = request.State ?? GenerateSecureState();
            
            var authorizationUrl = provider.GetAuthorizationUrl(request.RedirectUri, state, request.Scopes);

            _logger.LogInformation("OAuth2 authorization URL generated successfully for provider: {Provider}", request.Provider);

            return new OAuthAuthorizationResponseDto
            {
                AuthorizationUrl = authorizationUrl,
                State = state,
                Provider = request.Provider
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating OAuth2 authorization URL for provider: {Provider}", request.Provider);
            throw;
        }
    }

    private static string GenerateSecureState()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
}

/// <summary>
/// Handler para processar callback OAuth2 e realizar login/registro
/// </summary>
public class ProcessOAuthCallbackHandler : IRequestHandler<ProcessOAuthCallbackCommand, OAuthLoginResponseDto>
{
    private readonly IOAuthProviderFactory _oauthProviderFactory;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _tokenService;
    private readonly IValidator<ProcessOAuthCallbackCommand> _validator;
    private readonly ILogger<ProcessOAuthCallbackHandler> _logger;

    public ProcessOAuthCallbackHandler(
        IOAuthProviderFactory oauthProviderFactory,
        IUserRepository userRepository,
        IJwtTokenService tokenService,
        IValidator<ProcessOAuthCallbackCommand> validator,
        ILogger<ProcessOAuthCallbackHandler> logger)
    {
        _oauthProviderFactory = oauthProviderFactory;
        _userRepository = userRepository;
        _tokenService = tokenService;
        _validator = validator;
        _logger = logger;
    }

    public async Task<OAuthLoginResponseDto> Handle(ProcessOAuthCallbackCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing OAuth2 callback for provider: {Provider}", request.Provider);

        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            _logger.LogWarning("OAuth2 callback validation failed: {Errors}", errors);
            return new OAuthLoginResponseDto
            {
                Success = false,
                Message = $"Validation failed: {errors}"
            };
        }

        try
        {
            var provider = _oauthProviderFactory.CreateProvider(request.Provider);
            
            // Exchange authorization code for user information
            var externalAuth = await provider.ExchangeCodeForTokenAsync(
                request.AuthorizationCode, 
                request.State, 
                request.RedirectUri, 
                cancellationToken);

            _logger.LogInformation("Successfully exchanged OAuth2 code for user info. Provider: {Provider}, Email: {Email}", 
                request.Provider, externalAuth.Email);

            // Try to find existing user by external provider ID
            var existingUser = await _userRepository.FindByExternalProviderAsync(request.Provider, externalAuth.ProviderId, cancellationToken);
            
            if (existingUser != null)
            {
                // User exists with this external provider
                existingUser.RecordLogin();
                await _userRepository.UpdateAsync(existingUser, cancellationToken);
                
                return await CreateSuccessResponseAsync(existingUser, false);
            }

            // Check if user exists with same email
            var userByEmail = await _userRepository.FindByEmailAsync(externalAuth.Email, cancellationToken);
            
            if (userByEmail != null)
            {
                // User exists with same email - link the external account
                userByEmail.SetExternalProvider(request.Provider, externalAuth.ProviderId);
                userByEmail.RecordLogin();
                await _userRepository.UpdateAsync(userByEmail, cancellationToken);
                
                _logger.LogInformation("Linked external provider {Provider} to existing user {UserId}", request.Provider, userByEmail.Id);
                
                return await CreateSuccessResponseAsync(userByEmail, false);
            }

            // Create new user
            var newUser = new Domain.Entities.User(
                Guid.NewGuid(),
                new Name(externalAuth.Name),
                new Domain.ValueObjects.Email(externalAuth.Email),
                true);
            
            newUser.SetExternalProvider(request.Provider, externalAuth.ProviderId);
            newUser.VerifyEmail(); // External providers usually have verified emails
            newUser.RecordLogin();
            
            await _userRepository.AddAsync(newUser, cancellationToken);
            
            _logger.LogInformation("Created new user from external provider {Provider}. UserId: {UserId}", request.Provider, newUser.Id);
            
            return await CreateSuccessResponseAsync(newUser, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OAuth2 callback for provider: {Provider}", request.Provider);
            return new OAuthLoginResponseDto
            {
                Success = false,
                Message = "Authentication failed. Please try again."
            };
        }
    }

    private async Task<OAuthLoginResponseDto> CreateSuccessResponseAsync(Domain.Entities.User user, bool isNewUser)
    {
        var accessToken = await _tokenService.GenerateAccessTokenAsync(user);
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user);

        return new OAuthLoginResponseDto
        {
            Success = true,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            User = new UserDto
            {
                Id = user.Id,
                Name = user.Name.ToString(),
                Email = user.Email.ToString(),
                IsActive = user.IsActive,
                EmailVerified = user.EmailVerified,
                HasExternalProvider = user.HasExternalProvider,
                ExternalProvider = user.ExternalProvider
            },
            Provider = user.ExternalProvider,
            IsNewUser = isNewUser
        };
    }
}

/// <summary>
/// Handler para vincular conta externa a usuário existente
/// </summary>
public class LinkExternalAccountHandler : IRequestHandler<LinkExternalAccountCommand, bool>
{
    private readonly IOAuthProviderFactory _oauthProviderFactory;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<LinkExternalAccountHandler> _logger;

    public LinkExternalAccountHandler(
        IOAuthProviderFactory oauthProviderFactory,
        IUserRepository userRepository,
        ILogger<LinkExternalAccountHandler> logger)
    {
        _oauthProviderFactory = oauthProviderFactory;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(LinkExternalAccountCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Linking external account {Provider} to user {UserId}", request.Provider, request.UserId);

        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for external account linking", request.UserId);
                return false;
            }

            var provider = _oauthProviderFactory.CreateProvider(request.Provider);
            var externalAuth = await provider.ExchangeCodeForTokenAsync(
                request.AuthorizationCode, 
                request.State, 
                request.RedirectUri, 
                cancellationToken);

            // Check if another user already has this external provider linked
            var existingUser = await _userRepository.FindByExternalProviderAsync(request.Provider, externalAuth.ProviderId, cancellationToken);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                _logger.LogWarning("External provider {Provider} already linked to different user", request.Provider);
                return false;
            }

            user.SetExternalProvider(request.Provider, externalAuth.ProviderId);
            await _userRepository.UpdateAsync(user, cancellationToken);

            _logger.LogInformation("Successfully linked external provider {Provider} to user {UserId}", request.Provider, request.UserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking external account {Provider} to user {UserId}", request.Provider, request.UserId);
            return false;
        }
    }
}

/// <summary>
/// Handler para desvincular conta externa
/// </summary>
public class UnlinkExternalAccountHandler : IRequestHandler<UnlinkExternalAccountCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UnlinkExternalAccountHandler> _logger;

    public UnlinkExternalAccountHandler(
        IUserRepository userRepository,
        ILogger<UnlinkExternalAccountHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(UnlinkExternalAccountCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Unlinking external account {Provider} from user {UserId}", request.Provider, request.UserId);

        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for external account unlinking", request.UserId);
                return false;
            }

            if (user.ExternalProvider != request.Provider)
            {
                _logger.LogWarning("User {UserId} does not have {Provider} linked", request.UserId, request.Provider);
                return false;
            }

            // Don't allow unlinking if user has no password (external-only user)
            if (user.IsExternalUser)
            {
                _logger.LogWarning("Cannot unlink external provider for external-only user {UserId}", request.UserId);
                return false;
            }

            user.ClearExternalProvider();
            await _userRepository.UpdateAsync(user, cancellationToken);

            _logger.LogInformation("Successfully unlinked external provider {Provider} from user {UserId}", request.Provider, request.UserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlinking external account {Provider} from user {UserId}", request.Provider, request.UserId);
            return false;
        }
    }
}