using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SmartAlarm.Domain.Abstractions;
using SmartAlarm.Domain.ValueObjects;
using SmartAlarm.KeyVault.Abstractions;

namespace SmartAlarm.Infrastructure.Security.OAuth;

/// <summary>
/// Classe base para provedores OAuth2 com funcionalidades comuns
/// </summary>
public abstract class BaseOAuthProvider : IOAuthProvider
{
    protected readonly HttpClient _httpClient;
    protected readonly IKeyVaultService _keyVault;
    protected readonly ILogger _logger;

    public abstract string ProviderName { get; }
    protected abstract string AuthorizationEndpoint { get; }
    protected abstract string TokenEndpoint { get; }
    protected abstract string UserInfoEndpoint { get; }
    protected abstract string[] DefaultScopes { get; }

    protected BaseOAuthProvider(
        HttpClient httpClient,
        IKeyVaultService keyVault,
        ILogger logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _keyVault = keyVault ?? throw new ArgumentNullException(nameof(keyVault));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public virtual string GetAuthorizationUrl(string redirectUri, string state, IEnumerable<string>? scopes = null)
    {
        try
        {
            var clientId = GetClientIdAsync().Result; // In real scenario, make this async
            var scopeList = scopes?.ToList() ?? DefaultScopes.ToList();
            var scopeString = string.Join(" ", scopeList);

            var parameters = new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["redirect_uri"] = redirectUri,
                ["response_type"] = "code",
                ["scope"] = scopeString,
                ["state"] = state,
                ["access_type"] = "offline", // For refresh tokens
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

    public virtual async Task<ExternalAuthInfo> ExchangeCodeForTokenAsync(
        string authorizationCode, 
        string? state, 
        string redirectUri, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Exchanging authorization code for token - Provider: {Provider}", ProviderName);

            var clientId = await GetClientIdAsync();
            var clientSecret = await GetClientSecretAsync();

            var tokenRequest = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("code", authorizationCode),
                new KeyValuePair<string, string>("redirect_uri", redirectUri)
            });

            var tokenResponse = await _httpClient.PostAsync(TokenEndpoint, tokenRequest, cancellationToken);
            tokenResponse.EnsureSuccessStatusCode();

            var tokenContent = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
            var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenContent);

            if (!tokenData.TryGetProperty("access_token", out var accessTokenElement))
            {
                throw new InvalidOperationException("Access token not found in token response");
            }

            var accessToken = accessTokenElement.GetString();
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new InvalidOperationException("Access token is null or empty");
            }

            return await GetUserInfoAsync(accessToken, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exchanging authorization code for token - Provider: {Provider}", ProviderName);
            throw;
        }
    }

    public virtual async Task<ExternalAuthInfo> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Validating access token - Provider: {Provider}", ProviderName);
            return await GetUserInfoAsync(accessToken, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating access token - Provider: {Provider}", ProviderName);
            throw;
        }
    }

    protected virtual async Task<ExternalAuthInfo> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, UserInfoEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var userContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var userData = JsonSerializer.Deserialize<JsonElement>(userContent);

            return ParseUserInfo(userData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user info - Provider: {Provider}", ProviderName);
            throw;
        }
    }

    protected abstract ExternalAuthInfo ParseUserInfo(JsonElement userData);

    protected virtual async Task<string> GetClientIdAsync()
    {
        var clientId = await _keyVault.GetSecretAsync($"{ProviderName.ToLower()}-client-id");
        if (string.IsNullOrEmpty(clientId))
        {
            throw new InvalidOperationException($"{ProviderName} client ID not configured in KeyVault");
        }
        return clientId;
    }

    protected virtual async Task<string> GetClientSecretAsync()
    {
        var clientSecret = await _keyVault.GetSecretAsync($"{ProviderName.ToLower()}-client-secret");
        if (string.IsNullOrEmpty(clientSecret))
        {
            throw new InvalidOperationException($"{ProviderName} client secret not configured in KeyVault");
        }
        return clientSecret;
    }

    protected static string GetRequiredStringProperty(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            throw new InvalidOperationException($"Required property '{propertyName}' not found in user info");
        }

        var value = property.GetString();
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException($"Required property '{propertyName}' is null or empty");
        }

        return value;
    }

    protected static string? GetOptionalStringProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) ? property.GetString() : null;
    }
}