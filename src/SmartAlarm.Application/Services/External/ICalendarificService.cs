using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Application.Services.External
{
    /// <summary>
    /// Interface para integração com API Calendarific para obter feriados
    /// </summary>
    public interface ICalendarificService
    {
        /// <summary>
        /// Obtém lista de feriados para um país e ano específico
        /// </summary>
        /// <param name="countryCode">Código do país (ex: BR para Brasil)</param>
        /// <param name="year">Ano para buscar feriados</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Lista de feriados</returns>
        Task<List<Holiday>> GetHolidaysAsync(string countryCode, int year, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtém lista de feriados para um país, estado e ano específico
        /// </summary>
        /// <param name="countryCode">Código do país (ex: BR para Brasil)</param>
        /// <param name="state">Código do estado (ex: SP para São Paulo)</param>
        /// <param name="year">Ano para buscar feriados</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Lista de feriados</returns>
        Task<List<Holiday>> GetHolidaysAsync(string countryCode, string state, int year, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifica se uma data específica é feriado
        /// </summary>
        /// <param name="date">Data para verificar</param>
        /// <param name="countryCode">Código do país</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>True se for feriado, false caso contrário</returns>
        Task<bool> IsHolidayAsync(DateTime date, string countryCode, CancellationToken cancellationToken = default);
    }
}