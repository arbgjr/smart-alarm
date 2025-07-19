using System;

namespace SmartAlarm.Domain.Exceptions
{
    /// <summary>
    /// Exceção para validação de regras de negócio no domínio.
    /// </summary>
    public class ValidationException : DomainException
    {
        public ValidationException(string message) : base(message) { }
    }
}
