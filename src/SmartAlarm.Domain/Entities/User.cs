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

        // Navegação para credenciais FIDO2
        public virtual ICollection<UserCredential> Credentials { get; private set; } = new List<UserCredential>();

        // Navegação para roles (RBAC)  
        public virtual ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();

        // Navegação para preferências de feriado
        public virtual ICollection<UserHolidayPreference> HolidayPreferences { get; private set; } = new List<UserHolidayPreference>();

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
    }
}
