using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Services;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Infrastructure.Services
{
    /// <summary>
    /// Implementação do serviço de cache para feriados usando Redis
    /// </summary>
    public class HolidayCacheService : IHolidayCacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<HolidayCacheService> _logger;
        private const string CacheKeyPrefix = "holidays";
        private static readonly TimeSpan DefaultExpiration = TimeSpan.FromDays(30);

        public HolidayCacheService(
            IDistributedCache cache,
            ILogger<HolidayCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<Holiday>?> GetHolidaysAsync(
            string country, 
            int year, 
            string? state = null, 
            CancellationToken cancellationToken = default)
        {
            var cacheKey = BuildCacheKey(country, year, state);

            try
            {
                var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
                
                if (string.IsNullOrEmpty(cachedData))
                {
                    _logger.LogDebug("Cache miss para feriados: {CacheKey}", cacheKey);
                    return null;
                }

                var holidays = JsonSerializer.Deserialize<List<HolidayCache>>(cachedData);
                
                if (holidays == null)
                {
                    _logger.LogWarning("Falha ao deserializar feriados do cache: {CacheKey}", cacheKey);
                    return null;
                }

                _logger.LogDebug("Cache hit para feriados: {CacheKey} - {Count} feriados encontrados", 
                    cacheKey, holidays.Count);

                // Converter de volta para entidades Holiday
                return holidays.Select(h => new Holiday(
                    h.Id,
                    h.Date,
                    h.Name,
                    h.Description,
                    h.Type,
                    h.Country,
                    h.State
                )).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar feriados do cache: {CacheKey}", cacheKey);
                return null;
            }
        }

        public async Task SetHolidaysAsync(
            List<Holiday> holidays, 
            string country, 
            int year, 
            string? state = null, 
            TimeSpan? expiration = null, 
            CancellationToken cancellationToken = default)
        {
            var cacheKey = BuildCacheKey(country, year, state);

            try
            {
                // Converter para DTOs serializáveis
                var cacheData = holidays.Select(h => new HolidayCache
                {
                    Id = h.Id,
                    Date = h.Date,
                    Name = h.Name,
                    Description = h.Description,
                    Type = h.Type,
                    Country = h.Country,
                    State = h.State
                }).ToList();

                var json = JsonSerializer.Serialize(cacheData);

                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration ?? DefaultExpiration
                };

                await _cache.SetStringAsync(cacheKey, json, options, cancellationToken);

                _logger.LogInformation("Feriados armazenados em cache: {CacheKey} - {Count} feriados, expira em {Expiration}", 
                    cacheKey, holidays.Count, options.AbsoluteExpirationRelativeToNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao armazenar feriados no cache: {CacheKey}", cacheKey);
                // Não lançar exceção para não quebrar o fluxo principal
            }
        }

        public async Task InvalidateAsync(
            string country, 
            int year, 
            string? state = null, 
            CancellationToken cancellationToken = default)
        {
            var cacheKey = BuildCacheKey(country, year, state);

            try
            {
                await _cache.RemoveAsync(cacheKey, cancellationToken);
                _logger.LogInformation("Cache de feriados invalidado: {CacheKey}", cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao invalidar cache de feriados: {CacheKey}", cacheKey);
            }
        }

        public async Task<bool> IsHolidayAsync(
            DateTime date, 
            string country, 
            string? state = null, 
            CancellationToken cancellationToken = default)
        {
            var holidays = await GetHolidaysAsync(country, date.Year, state, cancellationToken);
            
            if (holidays == null)
            {
                _logger.LogDebug("Não foi possível verificar feriado via cache para {Date} em {Country}{State}", 
                    date, country, state != null ? $"-{state}" : "");
                return false;
            }

            return holidays.Any(h => h.Date.Date == date.Date);
        }

        private static string BuildCacheKey(string country, int year, string? state = null)
        {
            var key = $"{CacheKeyPrefix}:{country.ToUpper()}:{year}";
            
            if (!string.IsNullOrEmpty(state))
            {
                key += $":{state.ToUpper()}";
            }

            return key;
        }

        /// <summary>
        /// Classe interna para serialização de feriados no cache
        /// </summary>
        private class HolidayCache
        {
            public Guid Id { get; set; }
            public DateTime Date { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public HolidayType Type { get; set; }
            public string Country { get; set; } = string.Empty;
            public string? State { get; set; }
        }
    }
}