using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.DTOs.User;
using SmartAlarm.Application.Queries.User;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Logging;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmartAlarm.Application.Handlers.User
{
    public class ListUsersHandler : IRequestHandler<ListUsersQuery, List<UserResponseDto>>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<ListUsersHandler> _logger;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly BusinessMetrics _businessMetrics;
        private readonly ICorrelationContext _correlationContext;

        public ListUsersHandler(
            IUserRepository userRepository,
            ILogger<ListUsersHandler> logger,
            SmartAlarmActivitySource activitySource,
            SmartAlarmMeter meter,
            BusinessMetrics businessMetrics,
            ICorrelationContext correlationContext)
        {
            _userRepository = userRepository;
            _logger = logger;
            _activitySource = activitySource;
            _meter = meter;
            _businessMetrics = businessMetrics;
            _correlationContext = correlationContext;
        }

        public async Task<List<UserResponseDto>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = _correlationContext.CorrelationId;
            
            _logger.LogDebug(LogTemplates.QueryStarted, 
                nameof(ListUsersQuery), 
                new { });

            using var activity = _activitySource.StartActivity("ListUsersHandler.Handle");
            activity?.SetTag("correlation.id", correlationId);
            activity?.SetTag("operation", "ListUsers");
            activity?.SetTag("handler", "ListUsersHandler");
            
            try
            {
                var users = await _userRepository.GetAllAsync();
                var result = users.Select(u => new UserResponseDto
                {
                    Id = u.Id,
                    Name = u.Name.ToString(),
                    Email = u.Email.ToString(),
                    IsActive = u.IsActive
                }).ToList();
                
                stopwatch.Stop();
                activity?.SetTag("users.count", result.Count);
                activity?.SetTag("users.active", result.Count(u => u.IsActive));
                activity?.SetStatus(ActivityStatusCode.Ok);
                
                // Métricas técnicas
                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "ListUsers", "Users");
                
                // Métricas de negócio
                _businessMetrics.UpdateUsersActiveToday(result.Count(u => u.IsActive));
                
                _logger.LogDebug(LogTemplates.QueryCompleted, 
                    nameof(ListUsersQuery), 
                    stopwatch.ElapsedMilliseconds,
                    result.Count);

                _logger.LogInformation(LogTemplates.BusinessEventOccurred,
                    "UsersListed",
                    new { Count = result.Count, ActiveCount = result.Count(u => u.IsActive) },
                    correlationId);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("QUERY", "Users", "ListError");
                
                _logger.LogError(LogTemplates.QueryFailed,
                    nameof(ListUsersQuery),
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                
                throw;
            }
        }
    }
}
