using System;
using MediatR;
using SmartAlarm.Application.DTOs;

namespace SmartAlarm.Application.Routines.Queries;

public record GetRoutineByIdQuery(Guid Id) : IRequest<RoutineDto?>;
