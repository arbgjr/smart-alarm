using System;
using System.Collections.Generic;

namespace SmartAlarm.Domain.Entities
{
    /// <summary>
    /// Representa uma rotina associada a um alarme.
    /// </summary>
    public class Routine
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public Guid AlarmId { get; private set; }
        public List<string> Actions { get; private set; }

        public Routine(Guid id, string name, Guid alarmId, List<string> actions)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Nome da rotina é obrigatório.", nameof(name));
            Id = id == Guid.Empty ? Guid.NewGuid() : id;
            Name = name;
            AlarmId = alarmId;
            Actions = actions ?? new List<string>();
        }
    }
}
