using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Commands.User;
using SmartAlarm.Domain.Entities;
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
    public class CreateUserHandler : IRequestHandler<CreateUserCommand, Guid>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<CreateUserHandler> _logger;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly BusinessMetrics _businessMetrics;
        private readonly ICorrelationContext _correlationContext;

        public CreateUserHandler(
            IUserRepository userRepository, 
            ILogger<CreateUserHandler> logger,
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

        public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = _correlationContext.CorrelationId;
            
            _logger.LogDebug(LogTemplates.CommandStarted, 
                nameof(CreateUserCommand), 
                request.User.Email,
                correlationId);

            using var activity = _activitySource.StartActivity("CreateUserHandler.Handle");
            activity?.SetTag("user.email", request.User.Email);
            activity?.SetTag("user.name", request.User.Name);
            activity?.SetTag("correlation.id", correlationId);
            activity?.SetTag("operation", "CreateUser");
            activity?.SetTag("handler", "CreateUserHandler");
            
            try
            {
                var user = new Domain.Entities.User(
                    Guid.NewGuid(),
                    new Name(request.User.Name),
                    new Email(request.User.Email)
                );
                
                await _userRepository.AddAsync(user);
                
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Ok);
                activity?.SetTag("user.id", user.Id.ToString());
                activity?.SetTag("user.created", true);
                
                // Métricas técnicas
                _meter.RecordRequestDuration(stopwatch.ElapsedMilliseconds, "CreateUser", "Success", "201");
                
                // Métricas de negócio
                _businessMetrics.UpdateUsersActiveToday(1);
                _businessMetrics.RecordAlarmProcessingTime(stopwatch.ElapsedMilliseconds, "user", "creation");
                
                _logger.LogDebug(LogTemplates.CommandCompleted, 
                    nameof(CreateUserCommand), 
                    stopwatch.ElapsedMilliseconds,
                    "Success");

                _logger.LogInformation(LogTemplates.BusinessEventOccurred,
                    "UserCreated",
                    new { UserId = user.Id, Email = request.User.Email, Name = request.User.Name },
                    correlationId);

                return user.Id;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("COMMAND", "Users", "CreateError");
                
                _logger.LogError(LogTemplates.CommandFailed,
                    nameof(CreateUserCommand),
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                
                throw;
            }
        }
    }
}
