using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Logging;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;

namespace SmartAlarm.Infrastructure.Messaging
{
    /// <summary>
    /// Implementação real de IMessagingService usando RabbitMQ.
    /// </summary>
    public class RabbitMqMessagingService : IMessagingService, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<RabbitMqMessagingService> _logger;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly SmartAlarmActivitySource _activitySource;

        public RabbitMqMessagingService(
            ILogger<RabbitMqMessagingService> logger,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            SmartAlarmActivitySource activitySource)
        {
            _logger = logger;
            _meter = meter;
            _correlationContext = correlationContext;
            _activitySource = activitySource;
            
            // Log das variáveis de ambiente para depuração
            _logger.LogInformation("ASPNETCORE_ENVIRONMENT: {Env}", 
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "não definido");
            _logger.LogInformation("DOTNET_ENVIRONMENT: {Env}", 
                Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "não definido");
            _logger.LogInformation("DOTNET_RUNNING_IN_CONTAINER: {InContainer}", 
                Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") ?? "não definido");
            _logger.LogInformation("RABBITMQ_HOST: {Host}", 
                Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "não definido");
                
            // Determinar o host do RabbitMQ
            string host = "localhost"; // valor padrão
            
            // Se RABBITMQ_HOST estiver definido, usar esse valor
            var envHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST");
            if (!string.IsNullOrWhiteSpace(envHost))
            {
                host = envHost;
                _logger.LogInformation("Usando host RabbitMQ da variável de ambiente: {Host}", host);
            }
            // Se estiver em contêiner e sem variável de ambiente específica, usar o nome do serviço
            else if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
            {
                host = "rabbitmq";
                _logger.LogInformation("Em contêiner: usando nome do serviço como host: {Host}", host);
            }
            else
            {
                _logger.LogInformation("Usando host local para RabbitMQ: {Host}", host);
            }
            var user = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest";
            var pass = Environment.GetEnvironmentVariable("RABBITMQ_PASS") ?? "guest";
            var factory = new ConnectionFactory { HostName = host, UserName = user, Password = pass };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
        }

        public Task PublishEventAsync(string topic, string message)
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = _correlationContext.CorrelationId;
            
            using var activity = _activitySource.StartActivity("RabbitMqMessagingService.PublishEventAsync");
            activity?.SetTag("messaging.system", "rabbitmq");
            activity?.SetTag("messaging.destination", topic);
            activity?.SetTag("messaging.operation", "publish");
            activity?.SetTag("correlation.id", correlationId);
            
            try
            {
                _logger.LogDebug(LogTemplates.MessagingOperationStarted, 
                    "RabbitMQ", "PublishEvent", topic, correlationId);
                
                _channel.QueueDeclare(queue: topic, durable: false, exclusive: false, autoDelete: false, arguments: null);
                var body = Encoding.UTF8.GetBytes(message);
                _channel.BasicPublish(exchange: "", routingKey: topic, basicProperties: null, body: body);
                
                stopwatch.Stop();
                _meter.RecordExternalServiceCallDuration(stopwatch.ElapsedMilliseconds, "messaging", "rabbitmq", true);
                
                _logger.LogInformation(LogTemplates.MessagingOperationCompleted, 
                    "RabbitMQ", "PublishEvent", topic, stopwatch.ElapsedMilliseconds, correlationId);
                    
                activity?.SetStatus(ActivityStatusCode.Ok);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.IncrementErrorCount("MESSAGING", "RabbitMQ", "PublishError");
                
                _logger.LogError(LogTemplates.MessagingOperationFailed, ex,
                    "RabbitMQ", "PublishEvent", topic, ex.Message, correlationId);
                    
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }

        public Task SubscribeAsync(string topic, Func<string, Task> handler)
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = _correlationContext.CorrelationId;
            
            using var activity = _activitySource.StartActivity("RabbitMqMessagingService.SubscribeAsync");
            activity?.SetTag("messaging.system", "rabbitmq");
            activity?.SetTag("messaging.destination", topic);
            activity?.SetTag("messaging.operation", "subscribe");
            activity?.SetTag("correlation.id", correlationId);
            
            try
            {
                _logger.LogDebug(LogTemplates.MessagingOperationStarted, 
                    "RabbitMQ", "Subscribe", topic, correlationId);
                
                _channel.QueueDeclare(queue: topic, durable: false, exclusive: false, autoDelete: false, arguments: null);
                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += async (model, ea) =>
                {
                    var handlerStopwatch = Stopwatch.StartNew();
                    try
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        await handler(message);
                        
                        handlerStopwatch.Stop();
                        _meter.RecordExternalServiceCallDuration(handlerStopwatch.ElapsedMilliseconds, "messaging", "rabbitmq_handler", true);
                        
                        _logger.LogDebug(LogTemplates.MessagingOperationCompleted, 
                            "RabbitMQ", "MessageReceived", topic, handlerStopwatch.ElapsedMilliseconds, correlationId);
                    }
                    catch (Exception ex)
                    {
                        handlerStopwatch.Stop();
                        _meter.IncrementErrorCount("MESSAGING", "RabbitMQ", "HandlerError");
                        
                        _logger.LogError(LogTemplates.MessagingOperationFailed, ex,
                            "RabbitMQ", "MessageHandler", topic, ex.Message, correlationId);
                    }
                };
                _channel.BasicConsume(queue: topic, autoAck: true, consumer: consumer);
                
                stopwatch.Stop();
                _meter.RecordExternalServiceCallDuration(stopwatch.ElapsedMilliseconds, "messaging", "rabbitmq", true);
                
                _logger.LogInformation(LogTemplates.MessagingOperationCompleted, 
                    "RabbitMQ", "Subscribe", topic, stopwatch.ElapsedMilliseconds, correlationId);
                    
                activity?.SetStatus(ActivityStatusCode.Ok);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.IncrementErrorCount("MESSAGING", "RabbitMQ", "SubscribeError");
                
                _logger.LogError(LogTemplates.MessagingOperationFailed, ex,
                    "RabbitMQ", "Subscribe", topic, ex.Message, correlationId);
                    
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}
