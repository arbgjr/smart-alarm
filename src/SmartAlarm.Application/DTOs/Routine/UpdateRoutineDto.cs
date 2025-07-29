using System.Collections.Generic;

namespace SmartAlarm.Application.DTOs.Routine
{
    /// <summary>
    /// DTO for updating routine information.
    /// </summary>
    public class UpdateRoutineDto
    {
        /// <summary>
        /// The new name for the routine.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The list of actions for the routine.
        /// </summary>
        public List<string> Actions { get; set; } = new();
    }
}
