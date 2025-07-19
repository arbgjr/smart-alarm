using System;
using System.Collections.Generic;

namespace SmartAlarm.IntegrationService.Application.Commands
{
    /// <summary>
    /// Resultado de uma operação de fetch de calendário externo
    /// </summary>
    public class CalendarFetchResult
    {
        public bool IsSuccess { get; }
        public List<ExternalCalendarEvent> Events { get; }
        public CalendarFetchError? Error { get; }
        public int RetryAttempts { get; }

        private CalendarFetchResult(
            bool isSuccess,
            List<ExternalCalendarEvent> events,
            CalendarFetchError? error = null,
            int retryAttempts = 0)
        {
            IsSuccess = isSuccess;
            Events = events;
            Error = error;
            RetryAttempts = retryAttempts;
        }

        public static CalendarFetchResult Success(List<ExternalCalendarEvent> events, int retryAttempts = 0)
            => new(true, events, retryAttempts: retryAttempts);

        public static CalendarFetchResult Failure(
            CalendarFetchError error,
            int retryAttempts = 0)
            => new(false, new List<ExternalCalendarEvent>(), error, retryAttempts);
    }

    /// <summary>
    /// Informações sobre erro durante fetch de calendário externo
    /// </summary>
    public record CalendarFetchError(
        string Provider,
        string ErrorCode,
        string Message,
        bool IsRetryable,
        DateTime OccurredAt,
        Exception? OriginalException = null,
        string? CalendarId = null
    );
}
