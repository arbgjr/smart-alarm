using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Minio;
using SmartAlarm.Infrastructure.Configuration;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Logging;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;

namespace SmartAlarm.Infrastructure.Storage
{
    /// <summary>
    /// Implementação real de IStorageService usando MinIO.
    /// </summary>
    public class MinioStorageService : IStorageService
    {
        private readonly IMinioClient _minioClient;
        private readonly ILogger<MinioStorageService> _logger;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly SmartAlarmActivitySource _activitySource;
        private const string BucketName = "smartalarm";

        private readonly IConfigurationResolver _configResolver;

        public MinioStorageService(
            ILogger<MinioStorageService> logger, 
            IConfigurationResolver configResolver,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            SmartAlarmActivitySource activitySource)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configResolver = configResolver ?? throw new ArgumentNullException(nameof(configResolver));
            _meter = meter ?? throw new ArgumentNullException(nameof(meter));
            _correlationContext = correlationContext ?? throw new ArgumentNullException(nameof(correlationContext));
            _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));

            // Busca configs obrigatoriamente via ConfigurationResolver
            var endpoint = _configResolver.GetConfigAsync("MINIO_ENDPOINT").GetAwaiter().GetResult();
            var portStr = _configResolver.GetConfigAsync("MINIO_PORT").GetAwaiter().GetResult();
            if (!int.TryParse(portStr, out var port))
            {
                _logger.LogError("MINIO_PORT inválido: {PortStr}", portStr);
                throw new InvalidOperationException($"MINIO_PORT inválido: {portStr}");
            }
            _minioClient = new MinioClient()
                .WithEndpoint(endpoint, port)
                .WithCredentials("minio", "minio123")
                .Build();
        }

        public async Task UploadAsync(string path, Stream content)
        {
            using var activity = _activitySource.StartActivity("MinIO.Upload");
            activity?.SetTag("storage.operation", "upload");
            activity?.SetTag("storage.provider", "minio");
            activity?.SetTag("storage.path", path);
            activity?.SetTag("storage.size", content.Length.ToString());

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "MinIOUpload",
                new { Path = path, Size = content.Length });

            try
            {
                await EnsureBucketExistsAsync();
                var putObjectArgs = new Minio.DataModel.Args.PutObjectArgs()
                    .WithBucket(BucketName)
                    .WithObject(path)
                    .WithStreamData(content)
                    .WithObjectSize(content.Length)
                    .WithContentType("application/octet-stream");
                await _minioClient.PutObjectAsync(putObjectArgs);
                stopwatch.Stop();

                _meter.RecordExternalServiceCallDuration(stopwatch.ElapsedMilliseconds, "MinIO", "Upload", true);

                _logger.LogInformation(LogTemplates.StorageOperationCompleted,
                    "Upload",
                    path,
                    BucketName,
                    stopwatch.ElapsedMilliseconds);

                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("STORAGE", "MinIO", "UploadError");

                _logger.LogError(LogTemplates.StorageOperationFailed,
                    "Upload",
                    path,
                    BucketName,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }

        public async Task<Stream> DownloadAsync(string path)
        {
            using var activity = _activitySource.StartActivity("MinIO.Download");
            activity?.SetTag("storage.operation", "download");
            activity?.SetTag("storage.provider", "minio");
            activity?.SetTag("storage.path", path);

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "MinIODownload",
                new { Path = path });

            try
            {
                await EnsureBucketExistsAsync();
                var ms = new MemoryStream();
                var getObjectArgs = new Minio.DataModel.Args.GetObjectArgs()
                    .WithBucket(BucketName)
                    .WithObject(path)
                    .WithCallbackStream(stream => stream.CopyTo(ms));
                await _minioClient.GetObjectAsync(getObjectArgs);
                ms.Position = 0;
                stopwatch.Stop();

                activity?.SetTag("storage.size", ms.Length.ToString());
                _meter.RecordExternalServiceCallDuration(stopwatch.ElapsedMilliseconds, "MinIO", "Download", true);

                _logger.LogInformation(LogTemplates.StorageOperationCompleted,
                    "Download",
                    path,
                    BucketName,
                    stopwatch.ElapsedMilliseconds);

                activity?.SetStatus(ActivityStatusCode.Ok);
                return ms;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("STORAGE", "MinIO", "DownloadError");

                _logger.LogError(LogTemplates.StorageOperationFailed,
                    "Download",
                    path,
                    BucketName,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }

        public async Task DeleteAsync(string path)
        {
            using var activity = _activitySource.StartActivity("MinIO.Delete");
            activity?.SetTag("storage.operation", "delete");
            activity?.SetTag("storage.provider", "minio");
            activity?.SetTag("storage.path", path);

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "MinIODelete",
                new { Path = path });

            try
            {
                await EnsureBucketExistsAsync();
                var removeObjectArgs = new Minio.DataModel.Args.RemoveObjectArgs()
                    .WithBucket(BucketName)
                    .WithObject(path);
                await _minioClient.RemoveObjectAsync(removeObjectArgs);
                stopwatch.Stop();

                _meter.RecordExternalServiceCallDuration(stopwatch.ElapsedMilliseconds, "MinIO", "Delete", true);

                _logger.LogInformation(LogTemplates.StorageOperationCompleted,
                    "Delete",
                    path,
                    BucketName,
                    stopwatch.ElapsedMilliseconds);

                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("STORAGE", "MinIO", "DeleteError");

                _logger.LogError(LogTemplates.StorageOperationFailed,
                    "Delete",
                    path,
                    BucketName,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }

        private async Task EnsureBucketExistsAsync()
        {
            var bucketExistsArgs = new Minio.DataModel.Args.BucketExistsArgs()
                .WithBucket(BucketName);
            var exists = await _minioClient.BucketExistsAsync(bucketExistsArgs);
            if (!exists)
            {
                var makeBucketArgs = new Minio.DataModel.Args.MakeBucketArgs()
                    .WithBucket(BucketName);
                await _minioClient.MakeBucketAsync(makeBucketArgs);
            }
        }
    }
}
