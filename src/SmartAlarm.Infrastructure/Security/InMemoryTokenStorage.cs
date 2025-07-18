using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace SmartAlarm.Infrastructure.Security
{
    /// <summary>
    /// Implementação em memória do token storage para desenvolvimento
    /// Em produção, seria substituído por Redis ou banco de dados
    /// </summary>
    public class InMemoryTokenStorage : ITokenStorage, IDisposable
    {
        private readonly ILogger<InMemoryTokenStorage> _logger;
        private readonly ConcurrentDictionary<string, DateTime> _revokedTokens = new();
        private readonly ConcurrentDictionary<string, RefreshTokenInfo> _refreshTokens = new();
        private readonly Timer _cleanupTimer;

        public InMemoryTokenStorage(ILogger<InMemoryTokenStorage> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Limpeza automática a cada 5 minutos
            _cleanupTimer = new Timer(CleanupExpiredTokens, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        public Task<bool> IsTokenRevokedAsync(string tokenId)
        {
            if (string.IsNullOrWhiteSpace(tokenId))
                return Task.FromResult(false);

            var isRevoked = _revokedTokens.ContainsKey(tokenId);
            _logger.LogDebug("Token {TokenId} revocation status: {IsRevoked}", tokenId, isRevoked);
            
            return Task.FromResult(isRevoked);
        }

        public Task RevokeTokenAsync(string tokenId, TimeSpan expiration)
        {
            if (string.IsNullOrWhiteSpace(tokenId))
                return Task.CompletedTask;

            var expirationTime = DateTime.UtcNow.Add(expiration);
            _revokedTokens.TryAdd(tokenId, expirationTime);
            
            _logger.LogInformation("Token {TokenId} revoked until {ExpirationTime}", tokenId, expirationTime);
            
            return Task.CompletedTask;
        }

        public Task<bool> StoreRefreshTokenAsync(string tokenId, Guid userId, TimeSpan expiration)
        {
            if (string.IsNullOrWhiteSpace(tokenId))
                return Task.FromResult(false);

            var tokenInfo = new RefreshTokenInfo
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(expiration)
            };

            var stored = _refreshTokens.TryAdd(tokenId, tokenInfo);
            
            if (stored)
            {
                _logger.LogDebug("Refresh token {TokenId} stored for user {UserId}", tokenId, userId);
            }
            
            return Task.FromResult(stored);
        }

        public Task<bool> ValidateRefreshTokenAsync(string tokenId)
        {
            if (string.IsNullOrWhiteSpace(tokenId))
                return Task.FromResult(false);

            if (!_refreshTokens.TryGetValue(tokenId, out var tokenInfo))
            {
                _logger.LogDebug("Refresh token {TokenId} not found", tokenId);
                return Task.FromResult(false);
            }

            if (DateTime.UtcNow > tokenInfo.ExpiresAt)
            {
                _logger.LogDebug("Refresh token {TokenId} expired", tokenId);
                _refreshTokens.TryRemove(tokenId, out _);
                return Task.FromResult(false);
            }

            _logger.LogDebug("Refresh token {TokenId} is valid", tokenId);
            return Task.FromResult(true);
        }

        public Task InvalidateRefreshTokenAsync(string tokenId)
        {
            if (string.IsNullOrWhiteSpace(tokenId))
                return Task.CompletedTask;

            var removed = _refreshTokens.TryRemove(tokenId, out _);
            
            if (removed)
            {
                _logger.LogDebug("Refresh token {TokenId} invalidated", tokenId);
            }
            
            return Task.CompletedTask;
        }

        private void CleanupExpiredTokens(object? state)
        {
            var now = DateTime.UtcNow;
            var expiredTokens = new List<string>();

            // Limpar tokens revogados expirados
            foreach (var kvp in _revokedTokens)
            {
                if (now > kvp.Value)
                {
                    expiredTokens.Add(kvp.Key);
                }
            }

            foreach (var tokenId in expiredTokens)
            {
                _revokedTokens.TryRemove(tokenId, out _);
            }

            // Limpar refresh tokens expirados
            var expiredRefreshTokens = new List<string>();
            foreach (var kvp in _refreshTokens)
            {
                if (now > kvp.Value.ExpiresAt)
                {
                    expiredRefreshTokens.Add(kvp.Key);
                }
            }

            foreach (var tokenId in expiredRefreshTokens)
            {
                _refreshTokens.TryRemove(tokenId, out _);
            }

            if (expiredTokens.Count > 0 || expiredRefreshTokens.Count > 0)
            {
                _logger.LogDebug("Cleaned up {RevokedCount} revoked tokens and {RefreshCount} refresh tokens", 
                    expiredTokens.Count, expiredRefreshTokens.Count);
            }
        }

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
        }

        private class RefreshTokenInfo
        {
            public Guid UserId { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime ExpiresAt { get; set; }
        }
    }
}
