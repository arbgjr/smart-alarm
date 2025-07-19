using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Domain.Abstractions;

/// <summary>
/// Interface para serviços de token JWT
/// Seguindo princípios de Clean Architecture - colocada no Domain
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Gera token de acesso JWT para o usuário
    /// </summary>
    /// <param name="user">Usuário</param>
    /// <returns>Token JWT</returns>
    Task<string> GenerateAccessTokenAsync(User user);

    /// <summary>
    /// Gera token de refresh para o usuário
    /// </summary>
    /// <param name="user">Usuário</param>
    /// <returns>Token de refresh</returns>
    Task<string> GenerateRefreshTokenAsync(User user);

    /// <summary>
    /// Valida token JWT
    /// </summary>
    /// <param name="token">Token a ser validado</param>
    /// <returns>Verdadeiro se válido</returns>
    Task<bool> ValidateTokenAsync(string token);

    /// <summary>
    /// Obtém usuário a partir do token
    /// </summary>
    /// <param name="token">Token JWT</param>
    /// <returns>ID do usuário se válido, null caso contrário</returns>
    Task<Guid?> GetUserIdFromTokenAsync(string token);

    /// <summary>
    /// Revoga token (adiciona à blacklist)
    /// </summary>
    /// <param name="token">Token a ser revogado</param>
    /// <returns>Sucesso da operação</returns>
    Task<bool> RevokeTokenAsync(string token);

    /// <summary>
    /// Renova token usando refresh token
    /// </summary>
    /// <param name="refreshToken">Token de refresh</param>
    /// <returns>Novo token de acesso se válido, null caso contrário</returns>
    Task<string?> RefreshTokenAsync(string refreshToken);
}
