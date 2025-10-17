﻿using System;

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
                throw new ArgumentException("Nome invÃ¡lido.", nameof(value));
            Value = value;
        }

        public override string ToString() => Value;
        public override bool Equals(object? obj) => obj is Name other && Value == other.Value;
        public override int GetHashCode() => Value.GetHashCode();

        // Implicit conversions
        public static implicit operator string(Name name) => name?.Value ?? string.Empty;
        public static implicit operator Name(string value) => new Name(value);
    }
}

