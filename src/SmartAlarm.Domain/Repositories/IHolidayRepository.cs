using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Domain.Repositories
{
    /// <summary>
    /// Interface para persistência e consulta de Feriados.
    /// </summary>
    public interface IHolidayRepository
    {
        /// <summary>
        /// Busca um feriado por ID.
        /// </summary>
        Task<Holiday?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Busca um feriado por data específica.
        /// </summary>
        Task<Holiday?> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default);

        /// <summary>
        /// Busca feriados por data específica (incluindo recorrentes).
        /// </summary>
        Task<IEnumerable<Holiday>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lista todos os feriados.
        /// </summary>
        Task<IEnumerable<Holiday>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Busca todos os feriados de um ano.
        /// </summary>
        Task<IEnumerable<Holiday>> GetByYearAsync(int year, CancellationToken cancellationToken = default);

        /// <summary>
        /// Busca feriados em um intervalo de datas.
        /// </summary>
        Task<IEnumerable<Holiday>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Busca feriados por país/região.
        /// </summary>
        Task<IEnumerable<Holiday>> GetByCountryAsync(string country, string? region = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lista feriados recorrentes (anuais).
        /// </summary>
        Task<IEnumerable<Holiday>> GetRecurringAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Adiciona um novo feriado.
        /// </summary>
        Task AddAsync(Holiday holiday, CancellationToken cancellationToken = default);

        /// <summary>
        /// Atualiza um feriado existente.
        /// </summary>
        Task UpdateAsync(Holiday holiday, CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove um feriado por ID.
        /// </summary>
        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifica se uma data é feriado.
        /// </summary>
        Task<bool> IsHolidayAsync(DateTime date, string? country = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifica se existe um feriado em uma data específica.
        /// </summary>
        Task<bool> ExistsOnDateAsync(DateOnly date, CancellationToken cancellationToken = default);

        /// <summary>
        /// Busca um feriado por data e país/estado.
        /// </summary>
        Task<Holiday?> GetByDateAndCountryAsync(DateTime date, string country, string? state = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Salva as alterações no repositório.
        /// </summary>
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}