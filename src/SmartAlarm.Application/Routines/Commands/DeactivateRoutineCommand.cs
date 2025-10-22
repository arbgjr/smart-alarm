using System;
using MediatR;

namespace SmartAlarm.Application.Routines.Commands;

public record DeactivateRoutineCommand(Guid Id) : IRequest<bool>;
