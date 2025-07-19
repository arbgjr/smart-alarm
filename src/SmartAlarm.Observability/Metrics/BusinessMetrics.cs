using System.Diagnostics.Metrics;
using System;
using System.Collections.Generic;

namespace SmartAlarm.Observability.Metrics
{
    /// <summary>
    /// Métricas específicas de negócio do Smart Alarm
    /// </summary>
    public class BusinessMetrics : IDisposable
    {
        /// <summary>
        /// Nome do meter de negócio
        /// </summary>
        public const string Name = "SmartAlarm.Business";

        private readonly Meter _meter;

        #region Business Counters
        private readonly Counter<long> _alarmSnoozeCounter;
        private readonly Counter<long> _alarmDeleteCounter;
        private readonly Counter<long> _fileUploadCounter;
        private readonly Counter<long> _holidayCheckCounter;
        private readonly Counter<long> _reminderSentCounter;
        #endregion

        #region Business Histograms  
        private readonly Histogram<double> _alarmProcessingTime;
        private readonly Histogram<double> _fileProcessingTime;
        private readonly Histogram<double> _userSessionDuration;
        #endregion

        #region Business Gauges
        private readonly ObservableGauge<int> _alarmsPendingToday;
        private readonly ObservableGauge<int> _usersActiveToday;
        private readonly ObservableGauge<double> _systemHealthScore;
        #endregion

        #region Private fields
        private int _currentAlarmsPendingToday = 0;
        private int _currentUsersActiveToday = 0;
        private double _currentSystemHealthScore = 1.0;
        #endregion

        /// <summary>
        /// Construtor
        /// </summary>
        public BusinessMetrics()
        {
            _meter = new Meter(Name, "1.0.0");

            // Contadores de negócio
            _alarmSnoozeCounter = _meter.CreateCounter<long>(
                "alarms_snoozed_total",
                description: "Total number of alarms snoozed by users");

            _alarmDeleteCounter = _meter.CreateCounter<long>(
                "alarms_deleted_total", 
                description: "Total number of alarms deleted by users");

            _fileUploadCounter = _meter.CreateCounter<long>(
                "files_uploaded_total",
                description: "Total number of files uploaded to the system");

            _holidayCheckCounter = _meter.CreateCounter<long>(
                "holiday_checks_total",
                description: "Total number of holiday API checks performed");

            _reminderSentCounter = _meter.CreateCounter<long>(
                "reminders_sent_total",
                description: "Total number of reminders sent to users");

            // Histogramas de negócio
            _alarmProcessingTime = _meter.CreateHistogram<double>(
                "alarm_processing_duration_ms",
                unit: "ms",
                description: "Time taken to process alarm triggers");

            _fileProcessingTime = _meter.CreateHistogram<double>(
                "file_processing_duration_ms", 
                unit: "ms",
                description: "Time taken to process uploaded files");

            _userSessionDuration = _meter.CreateHistogram<double>(
                "user_session_duration_minutes",
                unit: "minutes",
                description: "Duration of user sessions");

            // Gauges de negócio
            _alarmsPendingToday = _meter.CreateObservableGauge<int>(
                "alarms_pending_today",
                () => _currentAlarmsPendingToday,
                description: "Number of alarms pending for today");

            _usersActiveToday = _meter.CreateObservableGauge<int>(
                "users_active_today",
                () => _currentUsersActiveToday,
                description: "Number of users active today");

            _systemHealthScore = _meter.CreateObservableGauge<double>(
                "system_health_score",
                () => _currentSystemHealthScore,
                description: "Overall system health score (0.0 to 1.0)");
        }

        #region Business Counter Methods

        /// <summary>
        /// Incrementa o contador de alarmes adiados
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <param name="snoozeMinutes">Minutos de adiamento</param>
        public void IncrementAlarmSnoozed(string userId, int snoozeMinutes)
        {
            _alarmSnoozeCounter.Add(1, new[]
            {
                new KeyValuePair<string, object?>("user_id", userId),
                new KeyValuePair<string, object?>("snooze_minutes", snoozeMinutes)
            });
        }

        /// <summary>
        /// Incrementa o contador de alarmes deletados
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <param name="alarmType">Tipo do alarme deletado</param>
        /// <param name="reason">Razão da deleção (user_action, expired, etc.)</param>
        public void IncrementAlarmDeleted(string userId, string alarmType, string reason)
        {
            _alarmDeleteCounter.Add(1, new[]
            {
                new KeyValuePair<string, object?>("user_id", userId),
                new KeyValuePair<string, object?>("alarm_type", alarmType),
                new KeyValuePair<string, object?>("reason", reason)
            });
        }

        /// <summary>
        /// Incrementa o contador de uploads de arquivo
        /// </summary>
        /// <param name="fileType">Tipo do arquivo (audio, image, etc.)</param>
        /// <param name="fileSizeKb">Tamanho do arquivo em KB</param>
        /// <param name="uploadResult">Resultado do upload (success, failure)</param>
        public void IncrementFileUploaded(string fileType, long fileSizeKb, string uploadResult)
        {
            _fileUploadCounter.Add(1, new[]
            {
                new KeyValuePair<string, object?>("file_type", fileType),
                new KeyValuePair<string, object?>("file_size_kb", fileSizeKb),
                new KeyValuePair<string, object?>("result", uploadResult)
            });
        }

        /// <summary>
        /// Incrementa o contador de verificações de feriado
        /// </summary>
        /// <param name="country">País consultado</param>
        /// <param name="result">Resultado da consulta (success, failure, holiday_found, no_holiday)</param>
        public void IncrementHolidayCheck(string country, string result)
        {
            _holidayCheckCounter.Add(1, new[]
            {
                new KeyValuePair<string, object?>("country", country),
                new KeyValuePair<string, object?>("result", result)
            });
        }

        /// <summary>
        /// Incrementa o contador de lembretes enviados
        /// </summary>
        /// <param name="reminderType">Tipo do lembrete (alarm, notification, etc.)</param>
        /// <param name="channel">Canal de envio (email, push, sms)</param>
        /// <param name="result">Resultado do envio</param>
        public void IncrementReminderSent(string reminderType, string channel, string result)
        {
            _reminderSentCounter.Add(1, new[]
            {
                new KeyValuePair<string, object?>("reminder_type", reminderType),
                new KeyValuePair<string, object?>("channel", channel),
                new KeyValuePair<string, object?>("result", result)
            });
        }

        #endregion

        #region Business Histogram Methods

        /// <summary>
        /// Registra o tempo de processamento de alarme
        /// </summary>
        /// <param name="durationMs">Duração em milissegundos</param>
        /// <param name="alarmType">Tipo do alarme</param>
        /// <param name="processingStage">Estágio do processamento (trigger, validate, execute, notify)</param>
        public void RecordAlarmProcessingTime(double durationMs, string alarmType, string processingStage)
        {
            _alarmProcessingTime.Record(durationMs, new[]
            {
                new KeyValuePair<string, object?>("alarm_type", alarmType),
                new KeyValuePair<string, object?>("stage", processingStage)
            });
        }

        /// <summary>
        /// Registra o tempo de processamento de arquivo
        /// </summary>
        /// <param name="durationMs">Duração em milissegundos</param>
        /// <param name="fileType">Tipo do arquivo</param>
        /// <param name="operation">Operação realizada (upload, convert, validate)</param>
        public void RecordFileProcessingTime(double durationMs, string fileType, string operation)
        {
            _fileProcessingTime.Record(durationMs, new[]
            {
                new KeyValuePair<string, object?>("file_type", fileType),
                new KeyValuePair<string, object?>("operation", operation)
            });
        }

        /// <summary>
        /// Registra a duração da sessão do usuário
        /// </summary>
        /// <param name="durationMinutes">Duração em minutos</param>
        /// <param name="sessionType">Tipo da sessão (web, mobile, api)</param>
        /// <param name="endReason">Razão do fim da sessão (logout, timeout, force_logout)</param>
        public void RecordUserSessionDuration(double durationMinutes, string sessionType, string endReason)
        {
            _userSessionDuration.Record(durationMinutes, new[]
            {
                new KeyValuePair<string, object?>("session_type", sessionType),
                new KeyValuePair<string, object?>("end_reason", endReason)
            });
        }

        #endregion

        #region Business Gauge Methods

        /// <summary>
        /// Atualiza o número de alarmes pendentes para hoje
        /// </summary>
        /// <param name="count">Número de alarmes pendentes</param>
        public void UpdateAlarmsPendingToday(int count)
        {
            _currentAlarmsPendingToday = count;
        }

        /// <summary>
        /// Atualiza o número de usuários ativos hoje
        /// </summary>
        /// <param name="count">Número de usuários ativos</param>
        public void UpdateUsersActiveToday(int count)
        {
            _currentUsersActiveToday = count;
        }

        /// <summary>
        /// Atualiza o score de saúde do sistema
        /// </summary>
        /// <param name="score">Score entre 0.0 e 1.0</param>
        public void UpdateSystemHealthScore(double score)
        {
            // Garante que o score está entre 0.0 e 1.0
            _currentSystemHealthScore = Math.Max(0.0, Math.Min(1.0, score));
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Calcula e atualiza o score de saúde baseado nos health checks
        /// </summary>
        /// <param name="healthyComponents">Número de componentes saudáveis</param>
        /// <param name="totalComponents">Número total de componentes</param>
        /// <param name="degradedComponents">Número de componentes degradados</param>
        public void CalculateSystemHealthScore(int healthyComponents, int totalComponents, int degradedComponents)
        {
            if (totalComponents == 0)
            {
                UpdateSystemHealthScore(1.0);
                return;
            }

            // Score baseado na porcentagem de componentes saudáveis
            var healthyRatio = (double)healthyComponents / totalComponents;
            
            // Penalidade por componentes degradados (50% do peso dos saudáveis)
            var degradedPenalty = ((double)degradedComponents / totalComponents) * 0.5;
            
            var finalScore = healthyRatio - degradedPenalty;
            UpdateSystemHealthScore(finalScore);
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
