using System;

namespace SmartAlarm.Domain.Exceptions
{
    /// <summary>
    /// Exceção para entidades não encontradas no domínio.
    /// </summary>
    public class NotFoundException : DomainException
    {
        public NotFoundException(string entity, object key)
            : base($"Entidade '{entity}' com chave '{key}' não encontrada.")
        {
        }
    }
}
