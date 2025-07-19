using System.Security.Claims;

namespace SmartAlarm.Api.Services
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
        string? Email { get; }
        IEnumerable<string> Roles { get; }
        bool IsAuthenticated { get; }
        ClaimsPrincipal Principal { get; }
    }
}
