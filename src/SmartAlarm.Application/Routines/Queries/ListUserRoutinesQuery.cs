using System;
using MediatR;
using SmartAlarm.Application.Common;
using SmartAlarm.Application.DTOs;

namespace SmartAlarm.Application.Routines.Queries;

public record ListUserRoutinesQuery(
    Guid UserId,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<PaginatedList<RoutineDto>>;
