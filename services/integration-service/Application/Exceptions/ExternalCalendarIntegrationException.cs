using System;

namespace SmartAlarm.IntegrationService.Application.Exceptions
{
    /// <summary>
    /// Exception lançada quando há falhas na integração com calendários externos
    /// </summary>
    public class ExternalCalendarIntegrationException : Exception
    {
        public string Provider { get; }
        public string? CalendarId { get; }
        public bool IsRetryable { get; }

        public ExternalCalendarIntegrationException(
            string provider,
            string message,
            bool isRetryable = false,
            string? calendarId = null,
            Exception? innerException = null)
            : base($"[{provider}] Calendar integration failed: {message}", innerException)
        {
            Provider = provider;
            CalendarId = calendarId;
            IsRetryable = isRetryable;
        }
    }

    /// <summary>
    /// Exception lançada quando há falhas temporárias que podem ser resolvidas com retry
    /// </summary>
    public class ExternalCalendarTemporaryException : ExternalCalendarIntegrationException
    {
        public ExternalCalendarTemporaryException(
            string provider,
            string message,
            string? calendarId = null,
            Exception? innerException = null)
            : base(provider, message, isRetryable: true, calendarId, innerException)
        {
        }
    }

    /// <summary>
    /// Exception lançada quando há falhas permanentes que não devem ser retryadas
    /// </summary>
    public class ExternalCalendarPermanentException : ExternalCalendarIntegrationException
    {
        public ExternalCalendarPermanentException(
            string provider,
            string message,
            string? calendarId = null,
            Exception? innerException = null)
            : base(provider, message, isRetryable: false, calendarId, innerException)
        {
        }
    }
}
