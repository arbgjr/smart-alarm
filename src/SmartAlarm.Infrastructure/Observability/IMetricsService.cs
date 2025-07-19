using System.Threading.Tasks;

namespace SmartAlarm.Infrastructure.Observability
{
    /// <summary>
    /// Abstração para coleta de métricas customizadas.
    /// </summary>
    public interface IMetricsService
    {
        Task IncrementAsync(string metricName);
        Task RecordAsync(string metricName, double value);
    }
}
