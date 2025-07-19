using System;
using MediatR;
using SmartAlarm.Application.DTOs;

namespace SmartAlarm.Application.Queries
{
    /// <summary>
    /// Query to get an alarm by its Id.
    /// </summary>
    public class GetAlarmByIdQuery : IRequest<AlarmResponseDto?>
    {
        public Guid Id { get; }
        public GetAlarmByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
