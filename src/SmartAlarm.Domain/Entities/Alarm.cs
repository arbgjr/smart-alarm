using System;
using System.Collections.Generic;
using System.Linq;
using SmartAlarm.Domain.ValueObjects;
using SmartAlarm.Domain.Repositories;

namespace SmartAlarm.Domain.Entities
{
    /// <summary>
    /// Representa um alarme configurado pelo usuÃ¡rio.
    /// </summary>
    public class Alarm
    {
        private readonly List<Routine> _routines = new();
        private readonly List<Integration> _integrations = new();
        private readonly List<Schedule> _schedules = new();

        public Guid Id { get; private set; }
        public Name Name { get; private set; } = null!;
        public DateTime Time { get; private set; }
        public bool Enabled { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsRecurring { get; private set; }
        public Dictionary<string, object> Metadata { get; private set; } = new();
        public Guid UserId { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? LastTriggeredAt { get; private set; }
        public IReadOnlyList<Routine> Routines => _routines.AsReadOnly();
        public IReadOnlyList<Integration> Integrations => _integrations.AsReadOnly();
        public IReadOnlyList<Schedule> Schedules => _schedules.AsReadOnly();

        // Private constructor for EF Core
        private Alarm() { }

        public Alarm(Guid id, Name name, DateTime time, bool enabled, Guid userId)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (userId == Guid.Empty) throw new ArgumentException("UserId Ã© obrigatÃ³rio.", nameof(userId));

            Id = id == Guid.Empty ? Guid.NewGuid() : id;
            Name = name;
            Time = time;
            Enabled = enabled;
            IsActive = enabled; // Initialize IsActive with same value as Enabled
            IsRecurring = false; // Default to non-recurring
            Metadata = new Dictionary<string, object>();
            UserId = userId;
            CreatedAt = DateTime.UtcNow;
        }

        // Constructor for string parameters for backward compatibility
        public Alarm(Guid id, string name, DateTime time, bool enabled, Guid userId)
            : this(id, new Name(name), time, enabled, userId)
        {
        }

        public void Enable()
        {
            Enabled = true;
            IsActive = true;
        }

        public void Disable()
        {
            Enabled = false;
            IsActive = false;
        }

        public void SetActive(bool isActive) => IsActive = isActive;

        public void SetRecurring(bool isRecurring) => IsRecurring = isRecurring;

        public void UpdateMetadata(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            Metadata[key] = value;
        }

        public void RemoveMetadata(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            Metadata.Remove(key);
        }

        public T? GetMetadata<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || !Metadata.ContainsKey(key)) return default;
            try
            {
                return (T)Metadata[key];
            }
            catch
            {
                return default;
            }
        }

        public void UpdateName(Name newName)
        {
            Name = newName ?? throw new ArgumentNullException(nameof(newName));
        }

        public void UpdateTime(DateTime newTime)
        {
            Time = newTime;
        }

        public void AddRoutine(Routine routine)
        {
            if (routine == null) throw new ArgumentNullException(nameof(routine));
            if (_routines.Any(r => r.Id == routine.Id))
                throw new InvalidOperationException("Rotina jÃ¡ existe no alarme.");

            _routines.Add(routine);
        }

        public void RemoveRoutine(Guid routineId)
        {
            var routine = _routines.FirstOrDefault(r => r.Id == routineId);
            if (routine != null)
            {
                _routines.Remove(routine);
            }
        }

        public void AddIntegration(Integration integration)
        {
            if (integration == null) throw new ArgumentNullException(nameof(integration));
            if (_integrations.Any(i => i.Id == integration.Id))
                throw new InvalidOperationException("IntegraÃ§Ã£o jÃ¡ existe no alarme.");

            _integrations.Add(integration);
        }

        public void RemoveIntegration(Guid integrationId)
        {
            var integration = _integrations.FirstOrDefault(i => i.Id == integrationId);
            if (integration != null)
            {
                _integrations.Remove(integration);
            }
        }

        public void AddSchedule(Schedule schedule)
        {
            if (schedule == null) throw new ArgumentNullException(nameof(schedule));
            if (schedule.AlarmId != Id)
                throw new InvalidOperationException("Schedule nÃ£o pertence a este alarme.");
            if (_schedules.Any(s => s.Id == schedule.Id))
                throw new InvalidOperationException("Schedule jÃ¡ existe no alarme.");

            _schedules.Add(schedule);
        }

        public void RemoveSchedule(Guid scheduleId)
        {
            var schedule = _schedules.FirstOrDefault(s => s.Id == scheduleId);
            if (schedule != null)
            {
                _schedules.Remove(schedule);
            }
        }

        public void RecordTriggered()
        {
            if (!Enabled)
                throw new InvalidOperationException("NÃ£o Ã© possÃ­vel disparar um alarme desabilitado.");

            LastTriggeredAt = DateTime.UtcNow;
        }

        public void RecordTrigger(DateTime triggeredAt)
        {
            if (!Enabled)
                throw new InvalidOperationException("NÃ£o Ã© possÃ­vel disparar um alarme desabilitado.");

            LastTriggeredAt = triggeredAt;
        }

        public bool ShouldTriggerNow(
            IExceptionPeriodRepository? exceptionPeriodRepository = null,
            IHolidayRepository? holidayRepository = null,
            IUserHolidayPreferenceRepository? userHolidayPreferenceRepository = null)
        {
            if (!Enabled) return false;
            var now = DateTime.Now;

            // Se repositórios não foram fornecidos, apenas verifica schedules básicos
            // Isso permite que a entidade funcione sem dependências externas quando necessário
            if (exceptionPeriodRepository == null || holidayRepository == null || userHolidayPreferenceRepository == null)
            {
                return _schedules.Any(s => s.IsActive && s.ShouldTriggerToday() &&
                                          s.Time.Hour == now.Hour && s.Time.Minute == now.Minute);
            }

            // Verifica se existe ExceptionPeriod ativo para o usuário e data atual
            var hasActivePeriod = exceptionPeriodRepository.HasActivePeriodOnDateAsync(UserId, now).GetAwaiter().GetResult();
            if (hasActivePeriod)
                return false;

            // Verifica se hoje é feriado e se há preferência do usuário
            var holiday = holidayRepository.GetByDateAsync(now.Date).GetAwaiter().GetResult();
            if (holiday != null)
            {
                var preference = userHolidayPreferenceRepository.GetByUserAndHolidayAsync(UserId, holiday.Id).GetAwaiter().GetResult();
                if (preference != null && preference.IsEnabled)
                {
                    switch (preference.Action)
                    {
                        case HolidayPreferenceAction.Disable:
                            return false;
                        case HolidayPreferenceAction.Skip:
                            return false;
                        case HolidayPreferenceAction.Delay:
                            // Adiciona o atraso ao horário do alarme
                            return _schedules.Any(s => s.IsActive && s.ShouldTriggerToday() &&
                                s.Time.Hour == now.AddMinutes(-(preference.DelayInMinutes ?? 0)).Hour &&
                                s.Time.Minute == now.AddMinutes(-(preference.DelayInMinutes ?? 0)).Minute);
                        default:
                            break;
                    }
                }
            }

            // Lógica padrão
            return _schedules.Any(s => s.IsActive && s.ShouldTriggerToday() &&
                                      s.Time.Hour == now.Hour && s.Time.Minute == now.Minute);
        }
    }
}

