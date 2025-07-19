using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.DTOs.Routine;
using SmartAlarm.Application.Queries.Routine;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Observability.Logging;
using SmartAlarm.Observability.Metrics;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmartAlarm.Application.Handlers.Routine
{
    public class ListRoutinesHandler : IRequestHandler<ListRoutinesQuery, List<RoutineResponseDto>>
    {
        private readonly IRoutineRepository _routineRepository;
        private readonly IAlarmRepository _alarmRepository;
        private readonly ActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly BusinessMetrics _businessMetrics;
        private readonly ILogger<ListRoutinesHandler> _logger;

        public ListRoutinesHandler(
            IRoutineRepository routineRepository,
            IAlarmRepository alarmRepository,
            ActivitySource activitySource,
            SmartAlarmMeter meter,
            BusinessMetrics businessMetrics,
            ILogger<ListRoutinesHandler> logger)
        {
            _routineRepository = routineRepository;
            _alarmRepository = alarmRepository;
            _activitySource = activitySource;
            _meter = meter;
            _businessMetrics = businessMetrics;
            _logger = logger;
        }

        public async Task<List<RoutineResponseDto>> Handle(ListRoutinesQuery request, CancellationToken cancellationToken)
        {
            using var activity = _activitySource.StartActivity("ListRoutines");
            var stopwatch = Stopwatch.StartNew();

            activity?.SetTag("operation", "ListRoutines");
            activity?.SetTag("alarm.id", request.AlarmId?.ToString() ?? "All");
            activity?.SetTag("user.id", request.UserId?.ToString() ?? "All");

            _logger.LogDebug(LogTemplates.QueryStarted, "ListRoutines", "Routine");

            try
            {
                IEnumerable<Domain.Entities.Routine> routines;
                
                if (request.AlarmId.HasValue)
                {
                    // Buscar rotinas específicas de um alarme
                    routines = await _routineRepository.GetByAlarmIdAsync(request.AlarmId.Value);
                }
                else
                {
                    // Buscar todas as rotinas se AlarmId não for especificado
                    routines = await _routineRepository.GetAllAsync();
                }

                // Se UserId for especificado, filtrar por usuário através dos alarmes
                if (request.UserId.HasValue)
                {
                    var userAlarms = await _alarmRepository.GetByUserIdAsync(request.UserId.Value);
                    var userAlarmIds = userAlarms.Select(a => a.Id).ToHashSet();
                    routines = routines.Where(r => userAlarmIds.Contains(r.AlarmId));
                }

                var routineList = routines.ToList();
                var activeRoutines = routineList.Count(r => r.IsActive);

                stopwatch.Stop();
                var duration = stopwatch.ElapsedMilliseconds;

                activity?.SetTag("routines.count", routineList.Count.ToString());
                activity?.SetTag("routines.active", activeRoutines.ToString());

                _meter.RecordRequestDuration(duration, "ListRoutines", "Success", "200");
                _logger.LogInformation(LogTemplates.QueryCompleted, "ListRoutines", duration, routineList.Count);

                return routineList.Select(r => new RoutineResponseDto
                {
                    Id = r.Id,
                    Name = r.Name?.ToString() ?? string.Empty,
                    AlarmId = r.AlarmId,
                    Actions = r.Actions?.ToList() ?? new List<string>(),
                    IsActive = r.IsActive,
                    CreatedAt = r.CreatedAt
                }).ToList();
            }
            catch (System.Exception ex)
            {
                stopwatch.Stop();
                var duration = stopwatch.ElapsedMilliseconds;

                activity?.SetTag("error", "true");
                activity?.SetTag("error.type", ex.GetType().Name);

                _meter.IncrementErrorCount("Query", "Routine", ex.GetType().Name);
                _meter.RecordRequestDuration(duration, "ListRoutines", "Error", "500");

                _logger.LogError(LogTemplates.QueryFailed, "ListRoutines", ex.Message, ex);

                throw;
            }
        }
    }
}
