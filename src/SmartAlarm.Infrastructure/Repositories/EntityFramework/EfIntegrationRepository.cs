using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Infrastructure.Data;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Logging;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;

namespace SmartAlarm.Infrastructure.Repositories.EntityFramework
{
    /// <summary>
    /// Entity Framework Core implementation of IIntegrationRepository.
    /// </summary>
    public class EfIntegrationRepository : IIntegrationRepository
    {
        private readonly SmartAlarmDbContext _context;
        private readonly ILogger<EfIntegrationRepository> _logger;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;
        private readonly SmartAlarmActivitySource _activitySource;

        public EfIntegrationRepository(
            SmartAlarmDbContext context,
            ILogger<EfIntegrationRepository> logger,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            SmartAlarmActivitySource activitySource)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _meter = meter ?? throw new ArgumentNullException(nameof(meter));
            _correlationContext = correlationContext ?? throw new ArgumentNullException(nameof(correlationContext));
            _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
        }

        public async Task<Integration?> GetByIdAsync(Guid id)
        {
            using var activity = _activitySource.StartActivity("GetIntegrationById");
            activity?.SetTag("integration.id", id.ToString());

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "GetIntegrationById",
                new { Id = id });

            try
            {
                var integration = await _context.Integrations.FindAsync(id);
                stopwatch.Stop();

                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetById", "Integrations");

                _logger.LogInformation(LogTemplates.QueryCompleted,
                    "GetIntegrationById",
                    stopwatch.ElapsedMilliseconds,
                    integration != null ? 1 : 0);

                activity?.SetStatus(ActivityStatusCode.Ok);
                return integration;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Integrations", "QueryError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "GetIntegrationById",
                    "Integrations",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }

        public async Task<IEnumerable<Integration>> GetByAlarmIdAsync(Guid alarmId)
        {
            using var activity = _activitySource.StartActivity("GetIntegrationsByAlarmId");
            activity?.SetTag("alarm.id", alarmId.ToString());

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "GetIntegrationsByAlarmId",
                new { AlarmId = alarmId });

            try
            {
                var integrations = await _context.Integrations
                    .Where(i => i.AlarmId == alarmId)
                    .ToListAsync();
                stopwatch.Stop();

                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetByAlarmId", "Integrations");

                _logger.LogInformation(LogTemplates.QueryCompleted,
                    "GetIntegrationsByAlarmId",
                    stopwatch.ElapsedMilliseconds,
                    integrations.Count());

                activity?.SetStatus(ActivityStatusCode.Ok);
                return integrations;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Integrations", "QueryError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "GetIntegrationsByAlarmId",
                    "Integrations",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }

        public async Task<IEnumerable<Integration>> GetByUserIdAsync(Guid userId)
        {
            using var activity = _activitySource.StartActivity("GetIntegrationsByUserId");
            activity?.SetTag("user.id", userId.ToString());

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "GetIntegrationsByUserId",
                new { UserId = userId });

            try
            {
                // Busca integrações do usuário através dos alarmes
                var integrations = await (from integration in _context.Integrations
                                        join alarm in _context.Alarms on integration.AlarmId equals alarm.Id
                                        where alarm.UserId == userId
                                        select integration).ToListAsync();
                
                stopwatch.Stop();

                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetByUserId", "Integrations");

                _logger.LogInformation(LogTemplates.QueryCompleted,
                    "GetIntegrationsByUserId",
                    stopwatch.ElapsedMilliseconds,
                    integrations.Count());

                activity?.SetStatus(ActivityStatusCode.Ok);
                return integrations;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Integrations", "QueryError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "GetIntegrationsByUserId",
                    "Integrations",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }

        public async Task<IEnumerable<Integration>> GetActiveByUserIdAsync(Guid userId)
        {
            using var activity = _activitySource.StartActivity("GetActiveIntegrationsByUserId");
            activity?.SetTag("user.id", userId.ToString());

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "GetActiveIntegrationsByUserId",
                new { UserId = userId });

            try
            {
                // Busca integrações ativas do usuário através dos alarmes
                var integrations = await (from integration in _context.Integrations
                                        join alarm in _context.Alarms on integration.AlarmId equals alarm.Id
                                        where alarm.UserId == userId && integration.IsActive
                                        select integration).ToListAsync();
                
                stopwatch.Stop();

                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetActiveByUserId", "Integrations");

                _logger.LogInformation(LogTemplates.QueryCompleted,
                    "GetActiveIntegrationsByUserId",
                    stopwatch.ElapsedMilliseconds,
                    integrations.Count());

                activity?.SetStatus(ActivityStatusCode.Ok);
                return integrations;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Integrations", "QueryError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "GetActiveIntegrationsByUserId",
                    "Integrations",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }

        public async Task<IEnumerable<Integration>> GetAllAsync()
        {
            using var activity = _activitySource.StartActivity("GetAllIntegrations");

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "GetAllIntegrations",
                new { });

            try
            {
                var integrations = await _context.Integrations.ToListAsync();
                stopwatch.Stop();

                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "GetAll", "Integrations");

                _logger.LogInformation(LogTemplates.QueryCompleted,
                    "GetAllIntegrations",
                    stopwatch.ElapsedMilliseconds,
                    integrations.Count());

                activity?.SetStatus(ActivityStatusCode.Ok);
                return integrations;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Integrations", "QueryError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "GetAllIntegrations",
                    "Integrations",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }

        public async Task AddAsync(Integration integration)
        {
            using var activity = _activitySource.StartActivity("AddIntegration");
            activity?.SetTag("integration.id", integration.Id.ToString());
            activity?.SetTag("alarm.id", integration.AlarmId.ToString());

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "AddIntegration",
                new { IntegrationId = integration.Id, AlarmId = integration.AlarmId });

            try
            {
                await _context.Integrations.AddAsync(integration);
                stopwatch.Stop();

                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "AddAsync", "Integrations");

                _logger.LogInformation(LogTemplates.QueryCompleted,
                    "AddIntegration",
                    stopwatch.ElapsedMilliseconds,
                    "integration added successfully");

                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Integrations", "InsertError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "AddIntegration",
                    "Integrations",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }

        public Task UpdateAsync(Integration integration)
        {
            using var activity = _activitySource.StartActivity("UpdateIntegration");
            activity?.SetTag("integration.id", integration.Id.ToString());
            activity?.SetTag("alarm.id", integration.AlarmId.ToString());

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "UpdateIntegration",
                new { IntegrationId = integration.Id, AlarmId = integration.AlarmId });

            try
            {
                _context.Integrations.Update(integration);
                stopwatch.Stop();

                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "UpdateAsync", "Integrations");

                _logger.LogInformation(LogTemplates.QueryCompleted,
                    "UpdateIntegration",
                    stopwatch.ElapsedMilliseconds,
                    "integration updated successfully");

                activity?.SetStatus(ActivityStatusCode.Ok);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Integrations", "UpdateError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "UpdateIntegration",
                    "Integrations",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }

        public Task DeleteAsync(Guid id)
        {
            using var activity = _activitySource.StartActivity("DeleteIntegration");
            activity?.SetTag("integration.id", id.ToString());

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(LogTemplates.QueryStarted,
                "DeleteIntegration",
                new { IntegrationId = id });

            try
            {
                var integration = _context.Integrations.Find(id);
                bool wasDeleted = false;
                if (integration != null)
                {
                    _context.Integrations.Remove(integration);
                    wasDeleted = true;
                }

                stopwatch.Stop();
                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "DeleteAsync", "Integrations");

                _logger.LogInformation(LogTemplates.QueryCompleted,
                    "DeleteIntegration",
                    stopwatch.ElapsedMilliseconds,
                    wasDeleted ? "integration deleted successfully" : "integration not found");

                activity?.SetStatus(ActivityStatusCode.Ok);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("DATABASE", "Integrations", "DeleteError");

                _logger.LogError(LogTemplates.DatabaseQueryFailed,
                    "DeleteIntegration",
                    "Integrations",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }
}
