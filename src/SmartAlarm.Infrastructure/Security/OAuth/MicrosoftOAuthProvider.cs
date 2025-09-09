using System.Text.Json;
using Microsoft.Extensions.Logging;
using SmartAlarm.Domain.ValueObjects;
using SmartAlarm.KeyVault.Abstractions;

namespace SmartAlarm.Infrastructure.Security.OAuth;

/// <summary>
/// Provedor OAuth2 para Microsoft (Azure AD)
/// </summary>
public class MicrosoftOAuthProvider : BaseOAuthProvider
{
    public override string ProviderName => ExternalAuthInfo.SupportedProviders.Microsoft;
    
    protected override string AuthorizationEndpoint => "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";
    protected override string TokenEndpoint => "https://login.microsoftonline.com/common/oauth2/v2.0/token";
    protected override string UserInfoEndpoint => "https://graph.microsoft.com/v1.0/me";
    protected override string[] DefaultScopes => new[] { "openid", "profile", "email", "User.Read" };

    public MicrosoftOAuthProvider(
        HttpClient httpClient,
        IKeyVaultService keyVault,
        ILogger<MicrosoftOAuthProvider> logger) 
        : base(httpClient, keyVault, logger)
    {
    }

    public override string GetAuthorizationUrl(string redirectUri, string state, IEnumerable<string>? scopes = null)
    {
        try
        {
            var clientId = GetClientIdAsync().Result;
            var scopeList = scopes?.ToList() ?? DefaultScopes.ToList();
            var scopeString = string.Join(" ", scopeList);

            var parameters = new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["redirect_uri"] = redirectUri,
                ["response_type"] = "code",
                ["scope"] = scopeString,
                ["state"] = state,
                ["response_mode"] = "query",
                ["prompt"] = "consent"
            };

            var queryString = string.Join("&", 
                parameters.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

            var authUrl = $"{AuthorizationEndpoint}?{queryString}";
            
            _logger.LogDebug("Generated authorization URL for {Provider}", ProviderName);
            return authUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating authorization URL for {Provider}", ProviderName);
            throw;
        }
    }

    protected override ExternalAuthInfo ParseUserInfo(JsonElement userData)
    {
        try
        {
            var id = GetRequiredStringProperty(userData, "id");
            var displayName = GetOptionalStringProperty(userData, "displayName") ?? "Microsoft User";
            
            // Microsoft Graph might not always return email directly
            var email = GetOptionalStringProperty(userData, "mail") ?? 
                       GetOptionalStringProperty(userData, "userPrincipalName");
            
            if (string.IsNullOrEmpty(email))
            {
                throw new InvalidOperationException("Unable to retrieve email from Microsoft Graph API");
            }

            var additionalClaims = new Dictionary<string, object>();

            if (userData.TryGetProperty("givenName", out var givenName))
            {
                var givenNameValue = givenName.GetString();
                if (!string.IsNullOrEmpty(givenNameValue))
                {
                    additionalClaims["given_name"] = givenNameValue;
                }
            }

            if (userData.TryGetProperty("surname", out var surname))
            {
                var surnameValue = surname.GetString();
                if (!string.IsNullOrEmpty(surnameValue))
                {
                    additionalClaims["family_name"] = surnameValue;
                }
            }

            if (userData.TryGetProperty("jobTitle", out var jobTitle))
            {
                var jobTitleValue = jobTitle.GetString();
                if (!string.IsNullOrEmpty(jobTitleValue))
                {
                    additionalClaims["job_title"] = jobTitleValue;
                }
            }

            if (userData.TryGetProperty("companyName", out var companyName))
            {
                var companyNameValue = companyName.GetString();
                if (!string.IsNullOrEmpty(companyNameValue))
                {
                    additionalClaims["company"] = companyNameValue;
                }
            }

            if (userData.TryGetProperty("preferredLanguage", out var preferredLanguage))
            {
                var languageValue = preferredLanguage.GetString();
                if (!string.IsNullOrEmpty(languageValue))
                {
                    additionalClaims["locale"] = languageValue;
                }
            }

            if (userData.TryGetProperty("userPrincipalName", out var upn))
            {
                var upnValue = upn.GetString();
                if (!string.IsNullOrEmpty(upnValue))
                {
                    additionalClaims["user_principal_name"] = upnValue;
                }
            }

            _logger.LogDebug("Successfully parsed Microsoft user info for user: {Email}", email);

            return new ExternalAuthInfo(
                provider: ProviderName,
                providerId: id,
                email: email,
                name: displayName,
                avatarUrl: null, // Microsoft Graph photo endpoint requires separate call
                additionalClaims: additionalClaims);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Microsoft user info");
            throw new InvalidOperationException("Failed to parse Microsoft user information", ex);
        }
    }
}