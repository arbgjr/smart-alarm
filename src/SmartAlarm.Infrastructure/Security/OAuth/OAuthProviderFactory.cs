using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartAlarm.Domain.Abstractions;
using SmartAlarm.Domain.ValueObjects;

namespace SmartAlarm.Infrastructure.Security.OAuth;

/// <summary>
/// Factory para criar provedores OAuth2 específicos
/// </summary>
public class OAuthProviderFactory : IOAuthProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OAuthProviderFactory> _logger;

    public OAuthProviderFactory(
        IServiceProvider serviceProvider,
        ILogger<OAuthProviderFactory> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IOAuthProvider CreateProvider(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            throw new ArgumentException("Provider name cannot be null or empty", nameof(providerName));
        }

        if (!IsProviderSupported(providerName))
        {
            throw new ArgumentException($"OAuth provider '{providerName}' is not supported. Supported providers: {string.Join(", ", GetSupportedProviders())}", nameof(providerName));
        }

        try
        {
            IOAuthProvider provider = providerName.ToLowerInvariant() switch
            {
                "google" => _serviceProvider.GetRequiredService<GoogleOAuthProvider>(),
                "github" => _serviceProvider.GetRequiredService<GitHubOAuthProvider>(),
                "facebook" => _serviceProvider.GetRequiredService<FacebookOAuthProvider>(),
                "microsoft" => _serviceProvider.GetRequiredService<MicrosoftOAuthProvider>(),
                _ => throw new ArgumentException($"OAuth provider '{providerName}' is not implemented", nameof(providerName))
            };

            _logger.LogDebug("Created OAuth provider instance for: {ProviderName}", providerName);
            return provider;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating OAuth provider instance for: {ProviderName}", providerName);
            throw;
        }
    }

    public IEnumerable<string> GetSupportedProviders()
    {
        return ExternalAuthInfo.SupportedProviders.All;
    }

    public bool IsProviderSupported(string providerName)
    {
        return ExternalAuthInfo.SupportedProviders.IsSupported(providerName);
    }
}