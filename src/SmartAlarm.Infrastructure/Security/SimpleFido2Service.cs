using Microsoft.Extensions.Logging;
using SmartAlarm.Domain.Abstractions;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Infrastructure.Security;

/// <summary>
/// Implementação simplificada temporária do serviço FIDO2/WebAuthn
/// </summary>
public class SimpleFido2Service : IFido2Service
{
    private readonly ILogger<SimpleFido2Service> _logger;

    public SimpleFido2Service(ILogger<SimpleFido2Service> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<string> CreateCredentialRequestAsync(User user, string displayName)
    {
        _logger.LogInformation("Creating credential request for user {UserId}", user.Id);
        return Task.FromResult("simple-credential-request");
    }

    public Task<bool> CompleteCredentialRegistrationAsync(Guid userId, string attestationResponseJson, string sessionData)
    {
        _logger.LogInformation("Completing credential registration for user {UserId}", userId);
        return Task.FromResult(true);
    }

    public Task<string> CreateAuthenticationRequestAsync(string userEmail)
    {
        _logger.LogInformation("Creating authentication request for user {UserEmail}", userEmail);
        return Task.FromResult("simple-auth-request");
    }

    public Task<User?> CompleteAuthenticationAsync(string userEmail, string assertionResponseJson, string sessionData)
    {
        _logger.LogInformation("Completing authentication for user {UserEmail}", userEmail);
        return Task.FromResult<User?>(null);
    }

    public Task<IEnumerable<UserCredential>> GetUserCredentialsAsync(Guid userId)
    {
        _logger.LogInformation("Getting credentials for user {UserId}", userId);
        return Task.FromResult(Enumerable.Empty<UserCredential>());
    }

    public Task<bool> RemoveCredentialAsync(Guid userId, Guid credentialId)
    {
        _logger.LogInformation("Removing credential {CredentialId} for user {UserId}", credentialId, userId);
        return Task.FromResult(true);
    }
}
