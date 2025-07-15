using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Domain.Repositories
{
    /// <summary>
    /// Interface para persistência e consulta de Períodos de Exceção.
    /// </summary>
    public interface IExceptionPeriodRepository
    {
        /// <summary>
        /// Busca um período de exceção por ID.
        /// </summary>
        /// <param name="id">ID do período de exceção</param>
        /// <returns>Período de exceção encontrado ou null</returns>
        Task<ExceptionPeriod?> GetByIdAsync(Guid id);

        /// <summary>
        /// Busca todos os períodos de exceção de um usuário.
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <returns>Lista de períodos de exceção do usuário</returns>
        Task<IEnumerable<ExceptionPeriod>> GetByUserIdAsync(Guid userId);

        /// <summary>
        /// Busca períodos de exceção ativos de um usuário em uma data específica.
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <param name="date">Data para verificar</param>
        /// <returns>Lista de períodos de exceção ativos na data</returns>
        Task<IEnumerable<ExceptionPeriod>> GetActivePeriodsOnDateAsync(Guid userId, DateTime date);

        /// <summary>
        /// Busca períodos de exceção que se sobrepõem a um intervalo de datas.
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <param name="startDate">Data de início do intervalo</param>
        /// <param name="endDate">Data de fim do intervalo</param>
        /// <param name="excludeId">ID do período a excluir da busca (opcional)</param>
        /// <returns>Lista de períodos que se sobrepõem ao intervalo</returns>
        Task<IEnumerable<ExceptionPeriod>> GetOverlappingPeriodsAsync(Guid userId, DateTime startDate, DateTime endDate, Guid? excludeId = null);

        /// <summary>
        /// Busca períodos de exceção por tipo.
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <param name="type">Tipo do período de exceção</param>
        /// <returns>Lista de períodos do tipo especificado</returns>
        Task<IEnumerable<ExceptionPeriod>> GetByTypeAsync(Guid userId, ExceptionPeriodType type);

        /// <summary>
        /// Adiciona um novo período de exceção.
        /// </summary>
        /// <param name="exceptionPeriod">Período de exceção a ser adicionado</param>
        Task AddAsync(ExceptionPeriod exceptionPeriod);

        /// <summary>
        /// Atualiza um período de exceção existente.
        /// </summary>
        /// <param name="exceptionPeriod">Período de exceção a ser atualizado</param>
        Task UpdateAsync(ExceptionPeriod exceptionPeriod);

        /// <summary>
        /// Remove um período de exceção por ID.
        /// </summary>
        /// <param name="id">ID do período de exceção a ser removido</param>
        Task DeleteAsync(Guid id);

        /// <summary>
        /// Conta quantos períodos de exceção um usuário possui.
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <returns>Número de períodos de exceção do usuário</returns>
        Task<int> CountByUserIdAsync(Guid userId);

        /// <summary>
        /// Verifica se existe algum período de exceção ativo para um usuário em uma data.
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <param name="date">Data a verificar</param>
        /// <returns>True se existe período ativo</returns>
        Task<bool> HasActivePeriodOnDateAsync(Guid userId, DateTime date);
    }
}
