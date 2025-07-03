using System;

namespace SmartAlarm.Domain.ValueObjects
{
    /// <summary>
    /// Value Object para representar e validar nomes.
    /// </summary>
    public class Name
    {
        public string Value { get; }

        public Name(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Nome inválido.", nameof(value));
            Value = value;
        }

        public override string ToString() => Value;
        public override bool Equals(object obj) => obj is Name other && Value == other.Value;
        public override int GetHashCode() => Value.GetHashCode();
    }
}
