using System;
using System.Text.Json;
using SmartAlarm.Domain.ValueObjects;

namespace SmartAlarm.Domain.Entities
{
    /// <summary>
    /// Representa uma integraÃ§Ã£o externa configurada para um alarme.
    /// </summary>
    public class Integration
    {
        public Guid Id { get; private set; }
        public Name Name { get; private set; }
        public string Provider { get; private set; }
        public string Configuration { get; private set; }
        public bool IsActive { get; private set; }
        public Guid AlarmId { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? LastExecutedAt { get; private set; }

        // Private constructor for EF Core
        private Integration() { }

        public Integration(Guid id, Name name, string provider, string configuration, Guid alarmId)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrWhiteSpace(provider))
                throw new ArgumentException("Provedor da integraÃ§Ã£o Ã© obrigatÃ³rio.", nameof(provider));
            if (alarmId == Guid.Empty)
                throw new ArgumentException("AlarmId Ã© obrigatÃ³rio.", nameof(alarmId));
            
            ValidateConfiguration(configuration);
            
            Id = id == Guid.Empty ? Guid.NewGuid() : id;
            Name = name;
            Provider = provider;
            Configuration = configuration;
            AlarmId = alarmId;
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
        }

        // Constructor for string parameters for backward compatibility
        public Integration(Guid id, string name, string provider, string configuration, Guid alarmId)
            : this(id, new Name(name), provider, configuration, alarmId)
        {
        }

        // Legacy constructor - deprecated, will cause compilation errors for old usage
        [Obsolete("Use constructor with AlarmId parameter", true)]
        public Integration(Guid id, Name name, string provider, string configuration)
        {
            throw new NotSupportedException("Use constructor with AlarmId parameter");
        }

        // Legacy constructor - deprecated, will cause compilation errors for old usage  
        [Obsolete("Use constructor with AlarmId parameter", true)]
        public Integration(Guid id, string name, string provider, string configuration)
        {
            throw new NotSupportedException("Use constructor with AlarmId parameter");
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
                throw new InvalidOperationException("NÃ£o Ã© possÃ­vel executar uma integraÃ§Ã£o inativa.");
            
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
                throw new ArgumentException("ConfiguraÃ§Ã£o deve ser um JSON vÃ¡lido.", nameof(configuration));
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

