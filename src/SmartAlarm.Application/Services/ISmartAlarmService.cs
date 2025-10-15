using System;
using System.Threading;
using System.Threading.Tasks;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Application.Services
{
    /// <summary>
    /// Serviço principal para lógica inteligente de alarmes
    /// </summary>
    public interface ISmartAlarmService
    {
        /// <summary>
        /// Verifica se um alarme deve disparar considerando contexto inteligente
        /// </summary>
        /// <param name="alarm">Alarme a verificar</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>True se o alarme deve disparar</returns>
        Task<bool> ShouldAlarmTriggerAsync(Alarm alarm, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtém razão pela qual um alarme foi desativado
        /// </summary>
        /// <param name="alarmId">ID do alarme</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Razão da desativação ou null</returns>
        Task<string?> GetDisableReasonAsync(Guid alarmId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifica se hoje é feriado para o usuário
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>True se for feriado</returns>
        Task<bool> IsTodayHolidayAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifica se o usuário está de férias/folga hoje
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>True se estiver de férias/folga</returns>
        Task<bool> IsUserOnVacationAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}