using System.Runtime.Serialization;

namespace SmartAlarm.IntegrationService.Application.Exceptions
{
    /// <summary>
    /// Exceção para falhas em serviços externos (APIs de terceiros)
    /// </summary>
    [Serializable]
    public class ExternalServiceException : Exception
    {
        public string? ServiceName { get; }
        public string? ErrorCode { get; }
        public int? HttpStatusCode { get; }

        public ExternalServiceException() : base("Falha em serviço externo")
        {
        }

        public ExternalServiceException(string message) : base(message)
        {
        }

        public ExternalServiceException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public ExternalServiceException(string serviceName, string message, Exception innerException) : base(message, innerException)
        {
            ServiceName = serviceName;
        }

        public ExternalServiceException(string serviceName, string errorCode, string message, int? httpStatusCode = null) : base(message)
        {
            ServiceName = serviceName;
            ErrorCode = errorCode;
            HttpStatusCode = httpStatusCode;
        }

        public ExternalServiceException(string serviceName, string errorCode, string message, Exception innerException, int? httpStatusCode = null) : base(message, innerException)
        {
            ServiceName = serviceName;
            ErrorCode = errorCode;
            HttpStatusCode = httpStatusCode;
        }

        protected ExternalServiceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            ServiceName = info.GetString(nameof(ServiceName));
            ErrorCode = info.GetString(nameof(ErrorCode));
            HttpStatusCode = info.GetInt32(nameof(HttpStatusCode));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(ServiceName), ServiceName);
            info.AddValue(nameof(ErrorCode), ErrorCode);
            info.AddValue(nameof(HttpStatusCode), HttpStatusCode);
        }
    }
}
