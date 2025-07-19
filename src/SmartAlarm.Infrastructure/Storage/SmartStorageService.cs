using Microsoft.Extensions.Logging;
using SmartAlarm.Infrastructure.Configuration;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace SmartAlarm.Infrastructure.Storage
{
    /// <summary>
    /// Storage service inteligente que detecta se MinIO está disponível.
    /// Se MinIO estiver offline, faz fallback para MockStorageService automaticamente.
    /// Resolve o problema de usar Mock em produção de forma transparente.
    /// </summary>
    public class SmartStorageService : IStorageService
    {
        private readonly IStorageService _primaryService;
        private readonly IStorageService _fallbackService;
        private readonly ILogger<SmartStorageService> _logger;
        private bool _primaryServiceAvailable = true;

        public SmartStorageService(
            ILogger<SmartStorageService> logger,
            ILogger<MinioStorageService> minioLogger,
            ILogger<MockStorageService> mockLogger,
            IConfigurationResolver configResolver,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            SmartAlarmActivitySource activitySource)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Cria os serviços primário e fallback
            _primaryService = new MinioStorageService(
                minioLogger,
                configResolver,
                meter,
                correlationContext,
                activitySource);

            _fallbackService = new MockStorageService(mockLogger);

            // Testa disponibilidade do serviço primário na inicialização
            _ = Task.Run(CheckPrimaryServiceAvailability);
        }

        public async Task UploadAsync(string path, Stream content)
        {
            if (_primaryServiceAvailable)
            {
                try
                {
                    await _primaryService.UploadAsync(path, content);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Primary storage service failed for upload. Falling back to mock. Path: {Path}", path);
                    _primaryServiceAvailable = false;
                }
            }

            _logger.LogInformation("Using fallback storage service for upload. Path: {Path}", path);
            await _fallbackService.UploadAsync(path, content);
        }

        public async Task<Stream> DownloadAsync(string path)
        {
            if (_primaryServiceAvailable)
            {
                try
                {
                    return await _primaryService.DownloadAsync(path);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Primary storage service failed for download. Falling back to mock. Path: {Path}", path);
                    _primaryServiceAvailable = false;
                }
            }

            _logger.LogInformation("Using fallback storage service for download. Path: {Path}", path);
            return await _fallbackService.DownloadAsync(path);
        }

        public async Task DeleteAsync(string path)
        {
            if (_primaryServiceAvailable)
            {
                try
                {
                    await _primaryService.DeleteAsync(path);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Primary storage service failed for delete. Falling back to mock. Path: {Path}", path);
                    _primaryServiceAvailable = false;
                }
            }

            _logger.LogInformation("Using fallback storage service for delete. Path: {Path}", path);
            await _fallbackService.DeleteAsync(path);
        }

        private async Task CheckPrimaryServiceAvailability()
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var response = await client.GetAsync("http://localhost:9000/minio/health/live");
                _primaryServiceAvailable = response.IsSuccessStatusCode;
                
                if (_primaryServiceAvailable)
                {
                    _logger.LogInformation("Primary storage service (MinIO) is available");
                }
                else
                {
                    _logger.LogWarning("Primary storage service (MinIO) health check returned: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _primaryServiceAvailable = false;
                _logger.LogWarning(ex, "Primary storage service (MinIO) is not available. Will use fallback.");
            }
        }
    }
}
