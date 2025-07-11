using System;
using System.Collections.Generic;
using System.Linq;
using SmartAlarm.Domain.ValueObjects;

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
            UserId = userId;
            CreatedAt = DateTime.UtcNow;
        }

        // Constructor for string parameters for backward compatibility
        public Alarm(Guid id, string name, DateTime time, bool enabled, Guid userId)
            : this(id, new Name(name), time, enabled, userId)
        {
        }

        public void Enable() => Enabled = true;
        public void Disable() => Enabled = false;

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

        public virtual bool ShouldTriggerNow()
        {
            if (!Enabled) return false;
            var now = DateTime.Now;
            return _schedules.Any(s => s.IsActive && s.ShouldTriggerToday() &&
                                      s.Time.Hour == now.Hour && s.Time.Minute == now.Minute);
        }
    }
}

