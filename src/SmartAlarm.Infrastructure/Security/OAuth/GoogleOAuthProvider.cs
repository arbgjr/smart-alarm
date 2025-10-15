using System.Text.Json;
using Microsoft.Extensions.Logging;
using SmartAlarm.Domain.ValueObjects;
using SmartAlarm.KeyVault.Abstractions;

namespace SmartAlarm.Infrastructure.Security.OAuth;

/// <summary>
/// Provedor OAuth2 para Google
/// </summary>
public class GoogleOAuthProvider : BaseOAuthProvider
{
    public override string ProviderName => ExternalAuthInfo.SupportedProviders.Google;
    
    protected override string AuthorizationEndpoint => "https://accounts.google.com/o/oauth2/v2/auth";
    protected override string TokenEndpoint => "https://oauth2.googleapis.com/token";
    protected override string UserInfoEndpoint => "https://www.googleapis.com/oauth2/v2/userinfo";
    protected override string[] DefaultScopes => new[] { "openid", "email", "profile" };

    public GoogleOAuthProvider(
        HttpClient httpClient,
        IKeyVaultService keyVault,
        ILogger<GoogleOAuthProvider> logger) 
        : base(httpClient, keyVault, logger)
    {
    }

    protected override ExternalAuthInfo ParseUserInfo(JsonElement userData)
    {
        try
        {
            var id = GetRequiredStringProperty(userData, "id");
            var email = GetRequiredStringProperty(userData, "email");
            var name = GetRequiredStringProperty(userData, "name");
            var avatarUrl = GetOptionalStringProperty(userData, "picture");

            var additionalClaims = new Dictionary<string, object>();
            
            if (userData.TryGetProperty("given_name", out var givenName))
            {
                additionalClaims["given_name"] = givenName.GetString() ?? string.Empty;
            }
            
            if (userData.TryGetProperty("family_name", out var familyName))
            {
                additionalClaims["family_name"] = familyName.GetString() ?? string.Empty;
            }

            if (userData.TryGetProperty("locale", out var locale))
            {
                additionalClaims["locale"] = locale.GetString() ?? string.Empty;
            }

            if (userData.TryGetProperty("verified_email", out var verifiedEmail))
            {
                additionalClaims["email_verified"] = verifiedEmail.GetBoolean();
            }

            _logger.LogDebug("Successfully parsed Google user info for user: {Email}", email);

            return new ExternalAuthInfo(
                provider: ProviderName,
                providerId: id,
                email: email,
                name: name,
                avatarUrl: avatarUrl,
                additionalClaims: additionalClaims);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Google user info");
            throw new InvalidOperationException("Failed to parse Google user information", ex);
        }
    }
}