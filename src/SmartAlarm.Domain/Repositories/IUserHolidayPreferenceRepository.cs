using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Domain.Repositories
{
    /// <summary>
    /// Interface para persistência e consulta de Preferências de Feriados do Usuário.
    /// </summary>
    public interface IUserHolidayPreferenceRepository
    {
        /// <summary>
        /// Busca uma preferência por ID.
        /// </summary>
        Task<UserHolidayPreference?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Busca preferência de um usuário para um feriado específico.
        /// </summary>
        Task<UserHolidayPreference?> GetByUserAndHolidayAsync(Guid userId, Guid holidayId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Busca todas as preferências de feriados de um usuário.
        /// </summary>
        Task<IEnumerable<UserHolidayPreference>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Busca preferências ativas de um usuário.
        /// </summary>
        Task<IEnumerable<UserHolidayPreference>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lista preferências aplicáveis para uma data específica.
        /// </summary>
        Task<IEnumerable<UserHolidayPreference>> GetApplicableForDateAsync(Guid userId, DateTime date, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lista todas as preferências de feriado.
        /// </summary>
        Task<IEnumerable<UserHolidayPreference>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Adiciona uma nova preferência de feriado.
        /// </summary>
        Task AddAsync(UserHolidayPreference preference, CancellationToken cancellationToken = default);

        /// <summary>
        /// Atualiza uma preferência existente.
        /// </summary>
        Task UpdateAsync(UserHolidayPreference preference, CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove uma preferência por ID.
        /// </summary>
        Task DeleteAsync(UserHolidayPreference preference, CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove todas as preferências de um usuário.
        /// </summary>
        Task DeleteAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifica se o usuário tem preferência configurada para um feriado.
        /// </summary>
        Task<bool> ExistsAsync(Guid userId, Guid holidayId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Conta o número de preferências ativas de um usuário.
        /// </summary>
        Task<int> CountActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}