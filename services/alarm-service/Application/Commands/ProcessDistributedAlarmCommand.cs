using MediatR;
using SmartAlarm.AlarmService.Infrastructure.DistributedProcessing;
using SmartAlarm.AlarmService.Infrastructure.Queues;
using SmartAlarm.AlarmService.Infrastructure.Metrics;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Tracing;
using FluentValidation;
using System.Diagnostics;

namespace SmartAlarm.AlarmService.Application.Commands
{
    /// <summary>
    /// Command para processar alarme usando sistema distribuído
    /// </summary>
    public record ProcessDistributedAlarmCommand(
        Guid AlarmId,
        Guid UserId,
        string TriggerType = "scheduled",
        bool UseQueue = true,
        TimeSpan? Delay = null
    ) : IRequest<ProcessDistributedAlarmResponse>;

    /// <summary>
    /// Response do processamento distribuído
    /// </summary>
    public record ProcessDistributedAlarmResponse(
        Guid AlarmId,
        Guid UserId,
        bool Success,
        string Message,
        string? JobId = null,
        AlarmProcessingResult? ProcessingResult = null,
        DateTime ProcessedAt = default
    );


}
