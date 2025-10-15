using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Text.Json;
using SmartAlarm.Application.Abstractions;

namespace SmartAlarm.Api.Services;

/// <summary>
/// Implementação do serviço de rate limiting com Redis
/// </summary>
public class RateLimitingService : IRateLimitingService
{
    private readonly IDistributedCache _cache;
    private readonly RateLimitConfiguration _config;
    private readonly IAuditService _auditService;
    private readonly ILogger<RateLimitingService> _logger;

    public RateLimitingService(
        IDistributedCache cache,
        IOptions<RateLimitConfiguration> config,
        IAuditService auditService,
        ILogger<RateLimitingService> logger)
    {
        _cache = cache;
        _config = config.Value;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<bool> ShouldRateLimitAsync(string key, string endpoint, string requestType)
    {
        try
        {
            // Verificar se IP está bloqueado
            if (await IsIpBlockedAsync(key))
            {
                await LogSuspiciousActivityAsync(key, endpoint, "IP blocked due to excessive requests");
                return true;
            }

            // Verificar rate limits específicos do endpoint
            if (await CheckEndpointRateLimitAsync(key, endpoint, requestType))
            {
                return true;
            }

            // Verificar rate limits globais
            if (await CheckGlobalRateLimitAsync(key))
            {
                return true;
            }

            // Verificar rate limits de segurança
            if (await CheckSecurityRateLimitAsync(key, endpoint))
            {
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit for key {Key} and endpoint {Endpoint}", key, endpoint);
            // Em caso de erro, não bloquear (fail-open)
            return false;
        }
    }

    public async Task<RateLimitInfo> GetRateLimitInfoAsync(string key, string endpoint)
    {
        try
        {
            var cacheKey = $"ratelimit:{key}:{endpoint}:minute";
            var cachedData = await _cache.GetStringAsync(cacheKey);

            if (string.IsNullOrEmpty(cachedData))
            {
                return new RateLimitInfo
                {
                    RequestsRemaining = GetEndpointLimit(endpoint).RequestsPerMinute,
                    TotalRequests = GetEndpointLimit(endpoint).RequestsPerMinute,
                    ResetTime = TimeSpan.FromMinutes(1),
                    IsLimited = false
                };
            }

            var rateLimitData = JsonSerializer.Deserialize<RateLimitData>(cachedData);
            var endpointLimit = GetEndpointLimit(endpoint);

            return new RateLimitInfo
            {
                RequestsRemaining = Math.Max(0, endpointLimit.RequestsPerMinute - rateLimitData!.RequestCount),
                TotalRequests = endpointLimit.RequestsPerMinute,
                ResetTime = rateLimitData.ResetTime - DateTime.UtcNow,
                IsLimited = rateLimitData.RequestCount >= endpointLimit.RequestsPerMinute,
                RetryAfter = rateLimitData.RequestCount >= endpointLimit.RequestsPerMinute
                    ? ((int)(rateLimitData.ResetTime - DateTime.UtcNow).TotalSeconds).ToString()
                    : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rate limit info for key {Key} and endpoint {Endpoint}", key, endpoint);
            return new RateLimitInfo { IsLimited = false };
        }
    }

    public async Task LogSuspiciousActivityAsync(string key, string endpoint, string reason)
    {
        try
        {
            await _auditService.LogSecurityEventAsync(
                $"Suspicious activity detected: {reason}",
                null,
                JsonSerializer.Serialize(new
                {
                    Key = key,
                    Endpoint = endpoint,
                    Reason = reason,
                    Timestamp = DateTime.UtcNow
                }));

            _logger.LogWarning("Suspicious activity detected for key {Key} on endpoint {Endpoint}: {Reason}",
                key, endpoint, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging suspicious activity for key {Key}", key);
        }
    }

    private async Task<bool> IsIpBlockedAsync(string key)
    {
        var blockKey = $"blocked:{key}";
        var blockedUntil = await _cache.GetStringAsync(blockKey);

        if (string.IsNullOrEmpty(blockedUntil))
            return false;

        if (DateTime.TryParse(blockedUntil, out var blockTime))
        {
            if (DateTime.UtcNow < blockTime)
                return true;

            // Remover bloqueio expirado
            await _cache.RemoveAsync(blockKey);
        }

        return false;
    }

    private async Task<bool> CheckEndpointRateLimitAsync(string key, string endpoint, string requestType)
    {
        var endpointLimit = GetEndpointLimit(endpoint);

        // Verificar limite por minuto
        if (await CheckRateLimitAsync($"ratelimit:{key}:{endpoint}:minute",
            endpointLimit.RequestsPerMinute, TimeSpan.FromMinutes(1)))
        {
            return true;
        }

        // Verificar limite por hora
        if (await CheckRateLimitAsync($"ratelimit:{key}:{endpoint}:hour",
            endpointLimit.RequestsPerHour, TimeSpan.FromHours(1)))
        {
            return true;
        }

        // Verificar limite por dia
        if (await CheckRateLimitAsync($"ratelimit:{key}:{endpoint}:day",
            endpointLimit.RequestsPerDay, TimeSpan.FromDays(1)))
        {
            return true;
        }

        // Verificar proteção contra burst
        if (endpointLimit.EnableBurstProtection)
        {
            if (await CheckRateLimitAsync($"ratelimit:{key}:{endpoint}:burst",
                endpointLimit.BurstLimit, TimeSpan.FromSeconds(10)))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> CheckGlobalRateLimitAsync(string key)
    {
        // Verificar limite global por minuto
        if (await CheckRateLimitAsync($"ratelimit:{key}:global:minute",
            _config.Global.RequestsPerMinute, TimeSpan.FromMinutes(1)))
        {
            return true;
        }

        // Verificar limite global por hora
        if (await CheckRateLimitAsync($"ratelimit:{key}:global:hour",
            _config.Global.RequestsPerHour, TimeSpan.FromHours(1)))
        {
            // Se exceder limite global por hora, bloquear IP temporariamente
            if (_config.Global.EnableIpBlocking)
            {
                await BlockIpAsync(key, _config.Global.BlockDuration);
            }
            return true;
        }

        return false;
    }

    private async Task<bool> CheckSecurityRateLimitAsync(string key, string endpoint)
    {
        var endpointLower = endpoint.ToLowerInvariant();

        // Rate limiting para login
        if (endpointLower.Contains("/auth/login") || endpointLower.Contains("/login"))
        {
            if (await CheckRateLimitAsync($"security:{key}:login:minute",
                _config.Security.LoginAttemptsPerMinute, TimeSpan.FromMinutes(1)))
            {
                return true;
            }

            if (await CheckRateLimitAsync($"security:{key}:login:hour",
                _config.Security.LoginAttemptsPerHour, TimeSpan.FromHours(1)))
            {
                await BlockIpAsync(key, _config.Security.LoginBlockDuration);
                return true;
            }
        }

        // Rate limiting para reset de senha
        if (endpointLower.Contains("/password/reset") || endpointLower.Contains("/forgot-password"))
        {
            if (await CheckRateLimitAsync($"security:{key}:password-reset:hour",
                _config.Security.PasswordResetAttemptsPerHour, TimeSpan.FromHours(1)))
            {
                return true;
            }
        }

        // Rate limiting para registro
        if (endpointLower.Contains("/register") || endpointLower.Contains("/signup"))
        {
            if (await CheckRateLimitAsync($"security:{key}:registration:hour",
                _config.Security.RegistrationAttemptsPerHour, TimeSpan.FromHours(1)))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> CheckRateLimitAsync(string cacheKey, int limit, TimeSpan window)
    {
        var cachedData = await _cache.GetStringAsync(cacheKey);
        var now = DateTime.UtcNow;

        RateLimitData rateLimitData;

        if (string.IsNullOrEmpty(cachedData))
        {
            rateLimitData = new RateLimitData
            {
                RequestCount = 1,
                ResetTime = now.Add(window)
            };
        }
        else
        {
            rateLimitData = JsonSerializer.Deserialize<RateLimitData>(cachedData)!;

            if (now >= rateLimitData.ResetTime)
            {
                // Reset do contador
                rateLimitData.RequestCount = 1;
                rateLimitData.ResetTime = now.Add(window);
            }
            else
            {
                rateLimitData.RequestCount++;
            }
        }

        // Salvar dados atualizados
        var serializedData = JsonSerializer.Serialize(rateLimitData);
        var expiry = rateLimitData.ResetTime - now;
        await _cache.SetStringAsync(cacheKey, serializedData, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry
        });

        return rateLimitData.RequestCount > limit;
    }

    private async Task BlockIpAsync(string key, TimeSpan duration)
    {
        var blockKey = $"blocked:{key}";
        var blockUntil = DateTime.UtcNow.Add(duration);

        await _cache.SetStringAsync(blockKey, blockUntil.ToString("O"), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = duration
        });

        await LogSuspiciousActivityAsync(key, "SYSTEM", $"IP blocked for {duration.TotalMinutes} minutes due to excessive requests");
    }

    private EndpointRateLimit GetEndpointLimit(string endpoint)
    {
        var endpointKey = endpoint.ToLowerInvariant();

        if (_config.Endpoints.TryGetValue(endpointKey, out var limit))
        {
            return limit;
        }

        // Limites padrão baseados no tipo de endpoint
        if (endpointKey.Contains("/auth/") || endpointKey.Contains("/login"))
        {
            return new EndpointRateLimit
            {
                RequestsPerMinute = 10,
                RequestsPerHour = 50,
                RequestsPerDay = 200,
                EnableBurstProtection = true,
                BurstLimit = 3
            };
        }

        if (endpointKey.Contains("/api/"))
        {
            return new EndpointRateLimit
            {
                RequestsPerMinute = 60,
                RequestsPerHour = 1000,
                RequestsPerDay = 10000,
                EnableBurstProtection = true,
                BurstLimit = 10
            };
        }

        // Limite padrão
        return new EndpointRateLimit
        {
            RequestsPerMinute = 30,
            RequestsPerHour = 500,
            RequestsPerDay = 5000,
            EnableBurstProtection = false,
            BurstLimit = 5
        };
    }

    private class RateLimitData
    {
        public int RequestCount { get; set; }
        public DateTime ResetTime { get; set; }
    }
}
