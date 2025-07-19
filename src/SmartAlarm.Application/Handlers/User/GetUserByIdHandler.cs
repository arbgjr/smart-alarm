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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SmartAlarm.Application.Handlers.User
{
    public class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, UserResponseDto?>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetUserByIdHandler> _logger;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly BusinessMetrics _businessMetrics;
        private readonly ICorrelationContext _correlationContext;

        public GetUserByIdHandler(
            IUserRepository userRepository,
            ILogger<GetUserByIdHandler> logger,
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

        public async Task<UserResponseDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = _correlationContext.CorrelationId;
            
            _logger.LogDebug(LogTemplates.QueryStarted, 
                nameof(GetUserByIdQuery), 
                new { UserId = request.Id });

            using var activity = _activitySource.StartActivity("GetUserByIdHandler.Handle");
            activity?.SetTag("user.id", request.Id.ToString());
            activity?.SetTag("correlation.id", correlationId);
            activity?.SetTag("operation", "GetUserById");
            activity?.SetTag("handler", "GetUserByIdHandler");
            
            try
            {
                var user = await _userRepository.GetByIdAsync(request.Id);
                
                if (user == null)
                {
                    stopwatch.Stop();
                    _meter.IncrementErrorCount("QUERY", "Users", "NotFound");
                    
                    _logger.LogDebug(LogTemplates.QueryCompleted, 
                        nameof(GetUserByIdQuery), 
                        stopwatch.ElapsedMilliseconds, 
                        0);
                        
                    activity?.SetStatus(ActivityStatusCode.Ok);
                    activity?.SetTag("record.found", false);
                    return null;
                }

                stopwatch.Stop();
                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetUserById", "Users");
                
                _logger.LogDebug(LogTemplates.QueryCompleted, 
                    nameof(GetUserByIdQuery), 
                    stopwatch.ElapsedMilliseconds,
                    1);
                    
                activity?.SetStatus(ActivityStatusCode.Ok);
                activity?.SetTag("record.found", true);
                activity?.SetTag("user.active", user.IsActive);
                
                return new UserResponseDto
                {
                    Id = user.Id,
                    Name = user.Name.ToString(),
                    Email = user.Email.ToString(),
                    IsActive = user.IsActive
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _meter.IncrementErrorCount("QUERY", "Users", "QueryError");
                
                _logger.LogError(LogTemplates.QueryFailed, 
                    nameof(GetUserByIdQuery), 
                    stopwatch.ElapsedMilliseconds, 
                    ex.Message);
                    
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }
    }
}
