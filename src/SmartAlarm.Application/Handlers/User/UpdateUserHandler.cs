using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Commands.User;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Domain.ValueObjects;
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
    public class UpdateUserHandler : IRequestHandler<UpdateUserCommand, bool>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UpdateUserHandler> _logger;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly BusinessMetrics _businessMetrics;
        private readonly ICorrelationContext _correlationContext;

        public UpdateUserHandler(
            IUserRepository userRepository, 
            ILogger<UpdateUserHandler> logger,
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

        public async Task<bool> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = _correlationContext.CorrelationId;
            
            _logger.LogDebug(LogTemplates.CommandStarted, 
                nameof(UpdateUserCommand), 
                request.Email,
                correlationId);

            using var activity = _activitySource.StartActivity("UpdateUserHandler.Handle");
            activity?.SetTag("user.id", request.Id.ToString());
            activity?.SetTag("user.email", request.Email);
            activity?.SetTag("correlation.id", correlationId);
            activity?.SetTag("operation", "UpdateUser");
            activity?.SetTag("handler", "UpdateUserHandler");
            
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
                
                user.UpdateName(new Name(request.Name));
                user.UpdateEmail(new Email(request.Email));
                await _userRepository.UpdateAsync(user);
                
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Ok);
                activity?.SetTag("user.updated", true);
                
                // Métricas técnicas
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "UpdateUser", "Success", "200");
                
                // Métricas de negócio
                _businessMetrics.RecordAlarmProcessingTime(stopwatch.ElapsedMilliseconds, "user", "update");
                
                _logger.LogDebug(LogTemplates.CommandCompleted, 
                    nameof(UpdateUserCommand), 
                    stopwatch.ElapsedMilliseconds,
                    "Success");

                _logger.LogInformation(LogTemplates.BusinessEventOccurred,
                    "UserUpdated",
                    new { UserId = user.Id, Email = request.Email, Name = request.Name },
                    correlationId);

                return true;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("COMMAND", "Users", "UpdateError");
                
                _logger.LogError(LogTemplates.CommandFailed,
                    nameof(UpdateUserCommand),
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                
                throw;
            }
        }
    }
}
