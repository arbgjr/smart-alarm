using System;
using SmartAlarm.Domain.ValueObjects;

namespace SmartAlarm.Domain.Entities
{
    /// <summary>
    /// Representa um usuário do sistema Smart Alarm.
    /// </summary>
    public class User
    {
        public Guid Id { get; private set; }
        public Name Name { get; private set; }
        public Email Email { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? LastLoginAt { get; private set; }

        public User(Guid id, Name name, Email email, bool isActive = true)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (email == null) throw new ArgumentNullException(nameof(email));
            
            Id = id == Guid.Empty ? Guid.NewGuid() : id;
            Name = name;
            Email = email;
            IsActive = isActive;
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
        }

        public void UpdateEmail(Email newEmail)
        {
            Email = newEmail ?? throw new ArgumentNullException(nameof(newEmail));
        }

        public void RecordLogin()
        {
            LastLoginAt = DateTime.UtcNow;
        }
    }
}
