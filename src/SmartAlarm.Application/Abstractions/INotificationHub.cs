using Microsoft.AspNetCore.SignalR;

namespace SmartAlarm.Application.Abstractions;

/// <summary>
/// Interface for notification hub to avoid circular dependencies
/// </summary>
public interface INotificationHub
{
    Task SendAsync(string method, object? arg1, CancellationToken cancellationToken = default);
    Task SendAsync(string method, object? arg1, object? arg2, CancellationToken cancellationToken = default);
}

/// <summary>
/// Wrapper for IHubContext to provide abstraction
/// </summary>
public interface INotificationHubContext
{
    IClientProxy Clients { get; }
    IGroupManager Groups { get; }
}
