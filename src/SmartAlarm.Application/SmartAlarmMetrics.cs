using System.Diagnostics.Metrics;

namespace SmartAlarm.Application
{
    /// <summary>
    /// Métricas customizadas para Application Layer.
    /// </summary>
    public static class SmartAlarmMetrics
    {
        public static readonly Meter Meter = new("SmartAlarm.Application");
        public static readonly Counter<long> AlarmsCreatedCounter = Meter.CreateCounter<long>("alarms_created");
    }
}
