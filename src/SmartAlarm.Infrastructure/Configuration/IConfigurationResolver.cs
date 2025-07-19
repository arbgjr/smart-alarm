using System.Threading;
using System.Threading.Tasks;

namespace SmartAlarm.Infrastructure.Configuration
{
    public interface IConfigurationResolver
    {
        Task<string> GetConfigAsync(string key, CancellationToken cancellationToken = default);
    }
}
