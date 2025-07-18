namespace SmartAlarm.Infrastructure.Security
{
    /// <summary>
    /// Interface para armazenamento de tokens revogados
    /// </summary>
    public interface ITokenStorage
    {
        Task<bool> IsTokenRevokedAsync(string tokenId);
        Task RevokeTokenAsync(string tokenId, TimeSpan expiration);
        Task<bool> StoreRefreshTokenAsync(string tokenId, Guid userId, TimeSpan expiration);
        Task<bool> ValidateRefreshTokenAsync(string tokenId);
        Task InvalidateRefreshTokenAsync(string tokenId);
    }
}
