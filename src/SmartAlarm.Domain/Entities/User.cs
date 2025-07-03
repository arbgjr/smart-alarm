using System;

namespace SmartAlarm.Domain.Entities
{
    /// <summary>
    /// Representa um usuário do sistema Smart Alarm.
    /// </summary>
    public class User
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public string Email { get; private set; }
        public bool IsActive { get; private set; }

        public User(Guid id, string name, string email, bool isActive)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Nome do usuário é obrigatório.", nameof(name));
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email do usuário é obrigatório.", nameof(email));
            Id = id == Guid.Empty ? Guid.NewGuid() : id;
            Name = name;
            Email = email;
            IsActive = isActive;
        }

        public void Activate() => IsActive = true;
        public void Deactivate() => IsActive = false;
    }
}
