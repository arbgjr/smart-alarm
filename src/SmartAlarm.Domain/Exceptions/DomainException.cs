using System;

namespace SmartAlarm.Domain.Exceptions
{
    /// <summary>
    /// Exceção de domínio para regras de negócio violadas.
    /// </summary>
    public class DomainException : Exception
    {
        public DomainException(string message) : base(message) { }
        public DomainException(string message, Exception innerException) : base(message, innerException) { }
    }
}
