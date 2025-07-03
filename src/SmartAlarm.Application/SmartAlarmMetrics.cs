using System.Diagnostics.Metrics;

namespace SmartAlarm.Application
{
    /// <summary>
    /// Métricas customizadas para Application Layer.
    /// </summary>
    public static class SmartAlarmMetrics
    {
        public static readonly Meter Meter = new("SmartAlarm.Application");
        
        // Contadores de operações
        public static readonly Counter<long> AlarmsCreatedCounter = Meter.CreateCounter<long>("alarms_created", "count", "Total number of alarms created");
        public static readonly Counter<long> AlarmsUpdatedCounter = Meter.CreateCounter<long>("alarms_updated", "count", "Total number of alarms updated");
        public static readonly Counter<long> AlarmsDeletedCounter = Meter.CreateCounter<long>("alarms_deleted", "count", "Total number of alarms deleted");
        public static readonly Counter<long> AlarmsRetrievedCounter = Meter.CreateCounter<long>("alarms_retrieved", "count", "Total number of alarms retrieved");
        public static readonly Counter<long> AlarmsListedCounter = Meter.CreateCounter<long>("alarms_listed", "count", "Total number of alarm list operations");
        
        // Contadores de erros
        public static readonly Counter<long> ValidationErrorsCounter = Meter.CreateCounter<long>("validation_errors", "count", "Total number of validation errors");
        public static readonly Counter<long> NotFoundErrorsCounter = Meter.CreateCounter<long>("not_found_errors", "count", "Total number of not found errors");
        
        // Histogramas para tempos de resposta (será usado pelos handlers se necessário)
        public static readonly Histogram<double> HandlerDuration = Meter.CreateHistogram<double>("handler_duration", "ms", "Duration of handler execution");
    }
}
