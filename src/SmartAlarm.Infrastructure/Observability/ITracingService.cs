using System.Threading.Tasks;

namespace SmartAlarm.Infrastructure.Observability
{
    /// <summary>
    /// Abstração para tracing/distribuição de rastreamento.
    /// </summary>
    public interface ITracingService
    {
        Task TraceAsync(string operation, string message);
    }
}
