using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Minio;

namespace SmartAlarm.Infrastructure.Storage
{
    /// <summary>
    /// Implementação real de IStorageService usando MinIO.
    /// </summary>
    public class MinioStorageService : IStorageService
    {
        private readonly IMinioClient _minioClient;
        private readonly ILogger<MinioStorageService> _logger;
        private const string BucketName = "smartalarm";

        public MinioStorageService(ILogger<MinioStorageService> logger)
        {
            // Permite sobrescrever o endpoint via variável de ambiente para rodar localmente
            var endpoint = Environment.GetEnvironmentVariable("MINIO_ENDPOINT") ?? "minio";
            var portStr = Environment.GetEnvironmentVariable("MINIO_PORT") ?? "9000";
            int port = int.TryParse(portStr, out var p) ? p : 9000;
            _minioClient = new MinioClient()
                .WithEndpoint(endpoint, port)
                .WithCredentials("minio", "minio123")
                .Build();
            _logger = logger;
        }

        public async Task UploadAsync(string path, Stream content)
        {
            await EnsureBucketExistsAsync();
            var putObjectArgs = new Minio.DataModel.Args.PutObjectArgs()
                .WithBucket(BucketName)
                .WithObject(path)
                .WithStreamData(content)
                .WithObjectSize(content.Length)
                .WithContentType("application/octet-stream");
            await _minioClient.PutObjectAsync(putObjectArgs);
            _logger.LogInformation("[MinIO] Upload para {Path}", path);
        }

        public async Task<Stream> DownloadAsync(string path)
        {
            await EnsureBucketExistsAsync();
            var ms = new MemoryStream();
            var getObjectArgs = new Minio.DataModel.Args.GetObjectArgs()
                .WithBucket(BucketName)
                .WithObject(path)
                .WithCallbackStream(stream => stream.CopyTo(ms));
            await _minioClient.GetObjectAsync(getObjectArgs);
            ms.Position = 0;
            _logger.LogInformation("[MinIO] Download de {Path}", path);
            return ms;
        }

        public async Task DeleteAsync(string path)
        {
            await EnsureBucketExistsAsync();
            var removeObjectArgs = new Minio.DataModel.Args.RemoveObjectArgs()
                .WithBucket(BucketName)
                .WithObject(path);
            await _minioClient.RemoveObjectAsync(removeObjectArgs);
            _logger.LogInformation("[MinIO] Delete de {Path}", path);
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
