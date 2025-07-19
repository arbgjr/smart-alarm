using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Collections.Generic;
using System.Text.Json;
using System.Diagnostics;
using SmartAlarm.Observability.Logging;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;
using Oci.StreamingService;
using Oci.StreamingService.Requests;
using Oci.StreamingService.Models;
using Oci.StreamingService.Responses;
using Oci.Common.Auth;

namespace SmartAlarm.Infrastructure.Messaging
{
    /// <summary>
    /// Implementação real do serviço de mensageria OCI Streaming
    /// Implementação completa para produção com Oracle OCI SDK oficial
    /// </summary>
    public class OciStreamingMessagingService : IMessagingService
    {
        private readonly ILogger<OciStreamingMessagingService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string? _compartmentId;
        private readonly string? _streamOcid;
        private readonly string? _consumerGroup;
        private readonly string? _partitionKey;
        private readonly string? _streamingEndpoint;
        private readonly string? _tenancyId;
        private readonly string? _userId;
        private readonly string? _fingerprint;
        private readonly string? _privateKeyPath;
        private readonly Lazy<StreamClient> _streamClient;
        private readonly SmartAlarmMeter _meter;
        private readonly ActivitySource _activitySource;

        public OciStreamingMessagingService(
            ILogger<OciStreamingMessagingService> logger,
            IConfiguration configuration,
            SmartAlarmMeter meter,
            ActivitySource activitySource,
            string? compartmentId = null,
            string? streamOcid = null,
            string? consumerGroup = null,
            string? partitionKey = null,
            string? streamingEndpoint = null,
            string? tenancyId = null,
            string? userId = null,
            string? fingerprint = null,
            string? privateKeyPath = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _meter = meter ?? throw new ArgumentNullException(nameof(meter));
            _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));

            _compartmentId = compartmentId ?? _configuration["OCI:CompartmentId"];
            _streamOcid = streamOcid ?? _configuration["OCI:StreamOcid"];
            _consumerGroup = consumerGroup ?? _configuration["OCI:ConsumerGroup"] ?? "default-group";
            _partitionKey = partitionKey ?? _configuration["OCI:PartitionKey"] ?? "default-partition";
            _streamingEndpoint = streamingEndpoint ?? _configuration["OCI:StreamingEndpoint"];
            _tenancyId = tenancyId ?? _configuration["OCI:TenancyId"];
            _userId = userId ?? _configuration["OCI:UserId"];
            _fingerprint = fingerprint ?? _configuration["OCI:Fingerprint"];
            _privateKeyPath = privateKeyPath ?? _configuration["OCI:PrivateKeyPath"];

            ValidateConfiguration();

            // Lazy initialization do cliente OCI Streaming
            _streamClient = new Lazy<StreamClient>(() => CreateStreamClient());

            _logger.LogInformation("OciStreamingMessagingService inicializado com endpoint {StreamingEndpoint} e stream {StreamOcid}",
                _streamingEndpoint, _streamOcid);
        }

        private StreamClient CreateStreamClient()
        {
            try
            {
                var authProvider = CreateAuthenticationProvider();
                var streamClient = new StreamClient(authProvider);
                streamClient.SetEndpoint(_streamingEndpoint);
                return streamClient;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar cliente OCI Streaming");
                throw new InvalidOperationException("Erro ao inicializar cliente OCI Streaming", ex);
            }
        }

        private IAuthenticationDetailsProvider CreateAuthenticationProvider()
        {
            try
            {
                var configFileProvider = new ConfigFileAuthenticationDetailsProvider("DEFAULT");
                return configFileProvider;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar provedor de autenticação OCI");
                throw new InvalidOperationException("Erro ao configurar autenticação OCI", ex);
            }
        }

        private void ValidateConfiguration()
        {
            if (string.IsNullOrEmpty(_compartmentId))
                throw new ArgumentException("CompartmentId não configurado");
            if (string.IsNullOrEmpty(_streamOcid))
                throw new ArgumentException("StreamOcid não configurado");
            if (string.IsNullOrEmpty(_streamingEndpoint))
                throw new ArgumentException("StreamingEndpoint não configurado");
        }

        public async Task PublishEventAsync(string topic, string message)
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                nameof(PublishEventAsync), topic);

            using var activity = _activitySource.StartActivity($"PublishEvent-{topic}");
            activity?.SetTag("topic", topic);
            activity?.SetTag("service", "oci-streaming");

            try
            {
                var putMessagesRequest = new PutMessagesRequest
                {
                    StreamId = _streamOcid,
                    PutMessagesDetails = new PutMessagesDetails
                    {
                        Messages = new List<PutMessagesDetailsEntry>
                        {
                            new PutMessagesDetailsEntry
                            {
                                Key = Encoding.UTF8.GetBytes($"{topic}:{_partitionKey}"),
                                Value = Encoding.UTF8.GetBytes(message)
                            }
                        }
                    }
                };

                activity?.SetTag("streamId", _streamOcid);
                activity?.SetTag("partitionKey", _partitionKey);

                _logger.LogInformation("Calling OCI Streaming PublishEvent for topic {Topic}", topic);

                var response = await _streamClient.Value.PutMessages(putMessagesRequest);

                stopwatch.Stop();

                activity?.SetStatus(ActivityStatusCode.Ok);
                activity?.SetTag("response.status", "success");

                _logger.LogInformation("Successfully published event to OCI Streaming topic {Topic}", topic);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.SetTag("error", ex.Message);

                _logger.LogError(ex, "OCI Streaming PublishEvent failed: {Error}", ex.Message);

                throw new InvalidOperationException($"Erro ao publicar evento no tópico {topic}", ex);
            }
        }

        public async Task SubscribeAsync(string topic, Func<string, Task> handler)
        {
            _logger.LogInformation("Iniciando subscription para tópico {Topic} no OCI Streaming", topic);

            using var activity = _activitySource.StartActivity($"Subscribe-{topic}");
            activity?.SetTag("topic", topic);
            activity?.SetTag("service", "oci-streaming");

            try
            {
                // Criar cursor para o grupo de consumidores
                var createCursorRequest = new CreateGroupCursorRequest
                {
                    StreamId = _streamOcid,
                    CreateGroupCursorDetails = new CreateGroupCursorDetails
                    {
                        GroupName = _consumerGroup,
                        Type = CreateGroupCursorDetails.TypeEnum.TrimHorizon
                    }
                };

                var cursor = await _streamClient.Value.CreateGroupCursor(createCursorRequest);

                // Loop de consumo
                while (true)
                {
                var getMessagesRequest = new GetMessagesRequest
                {
                    StreamId = _streamOcid,
                    Cursor = cursor.Cursor.Value,
                    Limit = 10
                };                    var messagesResponse = await _streamClient.Value.GetMessages(getMessagesRequest);

                    foreach (var message in messagesResponse.Items)
                    {
                        try
                        {
                            var messageContent = Encoding.UTF8.GetString(message.Value);
                            await handler(messageContent);

                            _logger.LogDebug("Mensagem processada com sucesso para tópico {Topic}", topic);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Erro ao processar mensagem do tópico {Topic}", topic);
                        }
                    }

                    // Aguardar antes da próxima poll
                    await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.SetTag("error", ex.Message);

                _logger.LogError(ex, "Erro ao fazer subscription do tópico {Topic}", topic);
                throw new InvalidOperationException($"Erro ao fazer subscription do tópico {topic}", ex);
            }
        }

        public void Dispose()
        {
            if (_streamClient?.IsValueCreated == true)
            {
                _streamClient.Value?.Dispose();
            }
        }
    }
}
