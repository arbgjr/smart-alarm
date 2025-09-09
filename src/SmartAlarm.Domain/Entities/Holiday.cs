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
        public string Description { get; private set; }
        public DateTime CreatedAt { get; private set; }

        // Navigation properties
        public virtual ICollection<UserHolidayPreference> UserPreferences { get; private set; } = new List<UserHolidayPreference>();

        // Private constructor for EF Core
        private Holiday()
        {
            Description = string.Empty;
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
            Description = description.Trim();
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
            return $"{Description} ({Date:dd/MM/yyyy})";
        }
    }
}
