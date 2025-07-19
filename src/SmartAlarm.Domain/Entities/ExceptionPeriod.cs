using System;

namespace SmartAlarm.Domain.Entities
{
    /// <summary>
    /// Representa um período de exceção onde alarmes podem ser desabilitados ou ter comportamento alterado.
    /// Exemplos: férias, feriados, períodos de manutenção, etc.
    /// </summary>
    public class ExceptionPeriod
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public string? Description { get; private set; }
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }
        public ExceptionPeriodType Type { get; private set; }
        public bool IsActive { get; private set; }
        public Guid UserId { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        // Private constructor for EF Core
        private ExceptionPeriod() 
        { 
            Name = string.Empty;
        }

        /// <summary>
        /// Cria um novo período de exceção.
        /// </summary>
        /// <param name="id">ID do período (Guid.Empty para gerar novo)</param>
        /// <param name="name">Nome do período de exceção</param>
        /// <param name="startDate">Data de início do período</param>
        /// <param name="endDate">Data de fim do período</param>
        /// <param name="type">Tipo do período de exceção</param>
        /// <param name="userId">ID do usuário proprietário</param>
        /// <param name="description">Descrição opcional</param>
        /// <exception cref="ArgumentException">Quando os parâmetros são inválidos</exception>
        public ExceptionPeriod(Guid id, string name, DateTime startDate, DateTime endDate, 
            ExceptionPeriodType type, Guid userId, string? description = null)
        {
            ValidateParameters(name, startDate, endDate, userId);

            Id = id == Guid.Empty ? Guid.NewGuid() : id;
            Name = name;
            Description = description;
            StartDate = startDate;
            EndDate = endDate;
            Type = type;
            UserId = userId;
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Ativa o período de exceção.
        /// </summary>
        public void Activate()
        {
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Desativa o período de exceção.
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Atualiza o nome do período de exceção.
        /// </summary>
        /// <param name="newName">Novo nome</param>
        /// <exception cref="ArgumentException">Quando o nome é inválido</exception>
        public void UpdateName(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("Nome é obrigatório.", nameof(newName));

            if (newName.Length > 100)
                throw new ArgumentException("Nome não pode ter mais de 100 caracteres.", nameof(newName));

            Name = newName.Trim();
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Atualiza a descrição do período de exceção.
        /// </summary>
        /// <param name="newDescription">Nova descrição</param>
        public void UpdateDescription(string? newDescription)
        {
            if (!string.IsNullOrEmpty(newDescription) && newDescription.Length > 500)
                throw new ArgumentException("Descrição não pode ter mais de 500 caracteres.", nameof(newDescription));

            Description = string.IsNullOrWhiteSpace(newDescription) ? null : newDescription.Trim();
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Atualiza as datas do período de exceção.
        /// </summary>
        /// <param name="newStartDate">Nova data de início</param>
        /// <param name="newEndDate">Nova data de fim</param>
        /// <exception cref="ArgumentException">Quando as datas são inválidas</exception>
        public void UpdatePeriod(DateTime newStartDate, DateTime newEndDate)
        {
            if (newStartDate >= newEndDate)
                throw new ArgumentException("Data de início deve ser anterior à data de fim.");

            StartDate = newStartDate;
            EndDate = newEndDate;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Atualiza o tipo do período de exceção.
        /// </summary>
        /// <param name="newType">Novo tipo</param>
        public void UpdateType(ExceptionPeriodType newType)
        {
            Type = newType;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Verifica se o período está ativo na data especificada.
        /// </summary>
        /// <param name="date">Data a verificar</param>
        /// <returns>True se o período está ativo na data</returns>
        public bool IsActiveOnDate(DateTime date)
        {
            if (!IsActive) return false;

            var dateOnly = date.Date;
            return dateOnly >= StartDate.Date && dateOnly <= EndDate.Date;
        }

        /// <summary>
        /// Verifica se o período está ativo atualmente.
        /// </summary>
        /// <returns>True se o período está ativo hoje</returns>
        public bool IsCurrentlyActive()
        {
            return IsActiveOnDate(DateTime.UtcNow);
        }

        /// <summary>
        /// Verifica se há sobreposição com outro período.
        /// </summary>
        /// <param name="other">Outro período para verificar</param>
        /// <returns>True se há sobreposição</returns>
        public bool OverlapsWith(ExceptionPeriod other)
        {
            if (other == null) return false;

            return StartDate.Date <= other.EndDate.Date && EndDate.Date >= other.StartDate.Date;
        }

        /// <summary>
        /// Calcula a duração do período em dias.
        /// </summary>
        /// <returns>Número de dias do período</returns>
        public int GetDurationInDays()
        {
            return (EndDate.Date - StartDate.Date).Days + 1;
        }

        private static void ValidateParameters(string name, DateTime startDate, DateTime endDate, Guid userId)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Nome é obrigatório.", nameof(name));

            if (name.Length > 100)
                throw new ArgumentException("Nome não pode ter mais de 100 caracteres.", nameof(name));

            if (startDate >= endDate)
                throw new ArgumentException("Data de início deve ser anterior à data de fim.");

            if (userId == Guid.Empty)
                throw new ArgumentException("UserId é obrigatório.", nameof(userId));
        }
    }
}
