using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;

namespace SmartAlarm.Domain.Services
{
    /// <summary>
    /// Implementação concreta do serviço de domínio para operações de negócio relacionadas a alarmes.
    /// </summary>
    public class AlarmDomainService : IAlarmDomainService
    {
        private readonly IAlarmRepository _alarmRepository;
        private readonly ILogger<AlarmDomainService> _logger;

        public AlarmDomainService(IAlarmRepository alarmRepository, ILogger<AlarmDomainService> logger)
        {
            _alarmRepository = alarmRepository ?? throw new ArgumentNullException(nameof(alarmRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> CanUserCreateAlarmAsync(Guid userId)
        {
            var alarms = await _alarmRepository.GetByUserIdAsync(userId);
            // Exemplo de regra: limitar a 10 alarmes por usuário
            return alarms.Count() < 10;
        }

        public async Task<bool> CanTriggerAlarmAsync(Guid alarmId)
        {
            var alarm = await _alarmRepository.GetByIdAsync(alarmId);
            if (alarm == null)
            {
                _logger.LogWarning("Alarme não encontrado: {AlarmId}", alarmId);
                return false;
            }
            return alarm.Enabled && alarm.ShouldTriggerNow();
        }

        public Task<IEnumerable<Alarm>> GetAlarmsDueForTriggeringAsync()
        {
            // Busca todos os alarmes habilitados e verifica se devem disparar agora
            // (Idealmente, otimizar para buscar apenas os necessários)
            var allAlarms = new List<Alarm>();
            // Este método deve ser otimizado na infraestrutura para grandes volumes
            // Exemplo: buscar todos os alarmes e filtrar
            // allAlarms = await _alarmRepository.GetAllEnabledAsync();
            // Aqui, simulação:
            var due = allAlarms.Where(a => a.ShouldTriggerNow());
            return Task.FromResult(due);
        }

        public bool IsValidAlarmTime(DateTime time)
        {
            // Exemplo: não permitir horários no passado
            return time > DateTime.UtcNow;
        }

        public async Task<DateTime?> GetNextTriggerTimeAsync(Guid alarmId)
        {
            var alarm = await _alarmRepository.GetByIdAsync(alarmId);
            if (alarm == null)
            {
                _logger.LogWarning("Alarme não encontrado: {AlarmId}", alarmId);
                return null;
            }
            // Corrigir comparação: Schedule.Time é TimeOnly, comparar com hora/minuto atual
            var now = DateTime.Now;
            var nextSchedule = alarm.Schedules
                .Where(s => s.IsActive &&
                            (s.Time.ToTimeSpan() > now.TimeOfDay ||
                             s.Time.ToTimeSpan() == now.TimeOfDay))
                .OrderBy(s => s.Time)
                .FirstOrDefault();
            if (nextSchedule == null)
                return null;
            // Retornar o próximo DateTime para hoje ou amanhã, conforme necessário
            var nextDateTime = new DateTime(now.Year, now.Month, now.Day,
                                            nextSchedule.Time.Hour, nextSchedule.Time.Minute, 0);
            if (nextDateTime <= now)
                nextDateTime = nextDateTime.AddDays(1);
            return nextDateTime;
        }
    }
}
