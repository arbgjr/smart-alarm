using MediatR;
using SmartAlarm.Application.DTOs.Routine;
using System;

namespace SmartAlarm.Application.Queries.Routine
{
    public class GetRoutineByIdQuery : IRequest<RoutineResponseDto>
    {
        public Guid Id { get; set; }
        public GetRoutineByIdQuery(Guid id) => Id = id;
    }
}
