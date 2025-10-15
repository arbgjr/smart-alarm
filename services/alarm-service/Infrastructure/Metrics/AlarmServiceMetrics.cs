using System.Diagnostics.Metrics;
using SmartAlarm.Observability.Metrics;

namespace SmartAlarm.AlarmService.Infrastructure.Metrics
{
    /// <summary>
    /// Métricas específicas do Alarm Service
    /// </summary>
    public class AlarmServiceMetrics : IDisposable
    {
        private readonly Meter _meter;
        private readonly Counter<long> _alarmsProcessedCounter;
        private readonly Counter<long> _alarmFailuresCounter;
        private readonly Histogram<double> _alarmProcessingDuration;
        private readonly Histogram<double> _queueWaitTime;
        private readonly UpDownCounter<long> _activeAlarmsGauge;
        private readonly UpDownCounter<long> _queueSizeGauge;
        private readonly Counter<long> _retryAttemptsCounter;
        private readonly Histogram<double> _notificationDeliveryTime;

        public AlarmServiceMetrics()
        {
            _meter = new Meter("SmartAlarm.AlarmService", "1.0.0");

            // Contadores
            _alarmsProcessedCounter = _meter.CreateCounter<long>(
                "smartalarm_alarms_processed_total",
                description: "Total number of alarms processed");

            _alarmFailuresCounter = _meter.CreateCounter<long>(
                "smartalarm_alarm_failures_total",
                description: "Total number of alarm processing failures");

            _retryAttemptsCounter = _meter.CreateCounter<long>(
                "smartalarm_alarm_retries_total",
                description: "Total number of alarm retry attempts");

            // Histogramas
            _alarmProcessingDuration = _meter.CreateHistogram<double>(
                "smartalarm_alarm_processing_duration_seconds",
                unit: "s",
                description: "Duration of alarm processing in seconds");

            _queueWaitTime = _meter.CreateHistogram<double>(
                "smartalarm_queue_wait_time_seconds",
                unit: "s",
                description: "Time alarms spend waiting in queue");

            _notificationDeliveryTime = _meter.CreateHistogram<double>(
                "smartalarm_notification_delivery_duration_seconds",
                unit: "s",
                description: "Time taken to deliver notifications");

            // Gauges
            _activeAlarmsGauge = _meter.CreateUpDownCounter<long>(
                "smartalarm_active_alarms_current",
                description: "Current number of active alarms being processed");

            _queueSizeGauge = _meter.CreateUpDownCounter<long>(
                "smartalarm_queue_size_current",
                description: "Current size of alarm processing queue");
        }

        /// <summary>
        /// Registra processamento bem-sucedido de alarme
        /// </summary>
        public void RecordAlarmProcessed(
            string triggerType,
            string userId,
            double durationSeconds,
            Dictionary<string, object>? additionalTags = null)
        {
            var tags = new List<KeyValuePair<string, object?>>
            {
                new("trigger_type", triggerType),
                new("user_id", userId),
                new("status", "success")
            };

            if (additionalTags != null)
            {
                tags.AddRange(additionalTags.Select(kvp => new KeyValuePair<string, object?>(kvp.Key, kvp.Value)));
            }

            _alarmsProcessedCounter.Add(1, tags.ToArray());
            _alarmProcessingDuration.Record(durationSeconds, tags.ToArray());
        }

        /// <summary>
        /// Registra falha no processamento de alarme
        /// </summary>
        public void RecordAlarmFailure(
            string triggerType,
            string userId,
            string errorType,
            double durationSeconds,
            Dictionary<string, object>? additionalTags = null)
        {
            var tags = new List<KeyValuePair<string, object?>>
            {
                new("trigger_type", triggerType),
                new("user_id", userId),
                new("error_type", errorType),
                new("status", "failed")
            };

            if (additionalTags != null)
            {
                tags.AddRange(additionalTags.Select(kvp => new KeyValuePair<string, object?>(kvp.Key, kvp.Value)));
            }

            _alarmFailuresCounter.Add(1, tags.ToArray());
            _alarmProcessingDuration.Record(durationSeconds, tags.ToArray());
        }

        /// <summary>
        /// Registra tentativa de retry
        /// </summary>
        public void RecordRetryAttempt(
            string triggerType,
            string userId,
            int retryCount,
            string reason)
        {
            var tags = new KeyValuePair<string, object?>[]
            {
                new("trigger_type", triggerType),
                new("user_id", userId),
                new("retry_count", retryCount),
                new("reason", reason)
            };

            _retryAttemptsCounter.Add(1, tags);
        }

        /// <summary>
        /// Registra tempo de espera na fila
        /// </summary>
        public void RecordQueueWaitTime(
            double waitTimeSeconds,
            string queueName,
            string priority = "normal")
        {
            var tags = new KeyValuePair<string, object?>[]
            {
                new("queue_name", queueName),
                new("priority", priority)
            };

            _queueWaitTime.Record(waitTimeSeconds, tags);
        }

        /// <summary>
        /// Registra tempo de entrega de notificação
        /// </summary>
        public void RecordNotificationDelivery(
            double deliveryTimeSeconds,
            string notificationType,
            string status,
            string userId)
        {
            var tags = new KeyValuePair<string, object?>[]
            {
                new("notification_type", notificationType),
                new("status", status),
                new("user_id", userId)
            };

            _notificationDeliveryTime.Record(deliveryTimeSeconds, tags);
        }

        /// <summary>
        /// Atualiza número de alarmes ativos
        /// </summary>
        public void UpdateActiveAlarms(long count, string processingNode)
        {
            var tags = new KeyValuePair<string, object?>[]
            {
                new("processing_node", processingNode)
            };

            _activeAlarmsGauge.Add(count, tags);
        }

        /// <summary>
        /// Atualiza tamanho da fila
        /// </summary>
        public void UpdateQueueSize(long size, string queueName)
        {
            var tags = new KeyValuePair<string, object?>[]
            {
                new("queue_name", queueName)
            };

            _queueSizeGauge.Add(size, tags);
        }

        /// <summary>
        /// Registra métricas de throughput
        /// </summary>
        public void RecordThroughput(
            long alarmsPerMinute,
            string timeWindow,
            string processingNode)
        {
            // Usar um gauge customizado para throughput
            var throughputGauge = _meter.CreateUpDownCounter<long>(
                "smartalarm_throughput_alarms_per_minute",
                description: "Number of alarms processed per minute");

            var tags = new KeyValuePair<string, object?>[]
            {
                new("time_window", timeWindow),
                new("processing_node", processingNode)
            };

            throughputGauge.Add(alarmsPerMinute, tags);
        }

        /// <summary>
        /// Registra métricas de latência de sistema
        /// </summary>
        public void RecordSystemLatency(
            double latencySeconds,
            string component,
            string operation)
        {
            var latencyHistogram = _meter.CreateHistogram<double>(
                "smartalarm_system_latency_seconds",
                unit: "s",
                description: "System component latency");

            var tags = new KeyValuePair<string, object?>[]
            {
                new("component", component),
                new("operation", operation)
            };

            latencyHistogram.Record(latencySeconds, tags);
        }

        /// <summary>
        /// Registra métricas de recursos do sistema
        /// </summary>
        public void RecordResourceUsage(
            double cpuPercent,
            double memoryMB,
            long activeConnections,
            string processingNode)
        {
            var cpuGauge = _meter.CreateUpDownCounter<double>(
                "smartalarm_cpu_usage_percent",
                description: "CPU usage percentage");

            var memoryGauge = _meter.CreateUpDownCounter<double>(
                "smartalarm_memory_usage_mb",
                unit: "MB",
                description: "Memory usage in megabytes");

            var connectionsGauge = _meter.CreateUpDownCounter<long>(
                "smartalarm_active_connections_current",
                description: "Current number of active connections");

            var tags = new KeyValuePair<string, object?>[]
            {
                new("processing_node", processingNode)
            };

            cpuGauge.Add(cpuPercent, tags);
            memoryGauge.Add(memoryMB, tags);
            connectionsGauge.Add(activeConnections, tags);
        }

        /// <summary>
        /// Registra métricas de qualidade de serviço
        /// </summary>
        public void RecordServiceQuality(
            double availabilityPercent,
            double errorRatePercent,
            double averageResponseTimeSeconds,
            string timeWindow)
        {
            var availabilityGauge = _meter.CreateUpDownCounter<double>(
                "smartalarm_availability_percent",
                description: "Service availability percentage");

            var errorRateGauge = _meter.CreateUpDownCounter<double>(
                "smartalarm_error_rate_percent",
                description: "Error rate percentage");

            var responseTimeGauge = _meter.CreateUpDownCounter<double>(
                "smartalarm_avg_response_time_seconds",
                unit: "s",
                description: "Average response time");

            var tags = new KeyValuePair<string, object?>[]
            {
                new("time_window", timeWindow)
            };

            availabilityGauge.Add(availabilityPercent, tags);
            errorRateGauge.Add(errorRatePercent, tags);
            responseTimeGauge.Add(averageResponseTimeSeconds, tags);
        }

        public void Dispose()
        {
            _meter?.Dispose();
        }
    }

    /// <summary>
    /// Extensões para facilitar o uso das métricas
    /// </summary>
    public static class AlarmServiceMetricsExtensions
    {
        /// <summary>
        /// Registra métricas de processamento com timing automático
        /// </summary>
        public static async Task<T> MeasureAlarmProcessingAsync<T>(
            this AlarmServiceMetrics metrics,
            Func<Task<T>> operation,
            string triggerType,
            string userId,
            Dictionary<string, object>? additionalTags = null)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var result = await operation();
                stopwatch.Stop();

                metrics.RecordAlarmProcessed(
                    triggerType,
                    userId,
                    stopwatch.Elapsed.TotalSeconds,
                    additionalTags);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                metrics.RecordAlarmFailure(
                    triggerType,
                    userId,
                    ex.GetType().Name,
                    stopwatch.Elapsed.TotalSeconds,
                    additionalTags);

                throw;
            }
        }

        /// <summary>
        /// Registra métricas de notificação com timing automático
        /// </summary>
        public static async Task<T> MeasureNotificationDeliveryAsync<T>(
            this AlarmServiceMetrics metrics,
            Func<Task<T>> operation,
            string notificationType,
            string userId)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var result = await operation();
                stopwatch.Stop();

                metrics.RecordNotificationDelivery(
                    stopwatch.Elapsed.TotalSeconds,
                    notificationType,
                    "success",
                    userId);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                metrics.RecordNotificationDelivery(
                    stopwatch.Elapsed.TotalSeconds,
                    notificationType,
                    "failed",
                    userId);

                throw;
            }
        }
    }
}
