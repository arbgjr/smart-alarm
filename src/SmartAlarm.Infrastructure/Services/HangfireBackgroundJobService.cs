using System.Linq.Expressions;
using Hangfire;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Abstractions;

namespace SmartAlarm.Infrastructure.Services;

public class HangfireBackgroundJobService : IBackgroundJobService
{
    private readonly ILogger<HangfireBackgroundJobService> _logger;

    public HangfireBackgroundJobService(ILogger<HangfireBackgroundJobService> logger)
    {
        _logger = logger;
    }

    public string ScheduleJob(Expression<Func<Task>> methodCall, DateTimeOffset enqueueAt)
    {
        try
        {
            var jobId = BackgroundJob.Schedule(methodCall, enqueueAt);
            _logger.LogInformation("Scheduled background job {JobId} to run at {EnqueueAt}", jobId, enqueueAt);
            return jobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule background job");
            throw;
        }
    }

    public string ScheduleJob<T>(Expression<Func<T, Task>> methodCall, DateTimeOffset enqueueAt)
    {
        try
        {
            var jobId = BackgroundJob.Schedule(methodCall, enqueueAt);
            _logger.LogInformation("Scheduled background job {JobId} to run at {EnqueueAt}", jobId, enqueueAt);
            return jobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule background job");
            throw;
        }
    }

    public string EnqueueJob(Expression<Func<Task>> methodCall)
    {
        try
        {
            var jobId = BackgroundJob.Enqueue(methodCall);
            _logger.LogInformation("Enqueued background job {JobId}", jobId);
            return jobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue background job");
            throw;
        }
    }

    public string EnqueueJob<T>(Expression<Func<T, Task>> methodCall)
    {
        try
        {
            var jobId = BackgroundJob.Enqueue(methodCall);
            _logger.LogInformation("Enqueued background job {JobId}", jobId);
            return jobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue background job");
            throw;
        }
    }

    public string ScheduleRecurringJob(string jobId, Expression<Func<Task>> methodCall, string cronExpression)
    {
        try
        {
            RecurringJob.AddOrUpdate(jobId, methodCall, cronExpression);
            _logger.LogInformation("Scheduled recurring job {JobId} with cron expression {CronExpression}", jobId, cronExpression);
            return jobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule recurring job {JobId}", jobId);
            throw;
        }
    }

    public string ScheduleRecurringJob<T>(string jobId, Expression<Func<T, Task>> methodCall, string cronExpression)
    {
        try
        {
            RecurringJob.AddOrUpdate(jobId, methodCall, cronExpression);
            _logger.LogInformation("Scheduled recurring job {JobId} with cron expression {CronExpression}", jobId, cronExpression);
            return jobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule recurring job {JobId}", jobId);
            throw;
        }
    }

    public bool DeleteJob(string jobId)
    {
        try
        {
            var result = BackgroundJob.Delete(jobId);
            _logger.LogInformation("Deleted background job {JobId}: {Result}", jobId, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete background job {JobId}", jobId);
            throw;
        }
    }

    public void RemoveRecurringJob(string jobId)
    {
        try
        {
            RecurringJob.RemoveIfExists(jobId);
            _logger.LogInformation("Removed recurring job {JobId}", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove recurring job {JobId}", jobId);
            throw;
        }
    }
}
