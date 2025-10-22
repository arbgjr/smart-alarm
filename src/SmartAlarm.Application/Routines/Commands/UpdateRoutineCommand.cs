using System;
using System.Collections.Generic;
using MediatR;
using SmartAlarm.Application.DTOs;

namespace SmartAlarm.Application.Routines.Commands;

public record UpdateRoutineCommand(
    Guid Id,
    string Name,
    string? Description,
    List<string> Actions,
    bool IsActive) : IRequest<RoutineDto?>;
