using System;
using System.Collections.Generic;
using MediatR;
using SmartAlarm.Application.DTOs;

namespace SmartAlarm.Application.Routines.Commands;

public record BulkUpdateRoutinesCommand(
    Guid UserId,
    ICollection<Guid> RoutineIds,
    BulkRoutineAction Action) : IRequest<Unit>;
