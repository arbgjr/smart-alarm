using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.DTOs.Routine;
using SmartAlarm.Application.Queries.Routine;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Observability.Logging;
using SmartAlarm.Observability.Metrics;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SmartAlarm.Application.Handlers.Routine
{
    public class GetRoutineByIdHandler : IRequestHandler<GetRoutineByIdQuery, RoutineResponseDto>
    {
        private readonly IRoutineRepository _routineRepository;
        private readonly ActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly BusinessMetrics _businessMetrics;
        private readonly ILogger<GetRoutineByIdHandler> _logger;

        public GetRoutineByIdHandler(
            IRoutineRepository routineRepository,
            ActivitySource activitySource,
            SmartAlarmMeter meter,
            BusinessMetrics businessMetrics,
            ILogger<GetRoutineByIdHandler> logger)
        {
            _routineRepository = routineRepository;
            _activitySource = activitySource;
            _meter = meter;
            _businessMetrics = businessMetrics;
            _logger = logger;
        }

        public async Task<RoutineResponseDto> Handle(GetRoutineByIdQuery request, CancellationToken cancellationToken)
        {
            using var activity = _activitySource.StartActivity("GetRoutineById");
            var stopwatch = Stopwatch.StartNew();

            activity?.SetTag("operation", "GetRoutineById");
            activity?.SetTag("routine.id", request.Id.ToString());

            _logger.LogDebug(LogTemplates.QueryStarted, "GetRoutineById", "Routine");

            try
            {
                var routine = await _routineRepository.GetByIdAsync(request.Id);
                
                stopwatch.Stop();
                var duration = stopwatch.ElapsedMilliseconds;

                if (routine == null)
                {
                    activity?.SetTag("record.found", "false");
                    _meter.RecordRequestDuration(duration, "GetRoutineById", "NotFound", "404");
                    _logger.LogInformation(LogTemplates.QueryCompleted, "GetRoutineById", duration, 0);
                    return null!;
                }

                activity?.SetTag("record.found", "true");
                activity?.SetTag("routine.active", routine.IsActive.ToString());

                _meter.RecordRequestDuration(duration, "GetRoutineById", "Success", "200");
                _logger.LogInformation(LogTemplates.QueryCompleted, "GetRoutineById", duration, 1);

                return new RoutineResponseDto
                {
                    Id = routine.Id,
                    Name = routine.Name?.ToString() ?? string.Empty,
                    AlarmId = routine.AlarmId,
                    Actions = routine.Actions?.ToList() ?? new List<string>(),
                    IsActive = routine.IsActive,
                    CreatedAt = routine.CreatedAt
                };
            }
            catch (System.Exception ex)
            {
                stopwatch.Stop();
                var duration = stopwatch.ElapsedMilliseconds;

                activity?.SetTag("error", "true");
                activity?.SetTag("error.type", ex.GetType().Name);

                _meter.IncrementErrorCount("Query", "Routine", ex.GetType().Name);
                _meter.RecordRequestDuration(duration, "GetRoutineById", "Error", "500");

                _logger.LogError(LogTemplates.QueryFailed, "GetRoutineById", ex.Message, ex);

                throw;
            }
        }
    }
}
