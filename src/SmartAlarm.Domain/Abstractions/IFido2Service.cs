using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Domain.Abstractions;

/// <summary>
/// Interface para serviços FIDO2/WebAuthn
/// Seguindo princípios de Clean Architecture - colocada no Domain
/// </summary>
public interface IFido2Service
{
    /// <summary>
    /// Cria opções para registro de credencial FIDO2
    /// </summary>
    /// <param name="user">Usuário</param>
    /// <param name="displayName">Nome para exibição</param>
    /// <returns>Dados serializados das opções de criação de credencial</returns>
    Task<string> CreateCredentialRequestAsync(User user, string displayName);

    /// <summary>
    /// Completa o registro de credencial FIDO2
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <param name="attestationResponseJson">Resposta de atestação em JSON</param>
    /// <param name="sessionData">Dados da sessão</param>
    /// <returns>Sucesso do registro</returns>
    Task<bool> CompleteCredentialRegistrationAsync(Guid userId, string attestationResponseJson, string sessionData);

    /// <summary>
    /// Cria opções para autenticação FIDO2
    /// </summary>
    /// <param name="userEmail">Email do usuário</param>
    /// <returns>Dados serializados das opções de autenticação</returns>
    Task<string> CreateAuthenticationRequestAsync(string userEmail);

    /// <summary>
    /// Completa a autenticação FIDO2
    /// </summary>
    /// <param name="userEmail">Email do usuário</param>
    /// <param name="assertionResponseJson">Resposta de asserção em JSON</param>
    /// <param name="sessionData">Dados da sessão</param>
    /// <returns>Usuário autenticado se sucesso, null caso contrário</returns>
    Task<User?> CompleteAuthenticationAsync(string userEmail, string assertionResponseJson, string sessionData);

    /// <summary>
    /// Lista credenciais FIDO2 do usuário
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <returns>Lista de credenciais</returns>
    Task<IEnumerable<UserCredential>> GetUserCredentialsAsync(Guid userId);

    /// <summary>
    /// Remove uma credencial FIDO2
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <param name="credentialId">ID da credencial</param>
    /// <returns>Sucesso da remoção</returns>
    Task<bool> RemoveCredentialAsync(Guid userId, Guid credentialId);
}
