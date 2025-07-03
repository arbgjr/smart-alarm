using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Domain.Services
{
    /// <summary>
    /// Serviço de domínio para operações de negócio relacionadas a alarmes.
    /// </summary>
    public interface IAlarmDomainService
    {
        /// <summary>
        /// Verifica se um usuário pode criar um novo alarme.
        /// </summary>
        Task<bool> CanUserCreateAlarmAsync(Guid userId);

        /// <summary>
        /// Verifica se um alarme pode ser disparado no momento atual.
        /// </summary>
        Task<bool> CanTriggerAlarmAsync(Guid alarmId);

        /// <summary>
        /// Obtém todos os alarmes que devem ser disparados agora.
        /// </summary>
        Task<IEnumerable<Alarm>> GetAlarmsDueForTriggeringAsync();

        /// <summary>
        /// Valida se uma configuração de horário é válida para um alarme.
        /// </summary>
        bool IsValidAlarmTime(DateTime time);

        /// <summary>
        /// Calcula o próximo horário em que um alarme deve ser disparado.
        /// </summary>
        Task<DateTime?> GetNextTriggerTimeAsync(Guid alarmId);
    }
}