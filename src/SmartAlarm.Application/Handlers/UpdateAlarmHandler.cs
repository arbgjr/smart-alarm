using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Commands;
using SmartAlarm.Application.DTOs;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using FluentValidation;

namespace SmartAlarm.Application.Handlers
{
    /// <summary>
    /// Handler para atualização de alarme.
    /// </summary>
    public class UpdateAlarmHandler : IRequestHandler<UpdateAlarmCommand, AlarmResponseDto>
    {
        private readonly IAlarmRepository _alarmRepository;
        private readonly IValidator<CreateAlarmDto> _validator;
        private readonly ILogger<UpdateAlarmHandler> _logger;

        public UpdateAlarmHandler(IAlarmRepository alarmRepository, IValidator<CreateAlarmDto> validator, ILogger<UpdateAlarmHandler> logger)
        {
            _alarmRepository = alarmRepository;
            _validator = validator;
            _logger = logger;
        }

        public async Task<AlarmResponseDto> Handle(UpdateAlarmCommand request, CancellationToken cancellationToken)
        {
            using var activity = SmartAlarmTracing.ActivitySource.StartActivity("UpdateAlarmHandler.Handle");
            activity?.SetTag("alarm.id", request.AlarmId.ToString());
            var validationResult = await _validator.ValidateAsync(request.Alarm, cancellationToken);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Falha de validação ao atualizar alarme: {@Errors}", validationResult.Errors);
                activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, "Validation failed");
                SmartAlarmMetrics.ValidationErrorsCounter.Add(1);
                throw new ValidationException(validationResult.Errors.ToString());
            }
            var existing = await _alarmRepository.GetByIdAsync(request.AlarmId);
            if (existing == null)
            {
                _logger.LogWarning("Alarme não encontrado: {AlarmId}", request.AlarmId);
                activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, "Alarm not found");
                SmartAlarmMetrics.NotFoundErrorsCounter.Add(1);
                throw new SmartAlarm.Domain.Exceptions.NotFoundException("Alarm", request.AlarmId);
            }
            var updated = new Alarm(request.AlarmId, request.Alarm.Name, request.Alarm.Time, existing.Enabled, request.Alarm.UserId);
            await _alarmRepository.UpdateAsync(updated);
            _logger.LogInformation("Alarme atualizado: {AlarmId}", updated.Id);
            activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok);
            SmartAlarmMetrics.AlarmsUpdatedCounter.Add(1);
            return new AlarmResponseDto
            {
                Id = updated.Id,
                Name = updated.Name,
                Time = updated.Time,
                Enabled = updated.Enabled,
                UserId = updated.UserId
            };
        }
    }
}
