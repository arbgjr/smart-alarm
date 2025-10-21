using Hangfire;
using Hangfire.Storage;
using System.Collections.Concurrent;
using System.Text.Json;

namespace SmartAlarm.AlarmService.Infrastructure.Queues
{
    /// <summary>
    /// Implementação de fila de alarmes usando Hangfire como backend
    /// </summary>
    public class HangfireAlarmQueue : IAlarmQueue
    {
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly JobStorage _jobStorage;
        private readonly ILogger<HangfireAlarmQueue> _logger;

        // Cache para estatísticas (em produção, usar Redis)
        private readonly ConcurrentDictionary<string, QueueStatistics> _statisticsCache = new();
        private readonly ConcurrentDictionary<string, AlarmQueueItem> _processingItems = new();
        private readonly QueueConfiguration _defaultConfig;

        public HangfireAlarmQueue(
            IBackgroundJobClient backgroundJobClient,
            JobStorage jobStorage,
            ILogger<HangfireAlarmQueue> logger,
            IConfiguration configuration)
        {
            _backgroundJobClient = backgroundJobClient;
            _jobStorage = jobStorage;
            _logger = logger;

            _defaultConfig = new QueueConfiguration(
                Name: "alarm-processing",
                MaxRetries: configuration.GetValue<int>("AlarmQueue:MaxRetries", 3),
                RetryDelay: TimeSpan.FromSeconds(configuration.GetValue<int>("AlarmQueue:RetryDelaySeconds", 30)),
                MessageTtl: TimeSpan.FromHours(configuration.GetValue<int>("AlarmQueue:MessageTtlHours", 24)),
                EnableDeadLetter: configuration.GetValue<bool>("AlarmQueue:EnableDeadLetter", true),
                DeadLetterQueue: "alarm-processing-dlq"
            );
        }

        /// <inheritdoc />
        public async Task<string> EnqueueAlarmAsync(
            AlarmQueueItem item,
            TimeSpan? delay = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Enfileirando alarme {AlarmId} para processamento - Delay: {Delay}",
                    item.AlarmId, delay?.ToString() ?? "none");

                // Serializar item para JSON
                var itemJson = JsonSerializer.Serialize(item);

                string jobId;

                if (delay.HasValue && delay.Value > TimeSpan.Zero)
                {
                    // Agendar para execução futura
                    jobId = _backgroundJobClient.Schedule(
                        () => ProcessQueuedAlarmAsync(itemJson),
                        delay.Value);
                }
                else
                {
                    // Enfileirar para execução imediata
                    jobId = _backgroundJobClient.Enqueue(
                        () => ProcessQueuedAlarmAsync(itemJson));
                }

                // Atualizar estatísticas
                await UpdateQueueStatisticsAsync(_defaultConfig.Name, 1, 0, 0);

                _logger.LogInformation("Alarme {AlarmId} enfileirado com sucesso - JobId: {JobId}",
                    item.AlarmId, jobId);

                return jobId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enfileirar alarme {AlarmId}", item.AlarmId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<AlarmQueueItem?> DequeueAlarmAsync(
            string queueName = "default",
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Hangfire gerencia automaticamente o dequeue através dos workers
                // Este método é mais para compatibilidade com a interface

                _logger.LogDebug("Dequeue solicitado para fila {QueueName}", queueName);

                // Em uma implementação real com RabbitMQ ou similar,
                // aqui faria o dequeue manual
                await Task.CompletedTask;

                return null; // Hangfire gerencia automaticamente
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fazer dequeue da fila {QueueName}", queueName);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task AcknowledgeAsync(
            string messageId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Confirmando processamento da mensagem {MessageId}", messageId);

                // Remover do cache de processamento
                _processingItems.TryRemove(messageId, out _);

                // Atualizar estatísticas
                await UpdateQueueStatisticsAsync(_defaultConfig.Name, 0, -1, 0);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao confirmar mensagem {MessageId}", messageId);
            }
        }

        /// <inheritdoc />
        public async Task RejectAsync(
            string messageId,
            bool requeue = true,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogWarning("Rejeitando mensagem {MessageId} - Requeue: {Requeue}",
                    messageId, requeue);

                if (_processingItems.TryGetValue(messageId, out var item))
                {
                    if (requeue && item.RetryCount < _defaultConfig.MaxRetries)
                    {
                        // Criar novo item com retry count incrementado
                        var retryItem = item with
                        {
                            RetryCount = item.RetryCount + 1,
                            Id = Guid.NewGuid().ToString()
                        };

                        // Calcular delay exponencial
                        var retryDelay = TimeSpan.FromSeconds(
                            _defaultConfig.RetryDelay.TotalSeconds * Math.Pow(2, item.RetryCount));

                        // Reenfileirar com delay
                        await EnqueueAlarmAsync(retryItem, retryDelay, cancellationToken);

                        _logger.LogInformation("Alarme {AlarmId} reenfileirado para retry {RetryCount}/{MaxRetries} com delay de {Delay}",
                            item.AlarmId, retryItem.RetryCount, _defaultConfig.MaxRetries, retryDelay);
                    }
                    else
                    {
                        // Enviar para dead letter queue se habilitado
                        if (_defaultConfig.EnableDeadLetter)
                        {
                            await SendToDeadLetterQueueAsync(item, cancellationToken);
                        }

                        _logger.LogError("Alarme {AlarmId} enviado para dead letter queue após {RetryCount} tentativas",
                            item.AlarmId, item.RetryCount);
                    }

                    _processingItems.TryRemove(messageId, out _);
                }

                // Atualizar estatísticas
                await UpdateQueueStatisticsAsync(_defaultConfig.Name, 0, -1, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao rejeitar mensagem {MessageId}", messageId);
            }
        }

        /// <inheritdoc />
        public async Task<QueueStatistics> GetQueueStatisticsAsync(
            string queueName = "default",
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Obter estatísticas do Hangfire
                using var connection = _jobStorage.GetConnection();
                var monitoring = _jobStorage.GetMonitoringApi();

                var enqueuedCount = monitoring.EnqueuedCount(queueName);
                var processingCount = monitoring.ProcessingCount();
                var failedCount = monitoring.FailedCount();

                // Calcular throughput (simplificado)
                var throughput = CalculateThroughput(queueName);

                var statistics = new QueueStatistics(
                    QueueName: queueName,
                    TotalMessages: (int)(enqueuedCount + processingCount + failedCount),
                    PendingMessages: (int)enqueuedCount,
                    ProcessingMessages: (int)processingCount,
                    FailedMessages: (int)failedCount,
                    LastActivity: DateTime.UtcNow,
                    AverageProcessingTime: TimeSpan.FromSeconds(30), // Estimativa
                    ThroughputPerMinute: throughput
                );

                _statisticsCache[queueName] = statistics;

                await Task.CompletedTask;
                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter estatísticas da fila {QueueName}", queueName);

                // Retornar estatísticas em cache ou padrão
                return _statisticsCache.GetValueOrDefault(queueName, new QueueStatistics(
                    queueName, 0, 0, 0, 0, DateTime.UtcNow, TimeSpan.Zero, 0.0));
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<AlarmQueueItem>> ListQueuedAlarmsAsync(
            string queueName = "default",
            int limit = 100,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var items = new List<AlarmQueueItem>();

                // Obter jobs enfileirados do Hangfire
                using var connection = _jobStorage.GetConnection();
                var monitoring = _jobStorage.GetMonitoringApi();

                var enqueuedJobs = monitoring.EnqueuedJobs(queueName, 0, limit);

                foreach (var job in enqueuedJobs)
                {
                    try
                    {
                        // Tentar extrair informações do job
                        if (job.Value?.Job?.Args?.FirstOrDefault() is string itemJson)
                        {
                            var item = JsonSerializer.Deserialize<AlarmQueueItem>(itemJson);
                            if (item != null)
                            {
                                items.Add(item);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erro ao deserializar job {JobId}", job.Key);
                    }
                }

                await Task.CompletedTask;
                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar alarmes da fila {QueueName}", queueName);
                return Array.Empty<AlarmQueueItem>();
            }
        }

        /// <inheritdoc />
        public async Task<int> PurgeQueueAsync(
            string queueName = "default",
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogWarning("Purgando fila {QueueName}", queueName);

                var purgedCount = 0;

                // Obter e deletar jobs enfileirados
                using var connection = _jobStorage.GetConnection();
                var monitoring = _jobStorage.GetMonitoringApi();

                var enqueuedJobs = monitoring.EnqueuedJobs(queueName, 0, 1000);

                foreach (var job in enqueuedJobs)
                {
                    try
                    {
                        _backgroundJobClient.Delete(job.Key);
                        purgedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erro ao deletar job {JobId} durante purge", job.Key);
                    }
                }

                // Resetar estatísticas
                _statisticsCache[queueName] = new QueueStatistics(
                    queueName, 0, 0, 0, 0, DateTime.UtcNow, TimeSpan.Zero, 0.0);

                _logger.LogInformation("Fila {QueueName} purgada - {PurgedCount} jobs removidos",
                    queueName, purgedCount);

                await Task.CompletedTask;
                return purgedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao purgar fila {QueueName}", queueName);
                return 0;
            }
        }

        #region Métodos Privados

        /// <summary>
        /// Método executado pelo Hangfire para processar alarme da fila
        /// </summary>
        [Queue("alarm-processing")]
        public async Task ProcessQueuedAlarmAsync(string itemJson)
        {
            AlarmQueueItem? item = null;

            try
            {
                // Deserializar item
                item = JsonSerializer.Deserialize<AlarmQueueItem>(itemJson);
                if (item == null)
                {
                    _logger.LogError("Erro ao deserializar item da fila: {ItemJson}", itemJson);
                    return;
                }

                _logger.LogInformation("Processando alarme {AlarmId} da fila - Retry: {RetryCount}",
                    item.AlarmId, item.RetryCount);

                // Adicionar ao cache de processamento
                _processingItems[item.Id] = item;

                // Atualizar estatísticas
                await UpdateQueueStatisticsAsync(_defaultConfig.Name, 0, 1, 0);

                // Aqui seria chamado o processador distribuído real
                // Por simplicidade, simular processamento
                await SimulateAlarmProcessingAsync(item);

                // Confirmar processamento
                await AcknowledgeAsync(item.Id);

                _logger.LogInformation("Alarme {AlarmId} processado com sucesso da fila", item.AlarmId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar alarme {AlarmId} da fila",
                    item?.AlarmId ?? Guid.Empty);

                if (item != null)
                {
                    await RejectAsync(item.Id, requeue: true);
                }
            }
        }

        private async Task SimulateAlarmProcessingAsync(AlarmQueueItem item)
        {
            // Simular processamento do alarme
            await Task.Delay(Random.Shared.Next(100, 500)); // 100-500ms

            // Simular falha ocasional para testar retry
            if (Random.Shared.NextDouble() < 0.1) // 10% de chance de falha
            {
                throw new InvalidOperationException("Falha simulada no processamento");
            }

            _logger.LogDebug("Alarme {AlarmId} processado com sucesso (simulação)", item.AlarmId);
        }

        private async Task SendToDeadLetterQueueAsync(AlarmQueueItem item, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(_defaultConfig.DeadLetterQueue))
                    return;

                // Criar item para dead letter queue
                var dlqItem = item with
                {
                    Id = Guid.NewGuid().ToString(),
                    Metadata = new Dictionary<string, object>(item.Metadata ?? new())
                    {
                        ["original_queue"] = _defaultConfig.Name,
                        ["failed_at"] = DateTime.UtcNow,
                        ["final_retry_count"] = item.RetryCount
                    }
                };

                // Enfileirar na DLQ (sem delay)
                var serializedItem = JsonSerializer.Serialize(dlqItem);
                var dlqJobId = _backgroundJobClient.Enqueue(
                    () => ProcessDeadLetterAlarmAsync(serializedItem));

                _logger.LogWarning("Alarme {AlarmId} enviado para dead letter queue - JobId: {JobId}",
                    item.AlarmId, dlqJobId);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar alarme {AlarmId} para dead letter queue", item.AlarmId);
            }
        }

        [Queue("alarm-processing-dlq")]
        public async Task ProcessDeadLetterAlarmAsync(string itemJson)
        {
            try
            {
                var item = JsonSerializer.Deserialize<AlarmQueueItem>(itemJson);
                if (item == null) return;

                _logger.LogError("Processando alarme {AlarmId} na dead letter queue - Falha permanente após {RetryCount} tentativas",
                    item.AlarmId, item.RetryCount);

                // Aqui poderia notificar administradores, salvar em log especial, etc.
                await Task.Delay(100); // Simular processamento de DLQ
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar item da dead letter queue");
            }
        }

        private async Task UpdateQueueStatisticsAsync(string queueName, int totalDelta, int processingDelta, int failedDelta)
        {
            try
            {
                if (_statisticsCache.TryGetValue(queueName, out var current))
                {
                    var updated = current with
                    {
                        TotalMessages = Math.Max(0, current.TotalMessages + totalDelta),
                        ProcessingMessages = Math.Max(0, current.ProcessingMessages + processingDelta),
                        FailedMessages = Math.Max(0, current.FailedMessages + failedDelta),
                        LastActivity = DateTime.UtcNow
                    };
                    _statisticsCache[queueName] = updated;
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao atualizar estatísticas da fila {QueueName}", queueName);
            }
        }

        private double CalculateThroughput(string queueName)
        {
            // Implementação simplificada - em produção usaria métricas históricas
            return Random.Shared.NextDouble() * 100; // 0-100 alarmes por minuto
        }

        #endregion
    }
}
