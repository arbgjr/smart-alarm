using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using SmartAlarm.KeyVault.Abstractions;
using SmartAlarm.KeyVault.Configuration;

namespace SmartAlarm.KeyVault.Services
{
    /// <summary>
    /// Service that manages multiple secret providers and provides unified access to secrets.
    /// </summary>
    public class KeyVaultService : IKeyVaultService
    {
        private readonly IEnumerable<ISecretProvider> _providers;
        private readonly KeyVaultOptions _options;
        private readonly ILogger<KeyVaultService> _logger;
        private readonly ConcurrentDictionary<string, (string Value, DateTime ExpiryTime)> _cache = new();
        private readonly SemaphoreSlim _cacheSemaphore = new(1, 1);
        private readonly ResiliencePipeline _resiliencePipeline;

        public KeyVaultService(
            IEnumerable<ISecretProvider> providers,
            IOptions<KeyVaultOptions> options,
            ILogger<KeyVaultService> logger)
        {
            _providers = providers;
            _options = options.Value;
            _logger = logger;
            _resiliencePipeline = CreateResiliencePipeline();
        }

        public async Task<string?> GetSecretAsync(string secretKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                _logger.LogWarning("Secret key cannot be null or empty");
                return null;
            }

            if (!_options.Enabled)
            {
                _logger.LogDebug("KeyVault service is disabled");
                return null;
            }

            // Check cache first
            if (_options.EnableCaching && TryGetFromCache(secretKey, out var cachedValue))
            {
                _logger.LogDebug("Retrieved secret '{SecretKey}' from cache", secretKey);
                return cachedValue;
            }

            // Try providers in priority order
            var orderedProviders = GetOrderedProviders();
            
            foreach (var provider in orderedProviders)
            {
                try
                {
                    if (_options.EnableDetailedLogging)
                    {
                        _logger.LogDebug("Trying provider '{ProviderName}' for secret '{SecretKey}'", provider.ProviderName, secretKey);
                    }

                    var isAvailable = await _resiliencePipeline.ExecuteAsync(async _ => 
                        await provider.IsAvailableAsync(cancellationToken), cancellationToken);

                    if (!isAvailable)
                    {
                        if (_options.EnableDetailedLogging)
                        {
                            _logger.LogDebug("Provider '{ProviderName}' is not available", provider.ProviderName);
                        }
                        continue;
                    }

                    var secretValue = await _resiliencePipeline.ExecuteAsync(async _ => 
                        await provider.GetSecretAsync(secretKey, cancellationToken), cancellationToken);

                    if (secretValue != null)
                    {
                        _logger.LogDebug("Successfully retrieved secret '{SecretKey}' from provider '{ProviderName}'", 
                            secretKey, provider.ProviderName);

                        // Cache the result
                        if (_options.EnableCaching)
                        {
                            await CacheSecretAsync(secretKey, secretValue);
                        }

                        return secretValue;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Provider '{ProviderName}' failed to retrieve secret '{SecretKey}'", 
                        provider.ProviderName, secretKey);
                }
            }

            _logger.LogWarning("Failed to retrieve secret '{SecretKey}' from any available provider", secretKey);
            return null;
        }

        public async Task<Dictionary<string, string?>> GetSecretsAsync(IEnumerable<string> secretKeys, CancellationToken cancellationToken = default)
        {
            var results = new Dictionary<string, string?>();
            
            foreach (var secretKey in secretKeys)
            {
                results[secretKey] = await GetSecretAsync(secretKey, cancellationToken);
            }
            
            return results;
        }

        public async Task<bool> SetSecretAsync(string secretKey, string secretValue, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(secretKey) || secretValue == null)
            {
                _logger.LogWarning("Secret key and value cannot be null or empty");
                return false;
            }

            if (!_options.Enabled)
            {
                _logger.LogDebug("KeyVault service is disabled");
                return false;
            }

            // Try providers in priority order
            var orderedProviders = GetOrderedProviders();
            
            foreach (var provider in orderedProviders)
            {
                try
                {
                    if (_options.EnableDetailedLogging)
                    {
                        _logger.LogDebug("Trying to set secret '{SecretKey}' using provider '{ProviderName}'", secretKey, provider.ProviderName);
                    }

                    var isAvailable = await _resiliencePipeline.ExecuteAsync(async _ => 
                        await provider.IsAvailableAsync(cancellationToken), cancellationToken);

                    if (!isAvailable)
                    {
                        if (_options.EnableDetailedLogging)
                        {
                            _logger.LogDebug("Provider '{ProviderName}' is not available", provider.ProviderName);
                        }
                        continue;
                    }

                    var success = await _resiliencePipeline.ExecuteAsync(async _ => 
                        await provider.SetSecretAsync(secretKey, secretValue, cancellationToken), cancellationToken);

                    if (success)
                    {
                        _logger.LogDebug("Successfully set secret '{SecretKey}' using provider '{ProviderName}'", 
                            secretKey, provider.ProviderName);

                        // Update cache
                        if (_options.EnableCaching)
                        {
                            await CacheSecretAsync(secretKey, secretValue);
                        }

                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Provider '{ProviderName}' failed to set secret '{SecretKey}'", 
                        provider.ProviderName, secretKey);
                }
            }

            _logger.LogWarning("Failed to set secret '{SecretKey}' using any available provider", secretKey);
            return false;
        }

        public async Task<IEnumerable<string>> GetAvailableProvidersAsync(CancellationToken cancellationToken = default)
        {
            var availableProviders = new List<string>();
            
            foreach (var provider in GetOrderedProviders())
            {
                try
                {
                    var isAvailable = await provider.IsAvailableAsync(cancellationToken);
                    if (isAvailable)
                    {
                        availableProviders.Add(provider.ProviderName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error checking availability of provider '{ProviderName}'", provider.ProviderName);
                }
            }
            
            return availableProviders;
        }

        private IEnumerable<ISecretProvider> GetOrderedProviders()
        {
            return _providers
                .Where(p => !_options.DisabledProviders.Contains(p.ProviderName, StringComparer.OrdinalIgnoreCase))
                .OrderBy(p => _options.ProviderPriorities.GetValueOrDefault(p.ProviderName, p.Priority))
                .ThenBy(p => p.Priority)
                .ThenBy(p => p.ProviderName);
        }

        private bool TryGetFromCache(string secretKey, out string? value)
        {
            value = null;
            
            if (!_cache.TryGetValue(secretKey, out var cachedEntry))
            {
                return false;
            }

            if (DateTime.UtcNow > cachedEntry.ExpiryTime)
            {
                _cache.TryRemove(secretKey, out _);
                return false;
            }

            value = cachedEntry.Value;
            return true;
        }

        private async Task CacheSecretAsync(string secretKey, string secretValue)
        {
            await _cacheSemaphore.WaitAsync();
            try
            {
                var expiryTime = DateTime.UtcNow.AddMinutes(_options.CacheExpirationMinutes);
                _cache.AddOrUpdate(secretKey, 
                    (secretValue, expiryTime), 
                    (_, _) => (secretValue, expiryTime));
            }
            finally
            {
                _cacheSemaphore.Release();
            }
        }

        private ResiliencePipeline CreateResiliencePipeline()
        {
            var retryOptions = new Polly.Retry.RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                MaxRetryAttempts = _options.RetryAttempts,
                BackoffType = _options.UseExponentialBackoff ? DelayBackoffType.Exponential : DelayBackoffType.Constant,
                Delay = TimeSpan.FromMilliseconds(_options.RetryDelayMs)
            };

            return new ResiliencePipelineBuilder()
                .AddRetry(retryOptions)
                .Build();
        }
    }
}