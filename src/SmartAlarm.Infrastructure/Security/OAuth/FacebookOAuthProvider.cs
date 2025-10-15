using System.Text.Json;
using Microsoft.Extensions.Logging;
using SmartAlarm.Domain.ValueObjects;
using SmartAlarm.KeyVault.Abstractions;

namespace SmartAlarm.Infrastructure.Security.OAuth;

/// <summary>
/// Provedor OAuth2 para Facebook
/// </summary>
public class FacebookOAuthProvider : BaseOAuthProvider
{
    public override string ProviderName => ExternalAuthInfo.SupportedProviders.Facebook;
    
    protected override string AuthorizationEndpoint => "https://www.facebook.com/v18.0/dialog/oauth";
    protected override string TokenEndpoint => "https://graph.facebook.com/v18.0/oauth/access_token";
    protected override string UserInfoEndpoint => "https://graph.facebook.com/v18.0/me?fields=id,name,email,picture.type(large)";
    protected override string[] DefaultScopes => new[] { "email", "public_profile" };

    public FacebookOAuthProvider(
        HttpClient httpClient,
        IKeyVaultService keyVault,
        ILogger<FacebookOAuthProvider> logger) 
        : base(httpClient, keyVault, logger)
    {
    }

    public override string GetAuthorizationUrl(string redirectUri, string state, IEnumerable<string>? scopes = null)
    {
        // Facebook uses different parameter names
        try
        {
            var clientId = GetClientIdAsync().Result;
            var scopeList = scopes?.ToList() ?? DefaultScopes.ToList();
            var scopeString = string.Join(",", scopeList); // Facebook uses comma-separated scopes

            var parameters = new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["redirect_uri"] = redirectUri,
                ["response_type"] = "code",
                ["scope"] = scopeString,
                ["state"] = state
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

    protected override async Task<ExternalAuthInfo> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            // Facebook requires access_token as query parameter
            var userInfoUrl = $"{UserInfoEndpoint}&access_token={Uri.EscapeDataString(accessToken)}";
            
            using var request = new HttpRequestMessage(HttpMethod.Get, userInfoUrl);
            
            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var userContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var userData = JsonSerializer.Deserialize<JsonElement>(userContent);

            return ParseUserInfo(userData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user info from Facebook");
            throw;
        }
    }

    protected override ExternalAuthInfo ParseUserInfo(JsonElement userData)
    {
        try
        {
            var id = GetRequiredStringProperty(userData, "id");
            var email = GetOptionalStringProperty(userData, "email");
            var name = GetRequiredStringProperty(userData, "name");

            // Facebook email might not be available if user doesn't grant permission
            if (string.IsNullOrEmpty(email))
            {
                throw new InvalidOperationException("Email permission not granted by Facebook user");
            }

            string? avatarUrl = null;
            if (userData.TryGetProperty("picture", out var pictureElement) &&
                pictureElement.TryGetProperty("data", out var dataElement) &&
                dataElement.TryGetProperty("url", out var urlElement))
            {
                avatarUrl = urlElement.GetString();
            }

            var additionalClaims = new Dictionary<string, object>();

            // Facebook might provide additional fields based on permissions
            if (userData.TryGetProperty("first_name", out var firstName))
            {
                var firstNameValue = firstName.GetString();
                if (!string.IsNullOrEmpty(firstNameValue))
                {
                    additionalClaims["first_name"] = firstNameValue;
                }
            }

            if (userData.TryGetProperty("last_name", out var lastName))
            {
                var lastNameValue = lastName.GetString();
                if (!string.IsNullOrEmpty(lastNameValue))
                {
                    additionalClaims["last_name"] = lastNameValue;
                }
            }

            if (userData.TryGetProperty("locale", out var locale))
            {
                var localeValue = locale.GetString();
                if (!string.IsNullOrEmpty(localeValue))
                {
                    additionalClaims["locale"] = localeValue;
                }
            }

            _logger.LogDebug("Successfully parsed Facebook user info for user: {Email}", email);

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
            _logger.LogError(ex, "Error parsing Facebook user info");
            throw new InvalidOperationException("Failed to parse Facebook user information", ex);
        }
    }
}