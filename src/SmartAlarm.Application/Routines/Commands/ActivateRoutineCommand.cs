using System;
using MediatR;

namespace SmartAlarm.Application.Routines.Commands;

public record ActivateRoutineCommand(Guid Id) : IRequest<bool>;
