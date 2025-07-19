using System;
using System.Collections.Generic;
using System.Linq;
using SmartAlarm.Domain.ValueObjects;

namespace SmartAlarm.Domain.Entities
{
    /// <summary>
    /// Representa uma rotina associada a um alarme.
    /// </summary>
    public class Routine
    {
        public List<string> Actions { get; private set; }

        public Guid Id { get; private set; }
        public Name? Name { get; private set; }
        public Guid AlarmId { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }

        // Private constructor for EF Core
        private Routine() { Actions = new List<string>(); }

        public Routine(Guid id, Name name, Guid alarmId, List<string>? actions = null)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (alarmId == Guid.Empty) throw new ArgumentException("AlarmId é obrigatório.", nameof(alarmId));
            Id = id == Guid.Empty ? Guid.NewGuid() : id;
            Name = name;
            AlarmId = alarmId;
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
            Actions = actions?.Where(a => !string.IsNullOrWhiteSpace(a)).ToList() ?? new List<string>();
        }

        // Constructor for string parameters for backward compatibility
        public Routine(Guid id, string name, Guid alarmId, List<string>? actions = null)
            : this(id, new Name(name), alarmId, actions)
        {
        }

        public void Activate() => IsActive = true;
        public void Deactivate() => IsActive = false;

        public void UpdateName(Name newName)
        {
            Name = newName ?? throw new ArgumentNullException(nameof(newName));
        }

        public void AddAction(string action)
        {
            if (string.IsNullOrWhiteSpace(action))
                throw new ArgumentException("Ação não pode ser vazia.", nameof(action));
            if (Actions.Contains(action))
                throw new InvalidOperationException("Ação já existe na rotina.");
            Actions.Add(action);
        }

        public void RemoveAction(string action)
        {
            Actions.Remove(action);
        }

        public void ClearActions()
        {
            Actions.Clear();
        }

        public bool HasActions => Actions.Any();
    }
}
