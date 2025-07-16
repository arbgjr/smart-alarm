using System.Diagnostics.Metrics;
using System;

namespace SmartAlarm.Observability.Metrics
{
    /// <summary>
    /// Métricas customizadas do Smart Alarm
    /// </summary>
    public class SmartAlarmMeter : IDisposable
    {
        /// <summary>
        /// Nome do meter
        /// </summary>
        public const string Name = "SmartAlarm";

        private readonly Meter _meter;
        private readonly Counter<long> _requestCounter;
        private readonly Histogram<double> _requestDuration;
        private readonly Counter<long> _errorCounter;
        private readonly Counter<long> _alarmCounter;
        private readonly ObservableGauge<int> _activeAlarmsGauge;

        private int _currentActiveAlarms = 0;

        /// <summary>
        /// Construtor
        /// </summary>
        public SmartAlarmMeter()
        {
            _meter = new Meter(Name, "1.0.0");

            // Contador de requisições HTTP
            _requestCounter = _meter.CreateCounter<long>(
                "http_requests_total",
                description: "Total number of HTTP requests");

            // Histograma de duração de requisições
            _requestDuration = _meter.CreateHistogram<double>(
                "http_request_duration_ms",
                unit: "ms",
                description: "Duration of HTTP requests in milliseconds");

            // Contador de erros
            _errorCounter = _meter.CreateCounter<long>(
                "http_errors_total",
                description: "Total number of HTTP errors");

            // Contador de alarmes criados
            _alarmCounter = _meter.CreateCounter<long>(
                "alarms_created_total",
                description: "Total number of alarms created");

            // Observable gauge de alarmes ativos
            _activeAlarmsGauge = _meter.CreateObservableGauge<int>(
                "active_alarms_count",
                () => _currentActiveAlarms,
                "Number of currently active alarms");
        }

        /// <summary>
        /// Incrementa o contador de requisições
        /// </summary>
        /// <param name="method">Método HTTP</param>
        /// <param name="path">Caminho da requisição</param>
        public void IncrementRequestCount(string method, string path)
        {
            _requestCounter.Add(1, new[]
            {
                new KeyValuePair<string, object?>("method", method),
                new KeyValuePair<string, object?>("path", path)
            });
        }

        /// <summary>
        /// Registra a duração de uma requisição
        /// </summary>
        /// <param name="durationMs">Duração em milissegundos</param>
        /// <param name="method">Método HTTP</param>
        /// <param name="path">Caminho da requisição</param>
        /// <param name="statusCode">Código de status da resposta</param>
        public void RecordRequestDuration(double durationMs, string method, string path, string statusCode)
        {
            _requestDuration.Record(durationMs, new[]
            {
                new KeyValuePair<string, object?>("method", method),
                new KeyValuePair<string, object?>("path", path),
                new KeyValuePair<string, object?>("status_code", statusCode)
            });
        }

        /// <summary>
        /// Incrementa o contador de erros
        /// </summary>
        /// <param name="method">Método HTTP</param>
        /// <param name="path">Caminho da requisição</param>
        /// <param name="errorType">Tipo do erro</param>
        public void IncrementErrorCount(string method, string path, string errorType)
        {
            _errorCounter.Add(1, new[]
            {
                new KeyValuePair<string, object?>("method", method),
                new KeyValuePair<string, object?>("path", path),
                new KeyValuePair<string, object?>("error_type", errorType)
            });
        }

        /// <summary>
        /// Incrementa o contador de alarmes criados
        /// </summary>
        /// <param name="alarmType">Tipo do alarme</param>
        /// <param name="userId">ID do usuário</param>
        public void IncrementAlarmCount(string alarmType, string userId)
        {
            _alarmCounter.Add(1, new[]
            {
                new KeyValuePair<string, object?>("alarm_type", alarmType),
                new KeyValuePair<string, object?>("user_id", userId)
            });
        }

        /// <summary>
        /// Atualiza o valor de alarmes ativos
        /// </summary>
        /// <param name="count">Número de alarmes ativos</param>
        /// <param name="alarmType">Tipo do alarme (não usado no observable gauge simples)</param>
        public void UpdateActiveAlarmsCount(int count, string? alarmType = null)
        {
            _currentActiveAlarms = count;
        }

        /// <summary>
        /// Libera recursos
        /// </summary>
        public void Dispose()
        {
            _meter?.Dispose();
        }
    }
}
