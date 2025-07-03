using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Commands;
using SmartAlarm.Domain.Repositories;

namespace SmartAlarm.Application.Handlers
{
    /// <summary>
    /// Handler para exclusão de alarme.
    /// </summary>
    public class DeleteAlarmHandler : IRequestHandler<DeleteAlarmCommand, bool>
    {
        private readonly IAlarmRepository _alarmRepository;
        private readonly ILogger<DeleteAlarmHandler> _logger;

        public DeleteAlarmHandler(IAlarmRepository alarmRepository, ILogger<DeleteAlarmHandler> logger)
        {
            _alarmRepository = alarmRepository;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteAlarmCommand request, CancellationToken cancellationToken)
        {
            using var activity = SmartAlarmTracing.ActivitySource.StartActivity("DeleteAlarmHandler.Handle");
            activity?.SetTag("alarm.id", request.AlarmId.ToString());
            var existing = await _alarmRepository.GetByIdAsync(request.AlarmId);
            if (existing == null)
            {
                _logger.LogWarning("Alarme não encontrado para exclusão: {AlarmId}", request.AlarmId);
                activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, "Alarm not found");
                return false;
            }
            await _alarmRepository.DeleteAsync(request.AlarmId);
            _logger.LogInformation("Alarme excluído: {AlarmId}", request.AlarmId);
            activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok);
            return true;
        }
    }
}
