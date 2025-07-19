using Serilog.Core;
using Serilog.Events;
using SmartAlarm.Observability.Context;
using System;

namespace SmartAlarm.Observability.Logging
{
    /// <summary>
    /// Enricher para adicionar informações de correlação aos logs
    /// </summary>
    public class CorrelationEnricher : ILogEventEnricher
    {
        private readonly ICorrelationContext _correlationContext;

        /// <summary>
        /// Construtor
        /// </summary>
        /// <param name="correlationContext">Contexto de correlação</param>
        public CorrelationEnricher(ICorrelationContext correlationContext)
        {
            _correlationContext = correlationContext;
        }

        /// <summary>
        /// Enriquece o evento de log com informações de correlação
        /// </summary>
        /// <param name="logEvent">Evento de log</param>
        /// <param name="propertyFactory">Factory de propriedades</param>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (_correlationContext != null)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("CorrelationId", _correlationContext.CorrelationId));
                
                if (!string.IsNullOrEmpty(_correlationContext.UserId))
                {
                    logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserId", _correlationContext.UserId));
                }
                
                if (!string.IsNullOrEmpty(_correlationContext.SessionId))
                {
                    logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SessionId", _correlationContext.SessionId));
                }
            }
        }
    }

    /// <summary>
    /// Enricher para adicionar informações de performance aos logs
    /// </summary>
    public class PerformanceEnricher : ILogEventEnricher
    {
        /// <summary>
        /// Enriquece o evento de log com informações de performance
        /// </summary>
        /// <param name="logEvent">Evento de log</param>
        /// <param name="propertyFactory">Factory de propriedades</param>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            // Adicionar informações de memória
            var workingSet = GC.GetTotalMemory(false);
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("MemoryUsage", workingSet));

            // Adicionar timestamp preciso
            var timestampUtc = DateTimeOffset.UtcNow;
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TimestampUtc", timestampUtc));

            // Adicionar informações de thread
            var threadId = Environment.CurrentManagedThreadId;
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ManagedThreadId", threadId));
        }
    }

    /// <summary>
    /// Enricher para adicionar informações do ambiente de execução
    /// </summary>
    public class EnvironmentEnricher : ILogEventEnricher
    {
        private static readonly string MachineName = Environment.MachineName;
        private static readonly string ProcessName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
        private static readonly int ProcessId = Environment.ProcessId;
        private static readonly string? EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        /// <summary>
        /// Enriquece o evento de log com informações do ambiente
        /// </summary>
        /// <param name="logEvent">Evento de log</param>
        /// <param name="propertyFactory">Factory de propriedades</param>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("MachineName", MachineName));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ProcessName", ProcessName));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ProcessId", ProcessId));
            
            if (!string.IsNullOrEmpty(EnvironmentName))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Environment", EnvironmentName));
            }
        }
    }
}
