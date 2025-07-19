using MediatR;
using SmartAlarm.Application.DTOs.Routine;
using System;
using System.Collections.Generic;

namespace SmartAlarm.Application.Queries.Routine
{
    public class ListRoutinesQuery : IRequest<List<RoutineResponseDto>>
    {
        public Guid? UserId { get; set; }
        public Guid? AlarmId { get; set; }
    }
}
