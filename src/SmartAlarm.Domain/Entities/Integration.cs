using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmartAlarm.Domain.ValueObjects;

namespace SmartAlarm.Domain.Entities
{
    /// <summary>
    /// Representa uma integraÃ§Ã£o externa configurada para um alarme.
    /// </summary>
    public class Integration
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public Name Name { get; private set; } = null!;
        public string Provider { get; private set; } = null!;
        public string Configuration { get; private set; } = null!;
        public bool IsActive { get; private set; }
        public bool IsEnabled { get; private set; }
        public Guid? AlarmId { get; private set; }
        public string? AccessToken { get; private set; }
        public string? RefreshToken { get; private set; }
        public DateTime? TokenExpiresAt { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? LastExecutedAt { get; private set; }
        public DateTime? LastSyncedAt { get; private set; }

        // Private constructor for EF Core
        private Integration() { }

        // JSON constructor for deserialization
        [JsonConstructor]
        public Integration(Guid id, Name name, string provider, string configuration, bool isActive, Guid alarmId, DateTime createdAt, DateTime? lastExecutedAt)
        {
            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Configuration = configuration ?? string.Empty;
            IsActive = isActive;
            AlarmId = alarmId;
            CreatedAt = createdAt;
            LastExecutedAt = lastExecutedAt;
        }

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
            IsEnabled = true;
            CreatedAt = DateTime.UtcNow;
        }

        // Constructor for user-level integrations (like Google Calendar)
        public Integration(Guid id, Guid userId, string provider, string name, Dictionary<string, string> configuration)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId é obrigatório.", nameof(userId));
            if (string.IsNullOrWhiteSpace(provider))
                throw new ArgumentException("Provider é obrigatório.", nameof(provider));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name é obrigatório.", nameof(name));

            Id = id == Guid.Empty ? Guid.NewGuid() : id;
            UserId = userId;
            Provider = provider;
            Name = new Name(name);
            Configuration = JsonSerializer.Serialize(configuration ?? new Dictionary<string, string>());
            IsActive = true;
            IsEnabled = true;
            CreatedAt = DateTime.UtcNow;
        }

        // Constructor for string parameters for backward compatibility
        public Integration(Guid id, string name, string provider, string configuration, Guid alarmId)
            : this(id, new Name(name), provider, configuration, alarmId)
        {
        }

        public void Activate() => IsActive = true;
        public void Deactivate() => IsActive = false;
        public void Enable() => IsEnabled = true;
        public void Disable() => IsEnabled = false;
        
        public void UpdateAccessToken(string accessToken, string? refreshToken = null, DateTime? expiresAt = null)
        {
            AccessToken = accessToken;
            if (refreshToken != null)
                RefreshToken = refreshToken;
            if (expiresAt != null)
                TokenExpiresAt = expiresAt;
            LastSyncedAt = DateTime.UtcNow;
        }

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

        public T? GetConfigurationValue<T>(string key)
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

