using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Logging;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;

namespace SmartAlarm.Infrastructure.Security
{
    /// <summary>
    /// Implementação enterprise do serviço JWT Blocklist usando Redis para distribuição
    /// </summary>
    public class RedisJwtBlocklistService : IJwtBlocklistService, IDisposable
    {
        private readonly ILogger<RedisJwtBlocklistService> _logger;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly ConnectionMultiplexer _redis;
        private readonly IDatabase _database;
        private bool _disposed = false;

        // Prefixos Redis para organização
        private const string BlocklistKeyPrefix = "jwt:blocklist:";
        private const string UserTokensKeyPrefix = "jwt:user_tokens:";
        private const string StatisticsKey = "jwt:blocklist:stats";
        private const string CleanupLockKey = "jwt:blocklist:cleanup_lock";

        public RedisJwtBlocklistService(
            ILogger<RedisJwtBlocklistService> logger,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            SmartAlarmActivitySource activitySource,
            string connectionString)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _meter = meter ?? throw new ArgumentNullException(nameof(meter));
            _correlationContext = correlationContext ?? throw new ArgumentNullException(nameof(correlationContext));
            _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));

            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentException("Redis connection string cannot be null or empty", nameof(connectionString));

            try
            {
                _redis = ConnectionMultiplexer.Connect(connectionString);
                _database = _redis.GetDatabase();
                
                _logger.LogInformation("Connected to Redis for JWT Blocklist service at {ConnectionString}", 
                    connectionString.Split(',')[0]); // Log apenas o primeiro endpoint por segurança
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to Redis for JWT Blocklist at {ConnectionString}", connectionString);
                throw;
            }
        }

        public async Task<bool> IsTokenBlockedAsync(string tokenId)
        {
            if (string.IsNullOrWhiteSpace(tokenId))
            {
                _logger.LogWarning("IsTokenBlockedAsync called with empty or null tokenId");
                return false;
            }

            using var activity = _activitySource.StartActivity("JwtBlocklist.IsTokenBlocked");
            activity?.SetTag("jwt.token_id", tokenId);
            activity?.SetTag("jwt.operation", "check_blocked");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var key = BlocklistKeyPrefix + tokenId;
                var exists = await _database.KeyExistsAsync(key);
                
                stopwatch.Stop();
                _meter.RecordExternalServiceCallDuration(stopwatch.ElapsedMilliseconds, "Redis", "KeyExists", true);

                if (exists)
                {
                    _logger.LogWarning("Token {TokenId} found in blocklist", tokenId);
                }

                activity?.SetStatus(ActivityStatusCode.Ok, exists ? "Token is blocked" : "Token is not blocked");
                return exists;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("JWT_BLOCKLIST", "Redis", "CheckBlockedError");

                _logger.LogError(ex, "Failed to check if token {TokenId} is blocked", tokenId);
                
                // Em caso de falha na verificação, consideramos o token como bloqueado por segurança
                return true;
            }
        }

        public async Task<bool> BlockTokenAsync(string tokenId, TimeSpan expiration, string? reason = null)
        {
            if (string.IsNullOrWhiteSpace(tokenId))
            {
                _logger.LogWarning("BlockTokenAsync called with empty or null tokenId");
                return false;
            }

            using var activity = _activitySource.StartActivity("JwtBlocklist.BlockToken");
            activity?.SetTag("jwt.token_id", tokenId);
            activity?.SetTag("jwt.operation", "block");
            activity?.SetTag("jwt.reason", reason ?? "not_specified");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var key = BlocklistKeyPrefix + tokenId;
                var blockInfo = new
                {
                    BlockedAt = DateTimeOffset.UtcNow,
                    Reason = reason ?? "Manual revocation",
                    ExpiresAt = DateTimeOffset.UtcNow.Add(expiration)
                };

                var value = JsonSerializer.Serialize(blockInfo);
                var result = await _database.StringSetAsync(key, value, expiration);
                
                stopwatch.Stop();
                _meter.RecordExternalServiceCallDuration(stopwatch.ElapsedMilliseconds, "Redis", "StringSet", result);

                if (result)
                {
                    _logger.LogInformation("Token {TokenId} added to blocklist with reason: {Reason}, expires in: {Expiration}", 
                        tokenId, reason ?? "not_specified", expiration);
                    
                    // Atualizar estatísticas
                    await UpdateStatisticsAsync("blocked", reason);
                }

                activity?.SetStatus(ActivityStatusCode.Ok, result ? "Token blocked successfully" : "Failed to block token");
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("JWT_BLOCKLIST", "Redis", "BlockTokenError");

                _logger.LogError(ex, "Failed to block token {TokenId}", tokenId);
                return false;
            }
        }

        public async Task<bool> BlockTokenAsync(string tokenId, DateTime expiry, string? reason = null)
        {
            var expiration = expiry > DateTime.UtcNow ? expiry - DateTime.UtcNow : TimeSpan.FromMinutes(1);
            return await BlockTokenAsync(tokenId, expiration, reason);
        }

        public async Task<bool> UnblockTokenAsync(string tokenId)
        {
            if (string.IsNullOrWhiteSpace(tokenId))
            {
                _logger.LogWarning("UnblockTokenAsync called with empty or null tokenId");
                return false;
            }

            using var activity = _activitySource.StartActivity("JwtBlocklist.UnblockToken");
            activity?.SetTag("jwt.token_id", tokenId);
            activity?.SetTag("jwt.operation", "unblock");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var key = BlocklistKeyPrefix + tokenId;
                var result = await _database.KeyDeleteAsync(key);
                
                stopwatch.Stop();
                _meter.RecordExternalServiceCallDuration(stopwatch.ElapsedMilliseconds, "Redis", "KeyDelete", result);

                if (result)
                {
                    _logger.LogInformation("Token {TokenId} removed from blocklist", tokenId);
                    
                    // Atualizar estatísticas
                    await UpdateStatisticsAsync("unblocked", null);
                }

                activity?.SetStatus(ActivityStatusCode.Ok, result ? "Token unblocked successfully" : "Token was not in blocklist");
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("JWT_BLOCKLIST", "Redis", "UnblockTokenError");

                _logger.LogError(ex, "Failed to unblock token {TokenId}", tokenId);
                return false;
            }
        }

        public async Task<int> BlockAllUserTokensAsync(Guid userId, string reason)
        {
            return await BlockAllUserTokensAsync(userId, DateTime.UtcNow.AddDays(30), reason);
        }

        public async Task<int> BlockAllUserTokensAsync(string userId, DateTime expiry, string reason)
        {
            if (Guid.TryParse(userId, out var userGuid))
            {
                return await BlockAllUserTokensAsync(userGuid, expiry, reason);
            }
            
            _logger.LogWarning("Invalid user ID format for blocking tokens: {UserId}", userId);
            return 0;
        }

        public async Task<int> BlockAllUserTokensAsync(Guid userId, DateTime expiry, string reason)
        {
            using var activity = _activitySource.StartActivity("JwtBlocklist.BlockAllUserTokens");
            activity?.SetTag("jwt.user_id", userId.ToString());
            activity?.SetTag("jwt.operation", "block_user_tokens");
            activity?.SetTag("jwt.reason", reason);

            var stopwatch = Stopwatch.StartNew();
            var blockedCount = 0;

            try
            {
                // Em uma implementação completa, seria necessário manter um índice de tokens por usuário
                // Por simplicidade, vamos implementar um mecanismo de bloqueio por usuário
                var userBlockKey = $"jwt:user_blocked:{userId}";
                var blockInfo = new
                {
                    BlockedAt = DateTimeOffset.UtcNow,
                    ExpiryDate = expiry,
                    Reason = reason,
                    AllTokens = true
                };

                var value = JsonSerializer.Serialize(blockInfo);
                // Usar a data de expiração fornecida
                var expiration = expiry > DateTime.UtcNow ? expiry - DateTime.UtcNow : TimeSpan.FromHours(24);
                var result = await _database.StringSetAsync(userBlockKey, value, expiration);
                
                stopwatch.Stop();
                _meter.RecordExternalServiceCallDuration(stopwatch.ElapsedMilliseconds, "Redis", "BlockUserTokens", result);

                if (result)
                {
                    blockedCount = 1; // Representa o bloqueio do usuário
                    _logger.LogInformation("All tokens for user {UserId} blocked with reason: {Reason}", userId, reason);
                    
                    // Atualizar estatísticas
                    await UpdateStatisticsAsync("user_blocked", reason);
                }

                activity?.SetStatus(ActivityStatusCode.Ok, $"Blocked tokens for user: {blockedCount}");
                return blockedCount;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("JWT_BLOCKLIST", "Redis", "BlockUserTokensError");

                _logger.LogError(ex, "Failed to block all tokens for user {UserId}", userId);
                return 0;
            }
        }

        public async Task<int> CleanupExpiredTokensAsync()
        {
            using var activity = _activitySource.StartActivity("JwtBlocklist.CleanupExpiredTokens");
            activity?.SetTag("jwt.operation", "cleanup");

            var stopwatch = Stopwatch.StartNew();
            var cleanedCount = 0;

            try
            {
                // Implementar lock distribuído para evitar múltiplas limpezas simultâneas
                var lockResult = await _database.StringSetAsync(CleanupLockKey, "locked", TimeSpan.FromMinutes(5), When.NotExists);
                
                if (!lockResult)
                {
                    _logger.LogInformation("Cleanup already in progress, skipping");
                    return 0;
                }

                try
                {
                    // Redis automaticamente remove chaves expiradas, mas podemos implementar uma limpeza manual se necessário
                    // Por enquanto, apenas atualizamos as estatísticas
                    _logger.LogInformation("JWT Blocklist cleanup completed, cleaned {CleanedCount} expired tokens", cleanedCount);
                    
                    // Atualizar estatísticas
                    await UpdateStatisticsAsync("cleanup", null);
                    
                    stopwatch.Stop();
                    _meter.RecordExternalServiceCallDuration(stopwatch.ElapsedMilliseconds, "Redis", "Cleanup", true);
                    
                    activity?.SetStatus(ActivityStatusCode.Ok, $"Cleaned {cleanedCount} expired tokens");
                    return cleanedCount;
                }
                finally
                {
                    // Liberar lock
                    await _database.KeyDeleteAsync(CleanupLockKey);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("JWT_BLOCKLIST", "Redis", "CleanupError");

                _logger.LogError(ex, "Failed to cleanup expired tokens");
                return 0;
            }
        }

        public async Task<BlocklistStatistics> GetStatisticsAsync()
        {
            using var activity = _activitySource.StartActivity("JwtBlocklist.GetStatistics");
            activity?.SetTag("jwt.operation", "get_statistics");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var statsValue = await _database.StringGetAsync(StatisticsKey);
                
                stopwatch.Stop();
                _meter.RecordExternalServiceCallDuration(stopwatch.ElapsedMilliseconds, "Redis", "GetStatistics", true);

                if (statsValue.HasValue)
                {
                    var stats = JsonSerializer.Deserialize<BlocklistStatistics>(statsValue!);
                    activity?.SetStatus(ActivityStatusCode.Ok, "Statistics retrieved successfully");
                    return stats ?? new BlocklistStatistics();
                }

                activity?.SetStatus(ActivityStatusCode.Ok, "No statistics found, returning default");
                return new BlocklistStatistics
                {
                    LastCleanup = DateTime.UtcNow,
                    BlockReasons = new Dictionary<string, int>()
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("JWT_BLOCKLIST", "Redis", "GetStatisticsError");

                _logger.LogError(ex, "Failed to get blocklist statistics");
                return new BlocklistStatistics();
            }
        }

        private async Task UpdateStatisticsAsync(string operation, string? reason)
        {
            try
            {
                var stats = await GetStatisticsAsync();
                
                switch (operation)
                {
                    case "blocked":
                        stats.TotalBlockedTokens++;
                        stats.ActiveBlockedTokens++;
                        if (!string.IsNullOrEmpty(reason))
                        {
                            stats.BlockReasons[reason] = stats.BlockReasons.GetValueOrDefault(reason, 0) + 1;
                        }
                        break;
                    case "unblocked":
                        stats.ActiveBlockedTokens = Math.Max(0, stats.ActiveBlockedTokens - 1);
                        break;
                    case "cleanup":
                        stats.LastCleanup = DateTime.UtcNow;
                        break;
                }

                var updatedStats = JsonSerializer.Serialize(stats);
                await _database.StringSetAsync(StatisticsKey, updatedStats, TimeSpan.FromDays(30));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update blocklist statistics for operation: {Operation}", operation);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _redis?.Dispose();
                _logger.LogInformation("Redis JWT Blocklist Service disposed");
                _disposed = true;
            }
        }
    }
}
