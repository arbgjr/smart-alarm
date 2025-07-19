using System.Diagnostics;
using System;

namespace SmartAlarm.Observability.Tracing
{
    /// <summary>
    /// Activity source customizado para o Smart Alarm
    /// </summary>
    public class SmartAlarmActivitySource : IDisposable
    {
        /// <summary>
        /// Nome do activity source
        /// </summary>
        public const string Name = "SmartAlarm";

        private readonly ActivitySource _activitySource;

        /// <summary>
        /// Construtor
        /// </summary>
        public SmartAlarmActivitySource()
        {
            _activitySource = new ActivitySource(Name, "1.0.0");
        }

        /// <summary>
        /// Inicia uma nova activity
        /// </summary>
        /// <param name="name">Nome da activity</param>
        /// <param name="kind">Tipo da activity</param>
        /// <param name="parentContext">Contexto pai (opcional)</param>
        /// <returns>Activity criada ou null se não houver listeners</returns>
        public Activity? StartActivity(
            string name,
            ActivityKind kind = ActivityKind.Internal,
            ActivityContext parentContext = default)
        {
            return parentContext == default
                ? _activitySource.StartActivity(name, kind)
                : _activitySource.StartActivity(name, kind, parentContext);
        }

        /// <summary>
        /// Inicia uma activity para operação de domínio
        /// </summary>
        /// <param name="operationName">Nome da operação</param>
        /// <param name="entityType">Tipo da entidade</param>
        /// <param name="entityId">ID da entidade (opcional)</param>
        /// <returns>Activity criada</returns>
        public Activity? StartDomainActivity(string operationName, string entityType, string? entityId = null)
        {
            var activity = _activitySource.StartActivity($"domain.{operationName}");
            
            activity?.SetTag("domain.operation", operationName);
            activity?.SetTag("domain.entity.type", entityType);
            
            if (entityId != null)
            {
                activity?.SetTag("domain.entity.id", entityId);
            }

            return activity;
        }

        /// <summary>
        /// Inicia uma activity para operação de repository
        /// </summary>
        /// <param name="operation">Operação (get, create, update, delete)</param>
        /// <param name="entityType">Tipo da entidade</param>
        /// <param name="entityId">ID da entidade (opcional)</param>
        /// <returns>Activity criada</returns>
        public Activity? StartRepositoryActivity(string operation, string entityType, string? entityId = null)
        {
            var activity = _activitySource.StartActivity($"repository.{operation}");
            
            activity?.SetTag("repository.operation", operation);
            activity?.SetTag("repository.entity.type", entityType);
            
            if (entityId != null)
            {
                activity?.SetTag("repository.entity.id", entityId);
            }

            return activity;
        }

        /// <summary>
        /// Inicia uma activity para operação de API externa
        /// </summary>
        /// <param name="serviceName">Nome do serviço</param>
        /// <param name="operation">Operação sendo realizada</param>
        /// <param name="endpoint">Endpoint sendo chamado (opcional)</param>
        /// <returns>Activity criada</returns>
        public Activity? StartExternalApiActivity(string serviceName, string operation, string? endpoint = null)
        {
            var activity = _activitySource.StartActivity($"external.{serviceName}");
            
            activity?.SetTag("external.service", serviceName);
            activity?.SetTag("external.operation", operation);
            
            if (endpoint != null)
            {
                activity?.SetTag("external.endpoint", endpoint);
            }

            return activity;
        }

        /// <summary>
        /// Inicia uma activity para processamento de alarme
        /// </summary>
        /// <param name="alarmId">ID do alarme</param>
        /// <param name="operation">Operação (create, trigger, snooze, dismiss)</param>
        /// <returns>Activity criada</returns>
        public Activity? StartAlarmActivity(string alarmId, string operation)
        {
            var activity = _activitySource.StartActivity($"alarm.{operation}");
            
            activity?.SetTag("alarm.id", alarmId);
            activity?.SetTag("alarm.operation", operation);

            return activity;
        }

        /// <summary>
        /// Adiciona informações de erro a uma activity
        /// </summary>
        /// <param name="activity">Activity</param>
        /// <param name="exception">Exceção</param>
        public static void SetError(Activity? activity, Exception exception)
        {
            if (activity == null) return;

            activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            activity.SetTag("error", true);
            activity.SetTag("error.type", exception.GetType().Name);
            activity.SetTag("error.message", exception.Message);
            
            if (exception.StackTrace != null)
            {
                activity.SetTag("error.stack", exception.StackTrace);
            }
        }

        /// <summary>
        /// Adiciona informações de sucesso a uma activity
        /// </summary>
        /// <param name="activity">Activity</param>
        /// <param name="resultInfo">Informações do resultado (opcional)</param>
        public static void SetSuccess(Activity? activity, string? resultInfo = null)
        {
            if (activity == null) return;

            activity.SetStatus(ActivityStatusCode.Ok);
            activity.SetTag("success", true);
            
            if (resultInfo != null)
            {
                activity.SetTag("result.info", resultInfo);
            }
        }

        /// <summary>
        /// Libera recursos
        /// </summary>
        public void Dispose()
        {
            _activitySource?.Dispose();
        }
    }
}
