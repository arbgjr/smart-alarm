using System;
using System.Collections.Generic;

namespace SmartAlarm.Domain.Entities
{
    /// <summary>
    /// Representa um alarme configurado pelo usuário.
    /// </summary>
    public class Alarm
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public DateTime Time { get; private set; }
        public bool Enabled { get; private set; }
        public Guid UserId { get; private set; }
        public List<Routine> Routines { get; private set; }
        public List<Integration> Integrations { get; private set; }

        public Alarm(Guid id, string name, DateTime time, bool enabled, Guid userId)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Nome do alarme é obrigatório.", nameof(name));
            Id = id == Guid.Empty ? Guid.NewGuid() : id;
            Name = name;
            Time = time;
            Enabled = enabled;
            UserId = userId;
            Routines = new List<Routine>();
            Integrations = new List<Integration>();
        }

        public void Enable() => Enabled = true;
        public void Disable() => Enabled = false;
        public void AddRoutine(Routine routine)
        {
            if (routine == null) throw new ArgumentNullException(nameof(routine));
            Routines.Add(routine);
        }
        public void AddIntegration(Integration integration)
        {
            if (integration == null) throw new ArgumentNullException(nameof(integration));
            Integrations.Add(integration);
        }
    }
}
