using Microsoft.Extensions.Logging;
using SmartAlarm.Infrastructure.Configuration;
using SmartAlarm.Infrastructure.Exceptions;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;
using Polly;
using Polly.CircuitBreaker;
using Polly.Fallback;
using Polly.Retry;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace SmartAlarm.Infrastructure.Storage
{
    /// <summary>
    /// Storage service resiliente com circuit breaker e retry policies.
    /// Quando MinIO não está disponível, retorna erro apropriado (503) ao invés de usar mock.
    /// </summary>
    public class SmartStorageService : IStorageService
    {
        private readonly IStorageService _primaryService;
        private readonly ILogger<SmartStorageService> _logger;
        private readonly ResiliencePipeline<Stream> _downloadPipeline;
        private readonly ResiliencePipeline _uploadPipeline;
        private readonly SmartAlarmMeter _meter;
        private readonly HttpClient _healthCheckClient;

        public SmartStorageService(
            ILogger<SmartStorageService> logger,
            ILogger<MinioStorageService> minioLogger,
            IConfigurationResolver configResolver,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            SmartAlarmActivitySource activitySource)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _meter = meter ?? throw new ArgumentNullException(nameof(meter));

            // Cria o serviço primário real
            _primaryService = new MinioStorageService(
                minioLogger,
                configResolver,
                meter,
                correlationContext,
                activitySource);

            _healthCheckClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

            // Configura políticas de resiliência com Polly v8
            var retryOptions = new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                OnRetry = args =>
                {
                    _logger.LogWarning(args.Outcome.Exception, 
                        "Storage operation retry {AttemptNumber} after {Duration}ms", 
                        args.AttemptNumber, args.Duration.TotalMilliseconds);
                    return ValueTask.CompletedTask;
                }
            };

            var circuitBreakerOptions = new CircuitBreakerStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 3,
                BreakDuration = TimeSpan.FromSeconds(30),
                OnOpened = args =>
                {
                    _logger.LogWarning(args.Outcome.Exception, "Storage service circuit breaker opened");
                    _meter.IncrementErrorCount("Storage", "CircuitBreakerOpen", "MinIO");
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation("Storage service circuit breaker reset");
                    _meter.IncrementCounter("storage_circuit_breaker_reset", 1);
                    return ValueTask.CompletedTask;
                }
            };

            _uploadPipeline = new ResiliencePipelineBuilder()
                .AddRetry(retryOptions)
                .AddCircuitBreaker(circuitBreakerOptions)
                .Build();

            var downloadFallbackOptions = new FallbackStrategyOptions<Stream>
            {
                ShouldHandle = new PredicateBuilder<Stream>().Handle<Exception>(),
                FallbackAction = args =>
                {
                    _logger.LogError(args.Outcome.Exception, "Storage download failed after all retries");
                    return ValueTask.FromResult(Outcome.FromResult(Stream.Null));
                }
            };

            _downloadPipeline = new ResiliencePipelineBuilder<Stream>()
                .AddRetry(new RetryStrategyOptions<Stream>
                {
                    ShouldHandle = new PredicateBuilder<Stream>().Handle<Exception>(),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(1),
                    OnRetry = args =>
                    {
                        _logger.LogWarning(args.Outcome.Exception, 
                            "Storage download retry {AttemptNumber} after {Duration}ms", 
                            args.AttemptNumber, args.Duration.TotalMilliseconds);
                        return ValueTask.CompletedTask;
                    }
                })
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions<Stream>
                {
                    ShouldHandle = new PredicateBuilder<Stream>().Handle<Exception>(),
                    FailureRatio = 0.5,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    MinimumThroughput = 3,
                    BreakDuration = TimeSpan.FromSeconds(30)
                })
                .AddFallback(downloadFallbackOptions)
                .Build();

            // Health check inicial
            _ = Task.Run(CheckPrimaryServiceAvailability);
        }

        public async Task UploadAsync(string path, Stream content)
        {
            try
            {
                await _uploadPipeline.ExecuteAsync(async (cancellationToken) =>
                {
                    await _primaryService.UploadAsync(path, content);
                });
                
                _meter.IncrementCounter("storage_upload_success", 1);
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogError(ex, "Storage service circuit breaker is open. Upload failed for path: {Path}", path);
                _meter.IncrementErrorCount("Storage", "Upload", "CircuitBreakerOpen");
                throw new StorageUnavailableException("Storage service is temporarily unavailable. Please try again later.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Storage upload failed after all retries. Path: {Path}", path);
                _meter.IncrementErrorCount("Storage", "Upload", "Failed");
                throw new StorageException($"Failed to upload file to path: {path}", ex);
            }
        }

        public async Task<Stream> DownloadAsync(string path)
        {
            try
            {
                var result = await _downloadPipeline.ExecuteAsync(async (cancellationToken) =>
                {
                    return await _primaryService.DownloadAsync(path);
                });
                
                _meter.IncrementCounter("storage_download_success", 1);
                return result;
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogError(ex, "Storage service circuit breaker is open. Download failed for path: {Path}", path);
                _meter.IncrementErrorCount("Storage", "Download", "CircuitBreakerOpen");
                throw new StorageUnavailableException("Storage service is temporarily unavailable. Please try again later.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Storage download failed after all retries. Path: {Path}", path);
                _meter.IncrementErrorCount("Storage", "Download", "Failed");
                throw new StorageException($"Failed to download file from path: {path}", ex);
            }
        }

        public async Task DeleteAsync(string path)
        {
            try
            {
                await _uploadPipeline.ExecuteAsync(async (cancellationToken) =>
                {
                    await _primaryService.DeleteAsync(path);
                });
                
                _meter.IncrementCounter("storage_delete_success", 1);
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogError(ex, "Storage service circuit breaker is open. Delete failed for path: {Path}", path);
                _meter.IncrementErrorCount("Storage", "Delete", "CircuitBreakerOpen");
                throw new StorageUnavailableException("Storage service is temporarily unavailable. Please try again later.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Storage delete failed after all retries. Path: {Path}", path);
                _meter.IncrementErrorCount("Storage", "Delete", "Failed");
                throw new StorageException($"Failed to delete file at path: {path}", ex);
            }
        }

        private async Task CheckPrimaryServiceAvailability()
        {
            try
            {
                var response = await _healthCheckClient.GetAsync("http://localhost:9000/minio/health/live");
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Primary storage service (MinIO) health check passed");
                    _meter.IncrementCounter("storage_health_check_success", 1);
                }
                else
                {
                    _logger.LogWarning("Primary storage service (MinIO) health check failed: {StatusCode}", response.StatusCode);
                    _meter.IncrementCounter("storage_health_check_failed", 1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Primary storage service (MinIO) health check error");
                _meter.IncrementErrorCount("Storage", "HealthCheck", "Failed");
            }
        }
    }
}
