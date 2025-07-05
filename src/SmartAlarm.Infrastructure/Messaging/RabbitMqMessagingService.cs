using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

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

        public RabbitMqMessagingService(ILogger<RabbitMqMessagingService> logger)
        {
            _logger = logger;
            var envHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST");
            string host;
            if (!string.IsNullOrWhiteSpace(envHost))
            {
                host = envHost;
            }
            else
            {
                // Detecta se está rodando em container (variável DOTNET_RUNNING_IN_CONTAINER)
                var inContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
                host = inContainer ? "rabbitmq" : "localhost";
            }
            var user = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest";
            var pass = Environment.GetEnvironmentVariable("RABBITMQ_PASS") ?? "guest";
            var factory = new ConnectionFactory { HostName = host, UserName = user, Password = pass };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
        }

        public Task PublishEventAsync(string topic, string message)
        {
            _channel.QueueDeclare(queue: topic, durable: false, exclusive: false, autoDelete: false, arguments: null);
            var body = Encoding.UTF8.GetBytes(message);
            _channel.BasicPublish(exchange: "", routingKey: topic, basicProperties: null, body: body);
            _logger.LogInformation("[RabbitMQ] Evento publicado no tópico {Topic}: {Message}", topic, message);
            return Task.CompletedTask;
        }

        public Task SubscribeAsync(string topic, Func<string, Task> handler)
        {
            _channel.QueueDeclare(queue: topic, durable: false, exclusive: false, autoDelete: false, arguments: null);
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                await handler(message);
            };
            _channel.BasicConsume(queue: topic, autoAck: true, consumer: consumer);
            _logger.LogInformation("[RabbitMQ] Subscrito ao tópico {Topic}", topic);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}
