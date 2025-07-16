using Microsoft.Extensions.Logging;
using System;

namespace SmartAlarm.Observability.Extensions.Logging
{
    /// <summary>
    /// Extensões para logging estruturado no Smart Alarm
    /// </summary>
    public static class LoggerExtensions
    {
        /// <summary>
        /// Log de início de operação de alarme
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="alarmId">ID do alarme</param>
        /// <param name="operation">Operação sendo realizada</param>
        /// <param name="userId">ID do usuário</param>
        public static void LogAlarmOperationStarted(this ILogger logger, string alarmId, string operation, string? userId = null)
        {
            logger.LogInformation("Alarm operation started: {Operation} for alarm {AlarmId} by user {UserId}",
                operation, alarmId, userId ?? "system");
        }

        /// <summary>
        /// Log de conclusão de operação de alarme
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="alarmId">ID do alarme</param>
        /// <param name="operation">Operação realizada</param>
        /// <param name="durationMs">Duração em milissegundos</param>
        /// <param name="userId">ID do usuário</param>
        public static void LogAlarmOperationCompleted(this ILogger logger, string alarmId, string operation, long durationMs, string? userId = null)
        {
            logger.LogInformation("Alarm operation completed: {Operation} for alarm {AlarmId} by user {UserId} in {Duration}ms",
                operation, alarmId, userId ?? "system", durationMs);
        }

        /// <summary>
        /// Log de erro em operação de alarme
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="exception">Exceção</param>
        /// <param name="alarmId">ID do alarme</param>
        /// <param name="operation">Operação que falhou</param>
        /// <param name="userId">ID do usuário</param>
        public static void LogAlarmOperationFailed(this ILogger logger, Exception exception, string alarmId, string operation, string? userId = null)
        {
            logger.LogError(exception, "Alarm operation failed: {Operation} for alarm {AlarmId} by user {UserId}",
                operation, alarmId, userId ?? "system");
        }

        /// <summary>
        /// Log de disparo de alarme
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="alarmId">ID do alarme</param>
        /// <param name="alarmType">Tipo do alarme</param>
        /// <param name="scheduledTime">Horário programado</param>
        /// <param name="actualTime">Horário real de disparo</param>
        public static void LogAlarmTriggered(this ILogger logger, string alarmId, string alarmType, DateTime scheduledTime, DateTime actualTime)
        {
            var delayMs = (actualTime - scheduledTime).TotalMilliseconds;
            logger.LogInformation("Alarm triggered: {AlarmId} of type {AlarmType}. Scheduled: {ScheduledTime}, Actual: {ActualTime}, Delay: {DelayMs}ms",
                alarmId, alarmType, scheduledTime, actualTime, delayMs);
        }

        /// <summary>
        /// Log de operação de repositório
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="operation">Operação (Create, Read, Update, Delete)</param>
        /// <param name="entityType">Tipo da entidade</param>
        /// <param name="entityId">ID da entidade</param>
        /// <param name="durationMs">Duração em milissegundos</param>
        public static void LogRepositoryOperation(this ILogger logger, string operation, string entityType, string entityId, long durationMs)
        {
            logger.LogDebug("Repository operation: {Operation} {EntityType} {EntityId} completed in {Duration}ms",
                operation, entityType, entityId, durationMs);
        }

        /// <summary>
        /// Log de erro em repositório
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="exception">Exceção</param>
        /// <param name="operation">Operação que falhou</param>
        /// <param name="entityType">Tipo da entidade</param>
        /// <param name="entityId">ID da entidade</param>
        public static void LogRepositoryError(this ILogger logger, Exception exception, string operation, string entityType, string? entityId = null)
        {
            logger.LogError(exception, "Repository operation failed: {Operation} {EntityType} {EntityId}",
                operation, entityType, entityId ?? "unknown");
        }

        /// <summary>
        /// Log de chamada para API externa
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="serviceName">Nome do serviço</param>
        /// <param name="endpoint">Endpoint chamado</param>
        /// <param name="method">Método HTTP</param>
        /// <param name="statusCode">Código de status da resposta</param>
        /// <param name="durationMs">Duração em milissegundos</param>
        public static void LogExternalApiCall(this ILogger logger, string serviceName, string endpoint, string method, int statusCode, long durationMs)
        {
            if (statusCode >= 200 && statusCode < 300)
            {
                logger.LogInformation("External API call successful: {Method} {ServiceName}{Endpoint} -> {StatusCode} in {Duration}ms",
                    method, serviceName, endpoint, statusCode, durationMs);
            }
            else
            {
                logger.LogWarning("External API call failed: {Method} {ServiceName}{Endpoint} -> {StatusCode} in {Duration}ms",
                    method, serviceName, endpoint, statusCode, durationMs);
            }
        }

        /// <summary>
        /// Log de erro em chamada para API externa
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="exception">Exceção</param>
        /// <param name="serviceName">Nome do serviço</param>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="method">Método HTTP</param>
        public static void LogExternalApiError(this ILogger logger, Exception exception, string serviceName, string endpoint, string method)
        {
            logger.LogError(exception, "External API call error: {Method} {ServiceName}{Endpoint}",
                method, serviceName, endpoint);
        }

        /// <summary>
        /// Log de operação de validação
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="entityType">Tipo da entidade validada</param>
        /// <param name="validationErrors">Erros de validação</param>
        public static void LogValidationFailed(this ILogger logger, string entityType, string[] validationErrors)
        {
            logger.LogWarning("Validation failed for {EntityType}: {ValidationErrors}",
                entityType, string.Join(", ", validationErrors));
        }

        /// <summary>
        /// Log de evento de negócio
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="eventName">Nome do evento</param>
        /// <param name="entityId">ID da entidade relacionada</param>
        /// <param name="userId">ID do usuário</param>
        /// <param name="additionalData">Dados adicionais</param>
        public static void LogBusinessEvent(this ILogger logger, string eventName, string entityId, string? userId = null, object? additionalData = null)
        {
            if (additionalData != null)
            {
                logger.LogInformation("Business event: {EventName} for entity {EntityId} by user {UserId} with data {@AdditionalData}",
                    eventName, entityId, userId ?? "system", additionalData);
            }
            else
            {
                logger.LogInformation("Business event: {EventName} for entity {EntityId} by user {UserId}",
                    eventName, entityId, userId ?? "system");
            }
        }
    }
}
