using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Application.Services
{
    /// <summary>
    /// Serviço de cache para feriados
    /// </summary>
    public interface IHolidayCacheService
    {
        /// <summary>
        /// Obtém feriados do cache
        /// </summary>
        /// <param name="country">Código do país</param>
        /// <param name="year">Ano</param>
        /// <param name="state">Estado (opcional)</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Lista de feriados ou null se não estiver em cache</returns>
        Task<List<Holiday>?> GetHolidaysAsync(string country, int year, string? state = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Armazena feriados no cache
        /// </summary>
        /// <param name="holidays">Lista de feriados</param>
        /// <param name="country">Código do país</param>
        /// <param name="year">Ano</param>
        /// <param name="state">Estado (opcional)</param>
        /// <param name="expiration">Tempo de expiração do cache</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        Task SetHolidaysAsync(List<Holiday> holidays, string country, int year, string? state = null, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove feriados do cache
        /// </summary>
        /// <param name="country">Código do país</param>
        /// <param name="year">Ano</param>
        /// <param name="state">Estado (opcional)</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        Task InvalidateAsync(string country, int year, string? state = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifica se uma data é feriado (com cache)
        /// </summary>
        /// <param name="date">Data para verificar</param>
        /// <param name="country">Código do país</param>
        /// <param name="state">Estado (opcional)</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>True se for feriado</returns>
        Task<bool> IsHolidayAsync(DateTime date, string country, string? state = null, CancellationToken cancellationToken = default);
    }
}