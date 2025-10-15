using Hangfire.Dashboard;

namespace SmartAlarm.Api.Services;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // In development, allow all access
        // In production, this should check for proper authentication/authorization
        var httpContext = context.GetHttpContext();

        // For now, allow access in development environments
        // TODO: Implement proper authorization for production
        return true;
    }
}
