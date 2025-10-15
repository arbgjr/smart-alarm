using System;

namespace SmartAlarm.Domain.Entities
{
    /// <summary>
    /// Representa um feriado que pode afetar o disparo de alarmes.
    /// </summary>
    public class Holiday
    {
        public Guid Id { get; private set; }
        public DateTime Date { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public HolidayType Type { get; private set; }
        public string Country { get; private set; }
        public string? State { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? LastSyncedAt { get; private set; }

        // Navigation properties
        public virtual ICollection<UserHolidayPreference> UserPreferences { get; private set; } = new List<UserHolidayPreference>();

        // Private constructor for EF Core
        private Holiday()
        {
            Name = string.Empty;
            Description = string.Empty;
            Country = string.Empty;
        }

        /// <summary>
        /// Cria um novo feriado.
        /// </summary>
        /// <param name="date">Data do feriado</param>
        /// <param name="description">Descrição do feriado</param>
        /// <exception cref="ArgumentException">Quando a descrição é inválida</exception>
        public Holiday(DateTime date, string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Description é obrigatória.", nameof(description));

            if (description.Length > 100)
                throw new ArgumentException("Description não pode ter mais de 100 caracteres.", nameof(description));

            Id = Guid.NewGuid();
            Date = date.Date; // Remove time component
            Name = description.Trim();
            Description = description.Trim();
            Type = HolidayType.Other;
            Country = "BR"; // Default
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Cria um feriado com ID específico (para testes ou importação).
        /// </summary>
        /// <param name="id">ID do feriado</param>
        /// <param name="date">Data do feriado</param>
        /// <param name="description">Descrição do feriado</param>
        public Holiday(Guid id, DateTime date, string description) : this(date, description)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Id não pode ser vazio.", nameof(id));

            Id = id;
        }

        /// <summary>
        /// Cria um feriado completo com todas as informações.
        /// </summary>
        public Holiday(Guid id, DateTime date, string name, string description, HolidayType type, string country, string? state = null)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Id não pode ser vazio.", nameof(id));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name é obrigatório.", nameof(name));
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Description é obrigatória.", nameof(description));
            if (string.IsNullOrWhiteSpace(country))
                throw new ArgumentException("Country é obrigatório.", nameof(country));

            Id = id;
            Date = date.Date;
            Name = name.Trim();
            Description = description.Trim();
            Type = type;
            Country = country.Trim().ToUpper();
            State = state?.Trim().ToUpper();
            CreatedAt = DateTime.UtcNow;
            LastSyncedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Atualiza a descrição do feriado.
        /// </summary>
        /// <param name="newDescription">Nova descrição</param>
        /// <exception cref="ArgumentException">Quando a descrição é inválida</exception>
        public void UpdateDescription(string newDescription)
        {
            if (string.IsNullOrWhiteSpace(newDescription))
                throw new ArgumentException("Description é obrigatória.", nameof(newDescription));

            if (newDescription.Length > 100)
                throw new ArgumentException("Description não pode ter mais de 100 caracteres.", nameof(newDescription));

            Description = newDescription.Trim();
        }

        /// <summary>
        /// Verifica se o feriado ocorre na data especificada.
        /// </summary>
        /// <param name="date">Data a verificar</param>
        /// <returns>True se o feriado ocorre na data</returns>
        public bool IsOnDate(DateTime date)
        {
            return Date.Date == date.Date;
        }

        /// <summary>
        /// Verifica se é um feriado de ano específico ou recorrente.
        /// </summary>
        /// <returns>True se é recorrente (sem ano específico)</returns>
        public bool IsRecurring()
        {
            // Considera recorrente se a data é 01/01/0001 (data padrão para feriados anuais)
            // ou se queremos implementar lógica mais complexa no futuro
            return Date.Year == 1;
        }

        public override bool Equals(object? obj)
        {
            if (obj is not Holiday other) return false;
            return Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            return $"{Name} ({Date:dd/MM/yyyy})";
        }

        /// <summary>
        /// Marca o feriado como sincronizado
        /// </summary>
        public void MarkAsSynced()
        {
            LastSyncedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Tipos de feriado
    /// </summary>
    public enum HolidayType
    {
        National,
        State,
        Municipal,
        Religious,
        Observance,
        Other
    }
}
