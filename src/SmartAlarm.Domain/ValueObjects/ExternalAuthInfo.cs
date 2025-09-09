using System;

namespace SmartAlarm.Domain.ValueObjects;

/// <summary>
/// Value object que representa informações de autenticação externa OAuth2
/// </summary>
public record ExternalAuthInfo
{
    public string Provider { get; init; }
    public string ProviderId { get; init; }
    public string Email { get; init; }
    public string Name { get; init; }
    public string? AvatarUrl { get; init; }
    public Dictionary<string, object>? AdditionalClaims { get; init; }

    public ExternalAuthInfo(string provider, string providerId, string email, string name, string? avatarUrl = null, Dictionary<string, object>? additionalClaims = null)
    {
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Provider cannot be null or empty", nameof(provider));
        if (string.IsNullOrWhiteSpace(providerId))
            throw new ArgumentException("Provider ID cannot be null or empty", nameof(providerId));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be null or empty", nameof(email));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty", nameof(name));

        Provider = provider;
        ProviderId = providerId;
        Email = email;
        Name = name;
        AvatarUrl = avatarUrl;
        AdditionalClaims = additionalClaims ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// Suported OAuth2 providers
    /// </summary>
    public static class SupportedProviders
    {
        public const string Google = "Google";
        public const string GitHub = "GitHub";
        public const string Facebook = "Facebook";
        public const string Microsoft = "Microsoft";

        public static readonly string[] All = { Google, GitHub, Facebook, Microsoft };

        public static bool IsSupported(string provider) =>
            All.Contains(provider, StringComparer.OrdinalIgnoreCase);
    }
}