using SmartAlarm.Domain.ValueObjects;

namespace SmartAlarm.Domain.Abstractions;

/// <summary>
/// Abstração para provedores OAuth2 externos
/// </summary>
public interface IOAuthProvider
{
    /// <summary>
    /// Nome do provedor (Google, GitHub, Facebook, Microsoft)
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Valida token de autorização e obtém informações do usuário
    /// </summary>
    /// <param name="authorizationCode">Código de autorização recebido do provider</param>
    /// <param name="state">State parameter para validação CSRF</param>
    /// <param name="redirectUri">URI de redirecionamento configurada</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Informações do usuário autenticado</returns>
    Task<ExternalAuthInfo> ExchangeCodeForTokenAsync(string authorizationCode, string? state, string redirectUri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gera URL de autorização para iniciar fluxo OAuth2
    /// </summary>
    /// <param name="redirectUri">URI de redirecionamento</param>
    /// <param name="state">State parameter para proteção CSRF</param>
    /// <param name="scopes">Scopes adicionais (opcional)</param>
    /// <returns>URL de autorização</returns>
    string GetAuthorizationUrl(string redirectUri, string state, IEnumerable<string>? scopes = null);

    /// <summary>
    /// Valida token de acesso e obtém informações atualizadas do usuário
    /// </summary>
    /// <param name="accessToken">Token de acesso</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Informações atualizadas do usuário</returns>
    Task<ExternalAuthInfo> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken = default);
}