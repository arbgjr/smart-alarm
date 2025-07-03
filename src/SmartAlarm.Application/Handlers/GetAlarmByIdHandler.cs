using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.DTOs;
using SmartAlarm.Application.Queries;
using SmartAlarm.Domain.Repositories;

namespace SmartAlarm.Application.Handlers
{
    /// <summary>
    /// Handler for GetAlarmByIdQuery.
    /// </summary>
    public class GetAlarmByIdHandler : IRequestHandler<GetAlarmByIdQuery, AlarmResponseDto?>
    {
        private readonly IAlarmRepository _alarmRepository;
        private readonly ILogger<GetAlarmByIdHandler> _logger;

        public GetAlarmByIdHandler(IAlarmRepository alarmRepository, ILogger<GetAlarmByIdHandler> logger)
        {
            _alarmRepository = alarmRepository;
            _logger = logger;
        }

        public async Task<AlarmResponseDto?> Handle(GetAlarmByIdQuery request, CancellationToken cancellationToken)
        {
            var alarm = await _alarmRepository.GetByIdAsync(request.Id);
            if (alarm == null)
            {
                _logger.LogWarning("Alarm not found: {AlarmId}", request.Id);
                return null;
            }
            return new AlarmResponseDto(alarm);
        }
    }
}
