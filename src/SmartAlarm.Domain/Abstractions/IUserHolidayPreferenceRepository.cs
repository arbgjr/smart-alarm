using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Domain.Abstractions
{
    /// <summary>
    /// Interface para repositório de UserHolidayPreference
    /// Seguindo princípios de Clean Architecture - colocada no Domain
    /// </summary>
    public interface IUserHolidayPreferenceRepository
    {
        /// <summary>
        /// Adiciona uma nova preferência de feriado
        /// </summary>
        /// <param name="preference">Preferência a ser adicionada</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Task representando a operação assíncrona</returns>
        Task AddAsync(UserHolidayPreference preference, CancellationToken cancellationToken = default);

        /// <summary>
        /// Busca uma preferência por ID
        /// </summary>
        /// <param name="id">ID da preferência</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Preferência encontrada ou null</returns>
        Task<UserHolidayPreference?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Busca preferência específica de um usuário para um feriado
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <param name="holidayId">ID do feriado</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Preferência encontrada ou null</returns>
        Task<UserHolidayPreference?> GetByUserAndHolidayAsync(Guid userId, Guid holidayId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lista todas as preferências de um usuário
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Lista de preferências do usuário</returns>
        Task<IEnumerable<UserHolidayPreference>> GetByUserIdAsync(Guid userId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lista todas as preferências ativas de um usuário
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Lista de preferências ativas do usuário</returns>
        Task<IEnumerable<UserHolidayPreference>> GetActiveByUserIdAsync(Guid userId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lista preferências aplicáveis para uma data específica
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <param name="date">Data a verificar</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Lista de preferências aplicáveis na data</returns>
        Task<IEnumerable<UserHolidayPreference>> GetApplicableForDateAsync(Guid userId, DateTime date, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lista todas as preferências de feriado
        /// </summary>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Lista de todas as preferências</returns>
        Task<IEnumerable<UserHolidayPreference>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Atualiza uma preferência existente
        /// </summary>
        /// <param name="preference">Preferência a ser atualizada</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Task representando a operação assíncrona</returns>
        Task UpdateAsync(UserHolidayPreference preference, CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove uma preferência
        /// </summary>
        /// <param name="preference">Preferência a ser removida</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Task representando a operação assíncrona</returns>
        Task DeleteAsync(UserHolidayPreference preference, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifica se já existe uma preferência para o usuário e feriado
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <param name="holidayId">ID do feriado</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>True se existe a preferência</returns>
        Task<bool> ExistsAsync(Guid userId, Guid holidayId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Conta o número de preferências ativas de um usuário
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Número de preferências ativas</returns>
        Task<int> CountActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
