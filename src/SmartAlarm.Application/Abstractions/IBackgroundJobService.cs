using System.Linq.Expressions;

namespace SmartAlarm.Application.Abstractions;

public interface IBackgroundJobService
{
    string ScheduleJob(Expression<Func<Task>> methodCall, DateTimeOffset enqueueAt);
    string ScheduleJob<T>(Expression<Func<T, Task>> methodCall, DateTimeOffset enqueueAt);
    string EnqueueJob(Expression<Func<Task>> methodCall);
    string EnqueueJob<T>(Expression<Func<T, Task>> methodCall);
    string ScheduleRecurringJob(string jobId, Expression<Func<Task>> methodCall, string cronExpression);
    string ScheduleRecurringJob<T>(string jobId, Expression<Func<T, Task>> methodCall, string cronExpression);
    bool DeleteJob(string jobId);
    void RemoveRecurringJob(string jobId);
}
