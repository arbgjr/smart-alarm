using System;
using System.Collections.Generic;
using SmartAlarm.Domain.ValueObjects;

namespace SmartAlarm.Domain.Entities
{
    /// <summary>
    /// Representa um usuário do sistema Smart Alarm.
    /// Expandido para suportar autenticação JWT/FIDO2
    /// </summary>
    public class User
    {
        public Guid Id { get; private set; }
        public Name Name { get; private set; }
        public Email Email { get; private set; }
        public string PasswordHash { get; private set; } = string.Empty;
        public bool IsActive { get; private set; }
        public bool EmailVerified { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        public DateTime? LastLoginAt { get; private set; }

        // Location information for holiday detection
        public string? Country { get; private set; }
        public string? State { get; private set; }
        public string? City { get; private set; }
        public string? TimeZone { get; private set; }

        // OAuth2 external provider support
        public string? ExternalProviderId { get; private set; }
        public string? ExternalProvider { get; private set; }

        // LGPD/GDPR compliance fields
        public bool MarkedForDeletion { get; set; }
        public DateTime? DeletionRequestedAt { get; set; }
        public string? DeletionReason { get; set; }
        public bool IsAnonymized { get; set; }
        public DateTime? AnonymizedAt { get; set; }
        public string? PreferredLanguage { get; set; } = "pt-BR";

        // Navegação para credenciais FIDO2
        public virtual ICollection<UserCredential> Credentials { get; private set; } = new List<UserCredential>();

        // Navegação para roles (RBAC)
        public virtual ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();

        // Navegação para preferências de feriado
        public virtual ICollection<UserHolidayPreference> HolidayPreferences { get; private set; } = new List<UserHolidayPreference>();

        // Navegação para consentimentos LGPD/GDPR
        public virtual ICollection<UserConsent> Consents { get; private set; } = new List<UserConsent>();

        // Navegação para logs de auditoria
        public virtual ICollection<AuditLog> AuditLogs { get; private set; } = new List<AuditLog>();

        // Private constructor for EF Core
        private User()
        {
            Name = null!;
            Email = null!;
        }

        public User(Guid id, Name name, Email email, bool isActive = true)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (email == null) throw new ArgumentNullException(nameof(email));

            Id = id == Guid.Empty ? Guid.NewGuid() : id;
            Name = name;
            Email = email;
            IsActive = isActive;
            EmailVerified = false;
            CreatedAt = DateTime.UtcNow;
        }

        // Constructor for string parameters for backward compatibility
        public User(Guid id, string name, string email, bool isActive = true)
            : this(id, new Name(name), new Email(email), isActive)
        {
        }

        public void Activate() => IsActive = true;
        public void Deactivate() => IsActive = false;

        public void UpdateName(Name newName)
        {
            Name = newName ?? throw new ArgumentNullException(nameof(newName));
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateEmail(Email newEmail)
        {
            Email = newEmail ?? throw new ArgumentNullException(nameof(newEmail));
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Define hash da senha para autenticação
        /// </summary>
        public void SetPasswordHash(string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new ArgumentException("Password hash cannot be null or empty", nameof(passwordHash));

            PasswordHash = passwordHash;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Verifica email do usuário
        /// </summary>
        public void VerifyEmail()
        {
            EmailVerified = true;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Registra login do usuário
        /// </summary>
        public void RecordLogin()
        {
            LastLoginAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Adiciona uma nova credencial FIDO2 ao usuário
        /// </summary>
        public void AddCredential(UserCredential credential)
        {
            if (credential == null)
                throw new ArgumentNullException(nameof(credential));

            Credentials.Add(credential);
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Remove uma credencial FIDO2 do usuário (marca como inativa)
        /// </summary>
        public void RemoveCredential(Guid credentialId)
        {
            var credential = Credentials.FirstOrDefault(c => c.Id == credentialId);
            if (credential != null)
            {
                credential.Deactivate();
                UpdatedAt = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Define provedor externo OAuth2 para o usuário
        /// </summary>
        public void SetExternalProvider(string provider, string providerId)
        {
            if (string.IsNullOrWhiteSpace(provider))
                throw new ArgumentException("Provider cannot be null or empty", nameof(provider));
            if (string.IsNullOrWhiteSpace(providerId))
                throw new ArgumentException("Provider ID cannot be null or empty", nameof(providerId));

            ExternalProvider = provider;
            ExternalProviderId = providerId;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Define informações de localização do usuário
        /// </summary>
        public void SetLocation(string? country, string? state = null, string? city = null, string? timeZone = null)
        {
            Country = country?.ToUpper();
            State = state?.ToUpper();
            City = city;
            TimeZone = timeZone ?? "America/Sao_Paulo"; // Default para Brasil
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Remove provedor externo do usuário
        /// </summary>
        public void ClearExternalProvider()
        {
            ExternalProvider = null;
            ExternalProviderId = null;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Verifica se o usuário tem provedor externo configurado
        /// </summary>
        public bool HasExternalProvider => !string.IsNullOrWhiteSpace(ExternalProvider);

        /// <summary>
        /// Verifica se o usuário foi criado via OAuth2
        /// </summary>
        public bool IsExternalUser => HasExternalProvider && string.IsNullOrEmpty(PasswordHash);
    }
}
