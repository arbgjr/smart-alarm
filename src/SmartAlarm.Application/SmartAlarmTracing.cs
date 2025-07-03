using System.Diagnostics;

namespace SmartAlarm.Application
{
    /// <summary>
    /// Fonte de tracing para instrumentação OpenTelemetry.
    /// </summary>
    public static class SmartAlarmTracing
    {
        public static readonly ActivitySource ActivitySource = new("SmartAlarm.Application");
    }
}
