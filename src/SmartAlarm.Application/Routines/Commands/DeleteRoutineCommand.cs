using System;
using MediatR;

namespace SmartAlarm.Application.Routines.Commands;

public record DeleteRoutineCommand(Guid Id) : IRequest<bool>;
