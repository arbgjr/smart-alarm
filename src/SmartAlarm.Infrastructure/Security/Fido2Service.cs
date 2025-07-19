using System.Text;
using System.Text.Json;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.Extensions.Logging;
using SmartAlarm.Domain.Abstractions;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.KeyVault.Abstractions;

namespace SmartAlarm.Infrastructure.Security;

/// <summary>
/// Implementação do serviço FIDO2/WebAuthn
/// Seguindo princípios SOLID e Clean Architecture
/// </summary>
public class Fido2Service : IFido2Service
{
    private readonly IFido2 _fido2;
    private readonly IUserRepository _userRepository;
    private readonly IKeyVaultService _keyVault;
    private readonly ILogger<Fido2Service> _logger;

    public Fido2Service(
        IFido2 fido2,
        IUserRepository userRepository,
        IKeyVaultService keyVault,
        ILogger<Fido2Service> logger)
    {
        _fido2 = fido2 ?? throw new ArgumentNullException(nameof(fido2));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _keyVault = keyVault ?? throw new ArgumentNullException(nameof(keyVault));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> CreateCredentialRequestAsync(User user, string displayName)
    {
        await Task.CompletedTask;
        try
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Display name is required", nameof(displayName));

            var fido2User = new Fido2User
            {
                Id = Encoding.UTF8.GetBytes(user.Id.ToString()),
                Name = user.Email.ToString(),
                DisplayName = displayName
            };

            // Obter credenciais existentes do usuário
            var existingCredentials = user.Credentials?
                .Where(c => c.IsActive)
                .Select(c => new PublicKeyCredentialDescriptor(Convert.FromBase64String(c.CredentialId)))
                .ToList() ?? new List<PublicKeyCredentialDescriptor>();

            var options = _fido2.RequestNewCredential(fido2User, existingCredentials);

            _logger.LogInformation("FIDO2 credential request created for user {UserId}", user.Id);
            
            // Serializar as opções para JSON
            return JsonSerializer.Serialize(options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating FIDO2 credential request for user {UserId}", user?.Id);
            throw;
        }
    }

    public async Task<bool> CompleteCredentialRegistrationAsync(Guid userId, string attestationResponseJson, string sessionData)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(attestationResponseJson))
                throw new ArgumentException("Attestation response is required", nameof(attestationResponseJson));

            if (string.IsNullOrWhiteSpace(sessionData))
                throw new ArgumentException("Session data is required", nameof(sessionData));

            // Deserializar resposta de atestação
            var attestationResponse = JsonSerializer.Deserialize<AuthenticatorAttestationRawResponse>(attestationResponseJson);
            if (attestationResponse == null)
                throw new ArgumentException("Invalid attestation response format");

            // Deserializar opções da sessão
            var options = JsonSerializer.Deserialize<CredentialCreateOptions>(sessionData);
            if (options == null)
                throw new ArgumentException("Invalid session data format");

            // Buscar usuário
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for credential registration: {UserId}", userId);
                return false;
            }

            // Validar resposta de atestado
            var result = await _fido2.MakeNewCredentialAsync(attestationResponse, options, async (args, cancellationToken) =>
            {
                await Task.CompletedTask;
                // Verificar se credencial já existe
                var credentialIdBase64 = Convert.ToBase64String(args.CredentialId);
                var existingCredential = user.Credentials?
                    .FirstOrDefault(c => c.CredentialId == credentialIdBase64);

                return existingCredential == null;
            });

            if (result.Status != "ok")
            {
                _logger.LogWarning("FIDO2 credential registration failed for user {UserId}: {Error}", 
                    userId, result.ErrorMessage);
                return false;
            }

            // Criar nova credencial
            var credential = new UserCredential
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CredentialId = Convert.ToBase64String(result.Result?.CredentialId ?? Array.Empty<byte>()),
                PublicKey = result.Result?.PublicKey ?? Array.Empty<byte>(),
                UserHandle = result.Result?.User.Id ?? Array.Empty<byte>(),
                SignatureCounter = result.Result?.Counter ?? 0,
                CredType = result.Result?.CredType ?? "",
                AaGuid = result.Result?.Aaguid.ToString() ?? "",
                DeviceName = "WebAuthn Device",
                IsActive = true
            };

            // Adicionar credencial ao usuário
            user.AddCredential(credential);

            // Salvar usuário com nova credencial
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("FIDO2 credential registered successfully for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing FIDO2 credential registration for user {UserId}", userId);
            return false;
        }
    }

    public async Task<string> CreateAuthenticationRequestAsync(string userEmail)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userEmail))
                throw new ArgumentException("User email is required", nameof(userEmail));

            // Buscar usuário
            var user = await _userRepository.GetByEmailAsync(userEmail);
            if (user == null)
            {
                _logger.LogWarning("User not found for authentication request: {UserEmail}", userEmail);
                throw new ArgumentException("User not found");
            }

            // Obter credenciais do usuário
            var allowedCredentials = user.Credentials?
                .Where(c => c.IsActive)
                .Select(c => new PublicKeyCredentialDescriptor(Convert.FromBase64String(c.CredentialId)))
                .ToList() ?? new List<PublicKeyCredentialDescriptor>();

            if (!allowedCredentials.Any())
            {
                _logger.LogWarning("No active credentials found for user {UserEmail}", userEmail);
                throw new ArgumentException("No credentials found for user");
            }

            var options = _fido2.GetAssertionOptions(allowedCredentials, UserVerificationRequirement.Preferred);

            _logger.LogInformation("FIDO2 authentication request created for user {UserEmail}", userEmail);
            
            // Serializar as opções para JSON
            return JsonSerializer.Serialize(options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating FIDO2 authentication request for user {UserEmail}", userEmail);
            throw;
        }
    }

    public async Task<User?> CompleteAuthenticationAsync(string userEmail, string assertionResponseJson, string sessionData)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userEmail))
                throw new ArgumentException("User email is required", nameof(userEmail));

            if (string.IsNullOrWhiteSpace(assertionResponseJson))
                throw new ArgumentException("Assertion response is required", nameof(assertionResponseJson));

            if (string.IsNullOrWhiteSpace(sessionData))
                throw new ArgumentException("Session data is required", nameof(sessionData));

            // Deserializar resposta de asserção
            var assertionResponse = JsonSerializer.Deserialize<AuthenticatorAssertionRawResponse>(assertionResponseJson);
            if (assertionResponse == null)
                throw new ArgumentException("Invalid assertion response format");

            // Deserializar opções da sessão
            var options = JsonSerializer.Deserialize<AssertionOptions>(sessionData);
            if (options == null)
                throw new ArgumentException("Invalid session data format");

            // Buscar usuário
            var user = await _userRepository.GetByEmailAsync(userEmail);
            if (user == null)
            {
                _logger.LogWarning("User not found for authentication: {UserEmail}", userEmail);
                return null;
            }

            // Encontrar credencial correspondente
            var credentialIdBase64 = Convert.ToBase64String(assertionResponse.Id);
            var credential = user.Credentials?
                .FirstOrDefault(c => c.CredentialId == credentialIdBase64 && c.IsActive);

            if (credential == null)
            {
                _logger.LogWarning("Credential not found for user {UserEmail}", userEmail);
                return null;
            }

            // Validar resposta de asserção
            var result = await _fido2.MakeAssertionAsync(assertionResponse, options, credential.PublicKey, credential.SignatureCounter, async (args, cancellationToken) =>
            {
                await Task.CompletedTask;
                // Verificar se a credencial ainda é válida
                return credential.IsActive;
            });

            if (result.Status != "ok")
            {
                _logger.LogWarning("FIDO2 authentication failed for user {UserEmail}: {Error}", 
                    userEmail, result.ErrorMessage);
                return null;
            }

            // Atualizar contador de assinatura
            credential.SignatureCounter = result.Counter;
            credential.LastUsedAt = DateTime.UtcNow;

            // Salvar alterações
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("FIDO2 authentication successful for user {UserEmail}", userEmail);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing FIDO2 authentication for user {UserEmail}", userEmail);
            return null;
        }
    }

    public async Task<IEnumerable<UserCredential>> GetUserCredentialsAsync(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user?.Credentials == null)
                return Enumerable.Empty<UserCredential>();

            return user.Credentials.Where(c => c.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving credentials for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> RemoveCredentialAsync(Guid userId, Guid credentialId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user?.Credentials == null)
                return false;

            user.RemoveCredential(credentialId);
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Credential {CredentialId} removed for user {UserId}", credentialId, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing credential {CredentialId} for user {UserId}", credentialId, userId);
            return false;
        }
    }
}
