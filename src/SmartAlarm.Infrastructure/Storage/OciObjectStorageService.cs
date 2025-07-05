using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SmartAlarm.Infrastructure.Storage
{
    /// <summary>
    /// Stub para integração futura com OCI Object Storage (produção).
    /// </summary>
    public class OciObjectStorageService : IStorageService
    {
        private readonly ILogger<OciObjectStorageService> _logger;
        public OciObjectStorageService(ILogger<OciObjectStorageService> logger)
        {
            _logger = logger;
        }
        public Task UploadAsync(string path, Stream content)
        {
            _logger.LogInformation("[OCI Object Storage] Upload para {Path}", path);
            // TODO: Implementar integração real com OCI SDK
            return Task.CompletedTask;
        }
        public Task<Stream> DownloadAsync(string path)
        {
            _logger.LogInformation("[OCI Object Storage] Download de {Path}", path);
            // TODO: Implementar integração real com OCI SDK
            return Task.FromResult<Stream>(new MemoryStream());
        }
        public Task DeleteAsync(string path)
        {
            _logger.LogInformation("[OCI Object Storage] Delete de {Path}", path);
            // TODO: Implementar integração real com OCI SDK
            return Task.CompletedTask;
        }
    }
}
