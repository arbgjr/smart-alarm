using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartAlarm.Observability.Context;

namespace SmartAlarm.Observability.Tracing
{
    /// <summary>
    /// Serviço para gerenciar distributed tracing com correlação avançada
    /// </summary>
    public interface IDistributedTracingService
    {
        /// <summary>
        /// Inicia uma operação de negócio com tracing completo
        /// </summary>
        Task<T> TraceBusinessOperationAsync<T>(
            string operationName,
            Func<Activity?, Task<T>> operation,
            Dictionary<string, object>? tags = null);

        /// <summary>
        /// Inicia uma operação de domínio com tracing
        /// </summary>
        Task<T> TraceDomainOperationAsync<T>(
            string entityType,
            string operationName,
            string? entityId,
            Func<Activity?, Task<T>> operation,
            Dictionary<string, object>? tags = null);

        /// <summary>
        /// Inicia uma operação de repository com tracing
        /// </summary>
        Task<T> TraceRepositoryOperationAsync<T>(
            string entityType,
            string operation,
            string? entityId,
            Func<Activity?, Task<T>> operation,
            Dictionary<string, object>? tags = null);

        /// <summary>
        /// Inicia uma operação de API externa com tracing
        /// </summary>
        Task<T> TraceExternalApiOperationAsync<T>(
            string serviceName,
            string operation,
            string? endpoint,
            Func<Activity?, Task<T>> operation,
            Dictionary<string, object>? tags = null);

        /// <summary>
        /// Adiciona contexto de correlação ao trace atual
        /// </summary>
        void EnrichCurrentTrace(Dictionary<string, object> enrichmentData);

        /// <summary>
        /// Propaga contexto de trace para operações assíncronas
        /// </summary>
        ActivityContext GetCurrentTraceContext();
    }

    /// <summary>
    /// Implementação do serviço de distributed tracing
    /// </summary>
    public class DistributedTracingService : IDistributedTracingService
    {
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly ICorrelationContext _correlationContext;
        private readonly ILogger<DistributedTracingService> _logger;

        public DistributedTracingService(
            SmartAlarmActivitySource activitySource,
            ICorrelationContext correlationContext,
            ILogger<DistributedTracingService> logger)
        {
            _activitySource = activitySource;
            _correlationContext = correlationContext;
            _logger = logger;
        }

        public async Task<T> TraceBusinessOperationAsync<T>(
            string operationName,
            Func<Activity?, Task<T>> operation,
            Dictionary<string, object>? tags = null)
        {
            using var activity = _activitySource.StartActivity($"business.{operationName}");

            try
            {
                // Adicionar contexto de correlação
                EnrichActivityWithCorrelation(activity);

                // Adicionar tags customizadas
                if (tags != null)
                {
                    foreach (var tag in tags)
                    {
                        activity?.SetTag(tag.Key, tag.Value);
                    }
                }

                activity?.SetTag("operation.type", "business");
                activity?.SetTag("operation.name", operationName);

                _logger.LogDebug("Iniciando operação de negócio: {OperationName}", operationName);

                var result = await operation(activity);

                SmartAlarmActivitySource.SetSuccess(activity, "Business operation completed successfully");

                _logger.LogDebug("Operação de negócio concluída: {OperationName}", operationName);

                return result;
            }
            catch (Exception ex)
            {
                SmartAlarmActivitySource.SetError(activity, ex);
                _logger.LogError(ex, "Erro na operação de negócio: {OperationName}", operationName);
                throw;
            }
        }

        public async Task<T> TraceDomainOperationAsync<T>(
            string entityType,
            string operationName,
            string? entityId,
            Func<Activity?, Task<T>> operation,
            Dictionary<string, object>? tags = null)
        {
            using var activity = _activitySource.StartDomainActivity(operationName, entityType, entityId);

            try
            {
                EnrichActivityWithCorrelation(activity);

                if (tags != null)
                {
                    foreach (var tag in tags)
                    {
                        activity?.SetTag(tag.Key, tag.Value);
                    }
                }

                _logger.LogDebug("Iniciando operação de domínio: {EntityType}.{OperationName} [{EntityId}]",
                    entityType, operationName, entityId ?? "N/A");

                var result = await operation(activity);

                SmartAlarmActivitySource.SetSuccess(activity, $"Domain operation {operationName} completed");

                _logger.LogDebug("Operação de domínio concluída: {EntityType}.{OperationName} [{EntityId}]",
                    entityType, operationName, entityId ?? "N/A");

                return result;
            }
            catch (Exception ex)
            {
                SmartAlarmActivitySource.SetError(activity, ex);
                _logger.LogError(ex, "Erro na operação de domínio: {EntityType}.{OperationName} [{EntityId}]",
                    entityType, operationName, entityId ?? "N/A");
                throw;
            }
        }

        public async Task<T> TraceRepositoryOperationAsync<T>(
            string entityType,
            string operation,
            string? entityId,
            Func<Activity?, Task<T>> operation,
            Dictionary<string, object>? tags = null)
        {
            using var activity = _activitySource.StartRepositoryActivity(operation, entityType, entityId);

            try
            {
                EnrichActivityWithCorrelation(activity);

                if (tags != null)
                {
                    foreach (var tag in tags)
                    {
                        activity?.SetTag(tag.Key, tag.Value);
                    }
                }

                _logger.LogDebug("Iniciando operação de repository: {EntityType}.{Operation} [{EntityId}]",
                    entityType, operation, entityId ?? "N/A");

                var result = await operation(activity);

                SmartAlarmActivitySource.SetSuccess(activity, $"Repository operation {operation} completed");

                _logger.LogDebug("Operação de repository concluída: {EntityType}.{Operation} [{EntityId}]",
                    entityType, operation, entityId ?? "N/A");

                return result;
            }
            catch (Exception ex)
            {
                SmartAlarmActivitySource.SetError(activity, ex);
                _logger.LogError(ex, "Erro na operação de repository: {EntityType}.{Operation} [{EntityId}]",
                    entityType, operation, entityId ?? "N/A");
                throw;
            }
        }

        public async Task<T> TraceExternalApiOperationAsync<T>(
            string serviceName,
            string operation,
            string? endpoint,
            Func<Activity?, Task<T>> operation,
            Dictionary<string, object>? tags = null)
        {
            using var activity = _activitySource.StartExternalApiActivity(serviceName, operation, endpoint);

            try
            {
                EnrichActivityWithCorrelation(activity);

                if (tags != null)
                {
                    foreach (var tag in tags)
                    {
                        activity?.SetTag(tag.Key, tag.Value);
                    }
                }

                _logger.LogDebug("Iniciando chamada para API externa: {ServiceName}.{Operation} [{Endpoint}]",
                    serviceName, operation, endpoint ?? "N/A");

                var result = await operation(activity);

                SmartAlarmActivitySource.SetSuccess(activity, $"External API call to {serviceName} completed");

                _logger.LogDebug("Chamada para API externa concluída: {ServiceName}.{Operation} [{Endpoint}]",
                    serviceName, operation, endpoint ?? "N/A");

                return result;
            }
            catch (Exception ex)
            {
                SmartAlarmActivitySource.SetError(activity, ex);
                _logger.LogError(ex, "Erro na chamada para API externa: {ServiceName}.{Operation} [{Endpoint}]",
                    serviceName, operation, endpoint ?? "N/A");
                throw;
            }
        }

        public void EnrichCurrentTrace(Dictionary<string, object> enrichmentData)
        {
            var currentActivity = Activity.Current;
            if (currentActivity == null) return;

            foreach (var data in enrichmentData)
            {
                currentActivity.SetTag(data.Key, data.Value);
            }

            // Sempre adicionar contexto de correlação atual
            EnrichActivityWithCorrelation(currentActivity);
        }

        public ActivityContext GetCurrentTraceContext()
        {
            var currentActivity = Activity.Current;
            return currentActivity?.Context ?? default;
        }

        /// <summary>
        /// Enriquece uma activity com informações de correlação
        /// </summary>
        private void EnrichActivityWithCorrelation(Activity? activity)
        {
            if (activity == null) return;

            activity.SetTag("correlation.id", _correlationContext.CorrelationId);

            if (!string.IsNullOrEmpty(_correlationContext.UserId))
            {
                activity.SetTag("user.id", _correlationContext.UserId);
            }

            if (!string.IsNullOrEmpty(_correlationContext.SessionId))
            {
                activity.SetTag("session.id", _correlationContext.SessionId);
            }

            // Adicionar propriedades de contexto customizadas
            foreach (var property in _correlationContext.GetAllProperties())
            {
                activity.SetTag($"context.{property.Key.ToLowerInvariant()}", property.Value);
            }
        }
    }
}
