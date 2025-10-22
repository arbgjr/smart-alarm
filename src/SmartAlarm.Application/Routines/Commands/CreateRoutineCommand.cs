using System;
using System.Collections.Generic;
using MediatR;
using SmartAlarm.Application.DTOs;

namespace SmartAlarm.Application.Routines.Commands;

public record CreateRoutineCommand(
    string Name,
    string? Description,
    Guid UserId,
    Guid AlarmId,
    List<string> Actions,
    bool IsActive = true) : IRequest<RoutineDto>;
