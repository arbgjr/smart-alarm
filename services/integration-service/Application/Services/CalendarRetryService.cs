using Microsoft.Extensions.Logging;
using SmartAlarm.IntegrationService.Application.Exceptions;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace SmartAlarm.IntegrationService.Application.Services
{
    /// <summary>
    /// Configuração para retry policy
    /// </summary>
    public record RetryPolicy(
        int MaxAttempts = 3,
        TimeSpan InitialDelay = default,
        double BackoffMultiplier = 2.0,
        TimeSpan MaxDelay = default
    )
    {
        public RetryPolicy() : this(3, TimeSpan.FromSeconds(1), 2.0, TimeSpan.FromSeconds(30))
        {
        }
    }

    /// <summary>
    /// Serviço para implementar retry logic nas integrações de calendário
    /// </summary>
    public interface ICalendarRetryService
    {
        Task<T> ExecuteWithRetryAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            string operationName,
            string provider,
            RetryPolicy? policy = null,
            CancellationToken cancellationToken = default);
    }

    public class CalendarRetryService : ICalendarRetryService
    {
        private readonly ILogger<CalendarRetryService> _logger;
        private readonly RetryPolicy _defaultPolicy;

        public CalendarRetryService(ILogger<CalendarRetryService> logger)
        {
            _logger = logger;
            _defaultPolicy = new RetryPolicy();
        }

        public async Task<T> ExecuteWithRetryAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            string operationName,
            string provider,
            RetryPolicy? policy = null,
            CancellationToken cancellationToken = default)
        {
            var retryPolicy = policy ?? _defaultPolicy;
            var attempt = 0;
            Exception? lastException = null;

            while (attempt < retryPolicy.MaxAttempts)
            {
                attempt++;

                try
                {
                    _logger.LogDebug("Executing {OperationName} for {Provider}, attempt {Attempt}/{MaxAttempts}",
                        operationName, provider, attempt, retryPolicy.MaxAttempts);

                    var result = await operation(cancellationToken);
                    
                    if (attempt > 1)
                    {
                        _logger.LogInformation("Operation {OperationName} for {Provider} succeeded on attempt {Attempt}",
                            operationName, provider, attempt);
                    }

                    return result;
                }
                catch (Exception ex) when (attempt < retryPolicy.MaxAttempts && IsRetryableException(ex))
                {
                    lastException = ex;
                    var delay = CalculateDelay(attempt, retryPolicy);

                    _logger.LogWarning(ex,
                        "Operation {OperationName} for {Provider} failed on attempt {Attempt}/{MaxAttempts}. " +
                        "Retrying in {DelayMs}ms. Error: {ErrorMessage}",
                        operationName, provider, attempt, retryPolicy.MaxAttempts, delay.TotalMilliseconds, ex.Message);

                    await Task.Delay(delay, cancellationToken);
                    continue;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Operation {OperationName} for {Provider} failed permanently on attempt {Attempt}. Error: {ErrorMessage}",
                        operationName, provider, attempt, ex.Message);
                    throw;
                }
            }

            _logger.LogError(lastException,
                "Operation {OperationName} for {Provider} failed after {MaxAttempts} attempts",
                operationName, provider, retryPolicy.MaxAttempts);

            throw new ExternalCalendarTemporaryException(
                provider,
                $"Operation {operationName} failed after {retryPolicy.MaxAttempts} retry attempts",
                innerException: lastException);
        }

        private static bool IsRetryableException(Exception exception)
        {
            return exception switch
            {
                ExternalCalendarIntegrationException calendarEx => calendarEx.IsRetryable,
                TaskCanceledException => false, // Timeout ou cancellation não deve ser retryado
                HttpRequestException httpEx => IsRetryableHttpException(httpEx),
                _ => false // Por padrão, não retry exceções desconhecidas
            };
        }

        private static bool IsRetryableHttpException(HttpRequestException httpException)
        {
            // Retry para problemas de rede temporários
            var message = httpException.Message.ToLowerInvariant();
            return message.Contains("timeout") ||
                   message.Contains("connection") ||
                   message.Contains("network") ||
                   message.Contains("socket");
        }

        private static TimeSpan CalculateDelay(int attempt, RetryPolicy policy)
        {
            var delay = TimeSpan.FromTicks((long)(policy.InitialDelay.Ticks * Math.Pow(policy.BackoffMultiplier, attempt - 1)));
            return delay > policy.MaxDelay ? policy.MaxDelay : delay;
        }
    }
}
