using System.IO;
using System.Threading.Tasks;

namespace SmartAlarm.Infrastructure.Storage
{
    /// <summary>
    /// Abstração para serviço de armazenamento de arquivos.
    /// </summary>
    public interface IStorageService
    {
        Task UploadAsync(string path, Stream content);
        Task<Stream> DownloadAsync(string path);
        Task DeleteAsync(string path);
    }
}
