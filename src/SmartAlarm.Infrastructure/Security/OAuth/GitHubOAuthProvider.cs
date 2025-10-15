using System.Text.Json;
using Microsoft.Extensions.Logging;
using SmartAlarm.Domain.ValueObjects;
using SmartAlarm.KeyVault.Abstractions;

namespace SmartAlarm.Infrastructure.Security.OAuth;

/// <summary>
/// Provedor OAuth2 para GitHub
/// </summary>
public class GitHubOAuthProvider : BaseOAuthProvider
{
    public override string ProviderName => ExternalAuthInfo.SupportedProviders.GitHub;
    
    protected override string AuthorizationEndpoint => "https://github.com/login/oauth/authorize";
    protected override string TokenEndpoint => "https://github.com/login/oauth/access_token";
    protected override string UserInfoEndpoint => "https://api.github.com/user";
    protected override string[] DefaultScopes => new[] { "user:email", "read:user" };

    private readonly string _userEmailEndpoint = "https://api.github.com/user/emails";

    public GitHubOAuthProvider(
        HttpClient httpClient,
        IKeyVaultService keyVault,
        ILogger<GitHubOAuthProvider> logger) 
        : base(httpClient, keyVault, logger)
    {
    }

    public override string GetAuthorizationUrl(string redirectUri, string state, IEnumerable<string>? scopes = null)
    {
        // GitHub doesn't use 'access_type' and 'prompt' parameters
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
            // Get user basic info
            var userInfo = await GetUserBasicInfo(accessToken, cancellationToken);
            
            // Get user primary email (GitHub API requires separate call)
            var email = await GetUserPrimaryEmail(accessToken, cancellationToken);

            return ParseUserInfoWithEmail(userInfo, email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting GitHub user info");
            throw;
        }
    }

    private async Task<JsonElement> GetUserBasicInfo(string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, UserInfoEndpoint);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.UserAgent.ParseAdd("SmartAlarm-OAuth/1.0"); // GitHub requires User-Agent

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<JsonElement>(content);
    }

    private async Task<string> GetUserPrimaryEmail(string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, _userEmailEndpoint);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.UserAgent.ParseAdd("SmartAlarm-OAuth/1.0");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var emails = JsonSerializer.Deserialize<JsonElement>(content);

        if (emails.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Invalid email response from GitHub API");
        }

        // Find primary email
        foreach (var emailObj in emails.EnumerateArray())
        {
            if (emailObj.TryGetProperty("primary", out var primaryProp) && primaryProp.GetBoolean())
            {
                if (emailObj.TryGetProperty("email", out var emailProp))
                {
                    var email = emailProp.GetString();
                    if (!string.IsNullOrEmpty(email))
                    {
                        return email;
                    }
                }
            }
        }

        // Fallback: get first verified email
        foreach (var emailObj in emails.EnumerateArray())
        {
            if (emailObj.TryGetProperty("verified", out var verifiedProp) && verifiedProp.GetBoolean())
            {
                if (emailObj.TryGetProperty("email", out var emailProp))
                {
                    var email = emailProp.GetString();
                    if (!string.IsNullOrEmpty(email))
                    {
                        return email;
                    }
                }
            }
        }

        throw new InvalidOperationException("No verified email found for GitHub user");
    }

    protected override ExternalAuthInfo ParseUserInfo(JsonElement userData)
    {
        // This method won't be called directly, but we need to implement it
        throw new NotImplementedException("Use ParseUserInfoWithEmail for GitHub");
    }

    private ExternalAuthInfo ParseUserInfoWithEmail(JsonElement userData, string email)
    {
        try
        {
            var id = GetRequiredStringProperty(userData, "id");
            var name = GetOptionalStringProperty(userData, "name") ?? GetRequiredStringProperty(userData, "login");
            var avatarUrl = GetOptionalStringProperty(userData, "avatar_url");

            var additionalClaims = new Dictionary<string, object>();
            
            if (userData.TryGetProperty("login", out var login))
            {
                additionalClaims["username"] = login.GetString() ?? string.Empty;
            }

            if (userData.TryGetProperty("bio", out var bio))
            {
                var bioValue = bio.GetString();
                if (!string.IsNullOrEmpty(bioValue))
                {
                    additionalClaims["bio"] = bioValue;
                }
            }

            if (userData.TryGetProperty("location", out var location))
            {
                var locationValue = location.GetString();
                if (!string.IsNullOrEmpty(locationValue))
                {
                    additionalClaims["location"] = locationValue;
                }
            }

            if (userData.TryGetProperty("company", out var company))
            {
                var companyValue = company.GetString();
                if (!string.IsNullOrEmpty(companyValue))
                {
                    additionalClaims["company"] = companyValue;
                }
            }

            _logger.LogDebug("Successfully parsed GitHub user info for user: {Email}", email);

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
            _logger.LogError(ex, "Error parsing GitHub user info");
            throw new InvalidOperationException("Failed to parse GitHub user information", ex);
        }
    }
}