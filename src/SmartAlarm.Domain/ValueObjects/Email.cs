using System;
using System.Text.RegularExpressions;

namespace SmartAlarm.Domain.ValueObjects
{
    /// <summary>
    /// Value Object para representar e validar e-mail de usuÃ¡rio.
    /// </summary>
    public class Email
    {
        private static readonly Regex EmailRegex = new Regex(
            @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string Address { get; }

        public Email(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("E-mail nÃ£o pode ser vazio.", nameof(address));
            
            if (!EmailRegex.IsMatch(address))
                throw new ArgumentException("E-mail invÃ¡lido.", nameof(address));
            
            Address = address;
        }

        public override string ToString() => Address;
        public override bool Equals(object? obj) => obj is Email other && Address == other.Address;
        public override int GetHashCode() => Address.GetHashCode();
    }
}

