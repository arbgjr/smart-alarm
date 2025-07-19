using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace SmartAlarm.Infrastructure.Security
{
    /// <summary>
    /// Implementação de token storage distribuído usando Redis
    /// para suporte a revogação de tokens em cenários de múltiplas instâncias.
    /// </summary>
    public class DistributedTokenStorage : ITokenStorage, IDisposable
    {
        private readonly ILogger<DistributedTokenStorage> _logger;
        private readonly ConnectionMultiplexer _redis;
        private readonly IDatabase _database;
        private const string BlacklistKeyPrefix = "jwt:blacklist:";
        private const string RefreshTokenKeyPrefix = "jwt:refresh:";
        private const string UserTokensKeyPrefix = "jwt:user:";

        public DistributedTokenStorage(ILogger<DistributedTokenStorage> logger, string connectionString)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentException("Redis connection string cannot be null or empty", nameof(connectionString));

            try
            {
                _redis = ConnectionMultiplexer.Connect(connectionString);
                _database = _redis.GetDatabase();
                
                _logger.LogInformation("Connected to Redis for distributed token storage at {ConnectionString}", 
                    connectionString.Split(',')[0]); // Log apenas o primeiro endpoint por segurança
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to Redis at {ConnectionString}", connectionString);
                throw;
            }
        }

        public async Task<bool> IsTokenRevokedAsync(string tokenId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tokenId))
                    return false;

                var key = BlacklistKeyPrefix + tokenId;
                var result = await _database.KeyExistsAsync(key);
                
                if (result)
                {
                    _logger.LogDebug("Token {TokenId} found in distributed blacklist", tokenId);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check token revocation status for {TokenId} in distributed storage", tokenId);
                // Em caso de falha na verificação, consideramos o token como revogado por segurança
                return true;
            }
        }

        public async Task RevokeTokenAsync(string tokenId, TimeSpan expiration)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tokenId))
                    return;

                var key = BlacklistKeyPrefix + tokenId;
                await _database.StringSetAsync(key, "revoked", expiration);
                _logger.LogInformation("Token {TokenId} added to distributed blacklist with expiry {Expiration}", tokenId, expiration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to revoke token {TokenId} in distributed storage", tokenId);
                throw;
            }
        }

        public async Task<bool> StoreRefreshTokenAsync(string tokenId, Guid userId, TimeSpan expiration)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tokenId))
                    return false;

                var key = RefreshTokenKeyPrefix + tokenId;
                var value = $"{userId}:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
                
                var result = await _database.StringSetAsync(key, value, expiration);
                
                if (result)
                {
                    _logger.LogInformation("Refresh token {TokenId} stored for user {UserId} with expiry {Expiration}", 
                        tokenId, userId, expiration);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store refresh token {TokenId} for user {UserId} in distributed storage", 
                    tokenId, userId);
                return false;
            }
        }

        public async Task<bool> ValidateRefreshTokenAsync(string tokenId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tokenId))
                    return false;

                var key = RefreshTokenKeyPrefix + tokenId;
                var value = await _database.StringGetAsync(key);
                
                var isValid = value.HasValue;
                
                if (isValid)
                {
                    _logger.LogDebug("Refresh token {TokenId} is valid", tokenId);
                }
                else
                {
                    _logger.LogDebug("Refresh token {TokenId} not found or expired", tokenId);
                }
                
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate refresh token {TokenId} in distributed storage", tokenId);
                return false;
            }
        }

        public async Task InvalidateRefreshTokenAsync(string tokenId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tokenId))
                    return;

                var key = RefreshTokenKeyPrefix + tokenId;
                var result = await _database.KeyDeleteAsync(key);
                
                if (result)
                {
                    _logger.LogInformation("Refresh token {TokenId} invalidated in distributed storage", tokenId);
                }
                else
                {
                    _logger.LogWarning("Refresh token {TokenId} was not found for invalidation", tokenId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to invalidate refresh token {TokenId} in distributed storage", tokenId);
                throw;
            }
        }

        public async Task RevokeAllUserTokensAsync(string userId)
        {
            try
            {
                var userKey = UserTokensKeyPrefix + userId;
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                
                // Definir um timestamp de revogação para todos os tokens do usuário
                // Tokens emitidos antes deste timestamp serão considerados inválidos
                await _database.StringSetAsync(userKey, timestamp, TimeSpan.FromDays(30)); // Manter por 30 dias
                
                _logger.LogInformation("All tokens for user {UserId} revoked in distributed storage at {Timestamp}", 
                    userId, timestamp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to revoke all tokens for user {UserId} in distributed storage", userId);
                throw;
            }
        }

        public async Task<bool> AreUserTokensRevokedAsync(string userId, DateTimeOffset tokenIssuedAt)
        {
            try
            {
                var userKey = UserTokensKeyPrefix + userId;
                var revokedAtString = await _database.StringGetAsync(userKey);
                
                if (!revokedAtString.HasValue)
                {
                    return false; // Nenhuma revogação global para este usuário
                }
                
                if (long.TryParse(revokedAtString, out var revokedAtTimestamp))
                {
                    var revokedAt = DateTimeOffset.FromUnixTimeSeconds(revokedAtTimestamp);
                    var isRevoked = tokenIssuedAt < revokedAt;
                    
                    if (isRevoked)
                    {
                        _logger.LogDebug("Token for user {UserId} is revoked (issued at {IssuedAt}, revoked at {RevokedAt})",
                            userId, tokenIssuedAt, revokedAt);
                    }
                    
                    return isRevoked;
                }
                
                _logger.LogWarning("Invalid revocation timestamp for user {UserId}: {RevokedAtString}", 
                    userId, revokedAtString);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check user token revocation status for {UserId} in distributed storage", userId);
                // Em caso de falha, consideramos não revogado para não bloquear usuários desnecessariamente
                return false;
            }
        }

        public void Dispose()
        {
            try
            {
                _redis?.Dispose();
                _logger.LogInformation("Distributed token storage connection disposed");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing distributed token storage connection");
            }
        }
    }
}
