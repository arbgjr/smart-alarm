using System.Threading.Tasks;

namespace SmartAlarm.Infrastructure.Messaging
{
    /// <summary>
    /// Abstração para serviço de mensageria (eventos, filas, etc).
    /// </summary>
    public interface IMessagingService
    {
        Task PublishEventAsync(string topic, string message);
        Task SubscribeAsync(string topic, System.Func<string, Task> handler);
    }
}
