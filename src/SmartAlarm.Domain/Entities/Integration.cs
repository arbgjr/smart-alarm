using System;

namespace SmartAlarm.Domain.Entities
{
    /// <summary>
    /// Representa uma integração externa configurada para um alarme.
    /// </summary>
    public class Integration
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public string Provider { get; private set; }
        public string Configuration { get; private set; }

        public Integration(Guid id, string name, string provider, string configuration)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Nome da integração é obrigatório.", nameof(name));
            if (string.IsNullOrWhiteSpace(provider))
                throw new ArgumentException("Provedor da integração é obrigatório.", nameof(provider));
            Id = id == Guid.Empty ? Guid.NewGuid() : id;
            Name = name;
            Provider = provider;
            Configuration = configuration;
        }
    }
}
