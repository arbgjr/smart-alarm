using System;
using MediatR;
using SmartAlarm.Application.DTOs;

namespace SmartAlarm.Application.Queries
{
    /// <summary>
    /// Query para listar alarmes de um usuário.
    /// </summary>
    public class ListAlarmsQuery : IRequest<IList<AlarmResponseDto>>
    {
        public Guid UserId { get; }
        public ListAlarmsQuery(Guid userId)
        {
            UserId = userId;
        }
    }
}
