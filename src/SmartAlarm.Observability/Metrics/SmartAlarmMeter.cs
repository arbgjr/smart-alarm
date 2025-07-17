using System.Diagnostics.Metrics;
using System;
using System.Collections.Generic;

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
        
        #region Counters
        private readonly Counter<long> _requestCounter;
        private readonly Counter<long> _errorCounter;
        private readonly Counter<long> _alarmCounter;
        private readonly Counter<long> _alarmTriggeredCounter;
        private readonly Counter<long> _userRegistrationCounter;
        private readonly Counter<long> _authenticationAttemptCounter;
        private readonly Counter<long> _monitoringRequestCounter;
        #endregion

        #region Histograms
        private readonly Histogram<double> _requestDuration;
        private readonly Histogram<double> _alarmCreationDuration;
        private readonly Histogram<double> _databaseQueryDuration;
        private readonly Histogram<double> _externalServiceCallDuration;
        #endregion

        #region Gauges
        private readonly ObservableGauge<int> _activeAlarmsGauge;
        private readonly ObservableGauge<int> _onlineUsersGauge;
        private readonly ObservableGauge<long> _systemMemoryUsage;
        private readonly ObservableGauge<int> _databaseConnectionsActive;
        #endregion

        #region Private fields for gauge values
        private int _currentActiveAlarms = 0;
        private int _currentOnlineUsers = 0;
        private long _currentMemoryUsage = 0;
        private int _currentDatabaseConnections = 0;
        #endregion

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

            // Contador de erros
            _errorCounter = _meter.CreateCounter<long>(
                "http_errors_total",
                description: "Total number of HTTP errors");

            // Contador de alarmes criados
            _alarmCounter = _meter.CreateCounter<long>(
                "alarms_created_total",
                description: "Total number of alarms created");

            // Contador de alarmes disparados
            _alarmTriggeredCounter = _meter.CreateCounter<long>(
                "alarms_triggered_total",
                description: "Total number of alarms triggered");

            // Contador de registros de usuário
            _userRegistrationCounter = _meter.CreateCounter<long>(
                "user_registrations_total",
                description: "Total number of user registrations");

            // Contador de tentativas de autenticação
            _authenticationAttemptCounter = _meter.CreateCounter<long>(
                "authentication_attempts_total",
                description: "Total number of authentication attempts");

            // Contador de requests de monitoramento
            _monitoringRequestCounter = _meter.CreateCounter<long>(
                "monitoring_requests_total",
                description: "Total number of monitoring endpoint requests");

            // Histograma de duração de requisições
            _requestDuration = _meter.CreateHistogram<double>(
                "http_request_duration_ms",
                unit: "ms",
                description: "Duration of HTTP requests in milliseconds");

            // Histograma de duração de criação de alarmes
            _alarmCreationDuration = _meter.CreateHistogram<double>(
                "alarm_creation_duration_ms",
                unit: "ms",
                description: "Duration of alarm creation operations in milliseconds");

            // Histograma de duração de queries de banco
            _databaseQueryDuration = _meter.CreateHistogram<double>(
                "database_query_duration_ms",
                unit: "ms",
                description: "Duration of database queries in milliseconds");

            // Histograma de duração de chamadas externas
            _externalServiceCallDuration = _meter.CreateHistogram<double>(
                "external_service_call_duration_ms",
                unit: "ms",
                description: "Duration of external service calls in milliseconds");

            // Observable gauge de alarmes ativos
            _activeAlarmsGauge = _meter.CreateObservableGauge<int>(
                "active_alarms_count",
                () => _currentActiveAlarms,
                "Number of currently active alarms");

            // Observable gauge de usuários online
            _onlineUsersGauge = _meter.CreateObservableGauge<int>(
                "online_users_count",
                () => _currentOnlineUsers,
                "Number of currently online users");

            // Observable gauge de uso de memória
            _systemMemoryUsage = _meter.CreateObservableGauge<long>(
                "system_memory_usage_bytes",
                () => _currentMemoryUsage,
                unit: "bytes",
                description: "Current system memory usage in bytes");

            // Observable gauge de conexões ativas do banco
            _databaseConnectionsActive = _meter.CreateObservableGauge<int>(
                "database_connections_active",
                () => _currentDatabaseConnections,
                "Number of active database connections");
        }

        #region Public Methods - Counters

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
        /// Incrementa o contador de alarmes disparados
        /// </summary>
        /// <param name="alarmType">Tipo do alarme</param>
        /// <param name="userId">ID do usuário</param>
        /// <param name="triggerResult">Resultado do disparo (success/failure)</param>
        public void IncrementAlarmTriggered(string alarmType, string userId, string triggerResult)
        {
            _alarmTriggeredCounter.Add(1, new[]
            {
                new KeyValuePair<string, object?>("alarm_type", alarmType),
                new KeyValuePair<string, object?>("user_id", userId),
                new KeyValuePair<string, object?>("result", triggerResult)
            });
        }

        /// <summary>
        /// Incrementa o contador de registros de usuário
        /// </summary>
        /// <param name="registrationMethod">Método de registro (email, social, etc.)</param>
        public void IncrementUserRegistration(string registrationMethod)
        {
            _userRegistrationCounter.Add(1, new[]
            {
                new KeyValuePair<string, object?>("method", registrationMethod)
            });
        }

        /// <summary>
        /// Incrementa o contador de tentativas de autenticação
        /// </summary>
        /// <param name="result">Resultado da autenticação (success/failure)</param>
        /// <param name="method">Método de autenticação</param>
        public void IncrementAuthenticationAttempt(string result, string method)
        {
            _authenticationAttemptCounter.Add(1, new[]
            {
                new KeyValuePair<string, object?>("result", result),
                new KeyValuePair<string, object?>("method", method)
            });
        }

        /// <summary>
        /// Incrementa o contador de requests de monitoramento
        /// </summary>
        /// <param name="endpoint">Endpoint de monitoramento acessado</param>
        public void IncrementMonitoringRequest(string endpoint)
        {
            _monitoringRequestCounter.Add(1, new[]
            {
                new KeyValuePair<string, object?>("endpoint", endpoint)
            });
        }

        #endregion

        #region Public Methods - Histograms

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
        /// Registra a duração de criação de alarme
        /// </summary>
        /// <param name="durationMs">Duração em milissegundos</param>
        /// <param name="alarmType">Tipo do alarme</param>
        /// <param name="success">Se a operação foi bem-sucedida</param>
        public void RecordAlarmCreationDuration(double durationMs, string alarmType, bool success)
        {
            _alarmCreationDuration.Record(durationMs, new[]
            {
                new KeyValuePair<string, object?>("alarm_type", alarmType),
                new KeyValuePair<string, object?>("success", success)
            });
        }

        /// <summary>
        /// Registra a duração de query de banco
        /// </summary>
        /// <param name="durationMs">Duração em milissegundos</param>
        /// <param name="operation">Tipo da operação (SELECT, INSERT, UPDATE, DELETE)</param>
        /// <param name="table">Tabela principal da query</param>
        public void RecordDatabaseQueryDuration(double durationMs, string operation, string table)
        {
            _databaseQueryDuration.Record(durationMs, new[]
            {
                new KeyValuePair<string, object?>("operation", operation),
                new KeyValuePair<string, object?>("table", table)
            });
        }

        /// <summary>
        /// Registra a duração de chamada de serviço externo
        /// </summary>
        /// <param name="durationMs">Duração em milissegundos</param>
        /// <param name="serviceName">Nome do serviço</param>
        /// <param name="operation">Operação realizada</param>
        /// <param name="success">Se a chamada foi bem-sucedida</param>
        public void RecordExternalServiceCallDuration(double durationMs, string serviceName, string operation, bool success)
        {
            _externalServiceCallDuration.Record(durationMs, new[]
            {
                new KeyValuePair<string, object?>("service", serviceName),
                new KeyValuePair<string, object?>("operation", operation),
                new KeyValuePair<string, object?>("success", success)
            });
        }

        #endregion

        #region Public Methods - Gauges

        /// <summary>
        /// Atualiza o valor de alarmes ativos
        /// </summary>
        /// <param name="count">Número de alarmes ativos</param>
        public void UpdateActiveAlarmsCount(int count)
        {
            _currentActiveAlarms = count;
        }

        /// <summary>
        /// Atualiza o valor de usuários online
        /// </summary>
        /// <param name="count">Número de usuários online</param>
        public void UpdateOnlineUsersCount(int count)
        {
            _currentOnlineUsers = count;
        }

        /// <summary>
        /// Atualiza o valor de uso de memória
        /// </summary>
        /// <param name="memoryUsageBytes">Uso de memória em bytes</param>
        public void UpdateSystemMemoryUsage(long memoryUsageBytes)
        {
            _currentMemoryUsage = memoryUsageBytes;
        }

        /// <summary>
        /// Atualiza o valor de conexões ativas do banco
        /// </summary>
        /// <param name="connectionCount">Número de conexões ativas</param>
        public void UpdateDatabaseConnectionsActive(int connectionCount)
        {
            _currentDatabaseConnections = connectionCount;
        }

        #endregion

        /// <summary>
        /// Libera recursos
        /// </summary>
        public void Dispose()
        {
            _meter?.Dispose();
        }
    }
}
