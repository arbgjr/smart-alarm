using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Domain.Abstractions;

/// <summary>
/// Interface para repositório de Holiday
/// Seguindo princípios de Clean Architecture - colocada no Domain
/// </summary>
public interface IHolidayRepository
{
    /// <summary>
    /// Adiciona um novo feriado
    /// </summary>
    /// <param name="holiday">Feriado a ser adicionado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Task representando a operação assíncrona</returns>
    Task AddAsync(Holiday holiday, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca um feriado por ID
    /// </summary>
    /// <param name="id">ID do feriado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Feriado encontrado ou null</returns>
    Task<Holiday?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista todos os feriados
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Lista de feriados</returns>
    Task<IEnumerable<Holiday>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca feriados por data específica
    /// </summary>
    /// <param name="date">Data para buscar feriados</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Lista de feriados na data especificada</returns>
    Task<IEnumerable<Holiday>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista feriados recorrentes (anuais)
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Lista de feriados recorrentes</returns>
    Task<IEnumerable<Holiday>> GetRecurringAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza um feriado existente
    /// </summary>
    /// <param name="holiday">Feriado com dados atualizados</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Task representando a operação assíncrona</returns>
    Task UpdateAsync(Holiday holiday, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove um feriado
    /// </summary>
    /// <param name="id">ID do feriado a ser removido</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>True se removido com sucesso, False se não encontrado</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se existe um feriado em uma data específica
    /// </summary>
    /// <param name="date">Data para verificar</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>True se existe feriado na data</returns>
    Task<bool> ExistsOnDateAsync(DateOnly date, CancellationToken cancellationToken = default);
}
