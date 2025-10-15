using System;

namespace SmartAlarm.Infrastructure.Exceptions
{
    /// <summary>
    /// Exceção base para erros de storage
    /// </summary>
    public class StorageException : Exception
    {
        public StorageException(string message) : base(message) { }
        public StorageException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exceção lançada quando o serviço de storage está temporariamente indisponível
    /// </summary>
    public class StorageUnavailableException : StorageException
    {
        public StorageUnavailableException(string message) : base(message) { }
        public StorageUnavailableException(string message, Exception innerException) : base(message, innerException) { }
    }
}