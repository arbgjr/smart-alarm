using System;
using System.Text.Json;
using SmartAlarm.Domain.ValueObjects;

namespace SmartAlarm.Domain.Entities
{
    /// <summary>
    /// Representa uma integração externa configurada para um alarme.
    /// </summary>
    public class Integration
    {
        public Guid Id { get; private set; }
        public Name Name { get; private set; }
        public string Provider { get; private set; }
        public string Configuration { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? LastExecutedAt { get; private set; }

        public Integration(Guid id, Name name, string provider, string configuration)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrWhiteSpace(provider))
                throw new ArgumentException("Provedor da integração é obrigatório.", nameof(provider));
            
            ValidateConfiguration(configuration);
            
            Id = id == Guid.Empty ? Guid.NewGuid() : id;
            Name = name;
            Provider = provider;
            Configuration = configuration;
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
        }

        // Constructor for string parameters for backward compatibility
        public Integration(Guid id, string name, string provider, string configuration)
            : this(id, new Name(name), provider, configuration)
        {
        }

        public void Activate() => IsActive = true;
        public void Deactivate() => IsActive = false;

        public void UpdateName(Name newName)
        {
            Name = newName ?? throw new ArgumentNullException(nameof(newName));
        }

        public void UpdateConfiguration(string newConfiguration)
        {
            ValidateConfiguration(newConfiguration);
            Configuration = newConfiguration;
        }

        public void RecordExecution()
        {
            if (!IsActive)
                throw new InvalidOperationException("Não é possível executar uma integração inativa.");
            
            LastExecutedAt = DateTime.UtcNow;
        }

        private static void ValidateConfiguration(string configuration)
        {
            if (string.IsNullOrWhiteSpace(configuration))
                return; // Configuration can be empty
            
            try
            {
                JsonDocument.Parse(configuration);
            }
            catch (JsonException)
            {
                throw new ArgumentException("Configuração deve ser um JSON válido.", nameof(configuration));
            }
        }

        public T GetConfigurationValue<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(Configuration))
                return default(T);

            try
            {
                var doc = JsonDocument.Parse(Configuration);
                if (doc.RootElement.TryGetProperty(key, out var element))
                {
                    return JsonSerializer.Deserialize<T>(element.GetRawText());
                }
            }
            catch (JsonException)
            {
                // Configuration is not valid JSON
            }

            return default(T);
        }
    }
}
