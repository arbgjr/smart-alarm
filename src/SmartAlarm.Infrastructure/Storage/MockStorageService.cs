using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SmartAlarm.Infrastructure.Storage
{
    /// <summary>
    /// Implementação mock de IStorageService para desenvolvimento e testes.
    /// </summary>
    public class MockStorageService : IStorageService
    {
        private readonly ILogger<MockStorageService> _logger;

        public MockStorageService(ILogger<MockStorageService> logger)
        {
            _logger = logger;
        }

        public Task UploadAsync(string path, Stream content)
        {
            _logger.LogInformation("[MockStorage] Upload para {Path}", path);
            return Task.CompletedTask;
        }

        public Task<Stream> DownloadAsync(string path)
        {
            _logger.LogInformation("[MockStorage] Download de {Path}", path);
            return Task.FromResult<Stream>(new MemoryStream());
        }

        public Task DeleteAsync(string path)
        {
            _logger.LogInformation("[MockStorage] Delete de {Path}", path);
            return Task.CompletedTask;
        }
    }
}
