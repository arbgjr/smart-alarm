using System;

namespace SmartAlarm.Domain.ValueObjects
{
    /// <summary>
    /// Value Object para representar e validar e-mail de usuário.
    /// </summary>
    public class Email
    {
        public string Address { get; }

        public Email(string address)
        {
            if (string.IsNullOrWhiteSpace(address) || !address.Contains("@"))
                throw new ArgumentException("E-mail inválido.", nameof(address));
            Address = address;
        }

        public override string ToString() => Address;
        public override bool Equals(object obj) => obj is Email other && Address == other.Address;
        public override int GetHashCode() => Address.GetHashCode();
    }
}
