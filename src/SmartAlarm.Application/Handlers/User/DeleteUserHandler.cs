using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Commands.User;
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
    public class DeleteUserHandler : IRequestHandler<DeleteUserCommand, bool>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<DeleteUserHandler> _logger;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly BusinessMetrics _businessMetrics;
        private readonly ICorrelationContext _correlationContext;

        public DeleteUserHandler(
            IUserRepository userRepository, 
            ILogger<DeleteUserHandler> logger,
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

        public async Task<bool> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = _correlationContext.CorrelationId;
            
            _logger.LogDebug(LogTemplates.CommandStarted, 
                nameof(DeleteUserCommand), 
                request.Id,
                correlationId);

            using var activity = _activitySource.StartActivity("DeleteUserHandler.Handle");
            activity?.SetTag("user.id", request.Id.ToString());
            activity?.SetTag("correlation.id", correlationId);
            activity?.SetTag("operation", "DeleteUser");
            activity?.SetTag("handler", "DeleteUserHandler");
            
            try
            {
                var user = await _userRepository.GetByIdAsync(request.Id);
                if (user == null)
                {
                    stopwatch.Stop();
                    activity?.SetStatus(ActivityStatusCode.Error, "User not found");
                    _meter.IncrementErrorCount("COMMAND", "Users", "NotFound");
                    
                    _logger.LogWarning(LogTemplates.EntityNotFound,
                        "User",
                        request.Id,
                        correlationId);
                    
                    return false;
                }
                
                await _userRepository.DeleteAsync(request.Id);
                
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Ok);
                activity?.SetTag("user.deleted", true);
                
                // Métricas técnicas
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "DeleteUser", "Success", "200");
                
                // Métricas de negócio
                _businessMetrics.RecordAlarmProcessingTime(stopwatch.ElapsedMilliseconds, "user", "deletion");
                
                _logger.LogDebug(LogTemplates.CommandCompleted, 
                    nameof(DeleteUserCommand), 
                    stopwatch.ElapsedMilliseconds,
                    "Success");

                _logger.LogInformation(LogTemplates.BusinessEventOccurred,
                    "UserDeleted",
                    new { UserId = request.Id, UserEmail = user.Email.ToString() },
                    correlationId);

                return true;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("COMMAND", "Users", "DeleteError");
                
                _logger.LogError(LogTemplates.CommandFailed,
                    nameof(DeleteUserCommand),
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                
                throw;
            }
        }
    }
}
