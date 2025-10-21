using System;
using System.Collections.Generic;

namespace SmartAlarm.Application.DTOs;

public enum BulkRoutineAction
{
    Enable,
    Disable,
    Delete
}

public record BulkRoutineUpdateDto(
    ICollection<Guid> RoutineIds,
    BulkRoutineAction Action);
