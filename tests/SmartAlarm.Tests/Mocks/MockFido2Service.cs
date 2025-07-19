using SmartAlarm.Domain.Abstractions;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.ValueObjects;

namespace SmartAlarm.Tests.Mocks;

public class MockFido2Service : IFido2Service
{
    public Task<string> CreateCredentialRequestAsync(User user, string displayName)
    {
        // Retorna um JSON mock para as opções de criação de credencial
        var mockOptions = """
        {
            "rp": {"id": "localhost", "name": "SmartAlarm Test"},
            "user": {"id": "dGVzdC11c2VyLWlk", "name": "test@example.com", "displayName": "Test User"},
            "challenge": "Y2hhbGxlbmdlLWRhdGE",
            "pubKeyCredParams": [{"type": "public-key", "alg": -7}],
            "timeout": 60000,
            "attestation": "direct",
            "authenticatorSelection": {
                "authenticatorAttachment": "platform",
                "userVerification": "required"
            }
        }
        """;
        
        return Task.FromResult(mockOptions);
    }

    public Task<bool> CompleteCredentialRegistrationAsync(Guid userId, string attestationResponseJson, string sessionData)
    {
        // Mock sempre retorna sucesso
        return Task.FromResult(true);
    }

    public Task<string> CreateAuthenticationRequestAsync(string userEmail)
    {
        // Retorna um JSON mock para as opções de autenticação
        var mockOptions = """
        {
            "challenge": "YXV0aC1jaGFsbGVuZ2U",
            "timeout": 60000,
            "rpId": "localhost",
            "allowCredentials": [
                {
                    "type": "public-key",
                    "id": "Y3JlZGVudGlhbC1pZA",
                    "transports": ["internal"]
                }
            ],
            "userVerification": "required"
        }
        """;
        
        return Task.FromResult(mockOptions);
    }

    public Task<User?> CompleteAuthenticationAsync(string userEmail, string assertionResponseJson, string sessionData)
    {
        // Mock retorna um usuário válido se o email existir
        if (userEmail == "test@example.com")
        {
            var user = new User(
                Guid.NewGuid(), 
                "Test User", 
                userEmail, 
                true
            );
            return Task.FromResult<User?>(user);
        }
        
        return Task.FromResult<User?>(null);
    }

    public Task<IEnumerable<UserCredential>> GetUserCredentialsAsync(Guid userId)
    {
        // Retorna uma lista vazia de credenciais
        return Task.FromResult<IEnumerable<UserCredential>>(new List<UserCredential>());
    }

    public Task<bool> RemoveCredentialAsync(Guid userId, Guid credentialId)
    {
        // Mock sempre retorna sucesso
        return Task.FromResult(true);
    }
}
