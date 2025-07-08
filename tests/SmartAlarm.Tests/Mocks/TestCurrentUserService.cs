using System.Security.Claims;
using SmartAlarm.Api.Services;

namespace SmartAlarm.Tests.Mocks
{
    public class TestCurrentUserService : ICurrentUserService
    {
        public string? UserId { get; set; } = "12345678-1234-1234-1234-123456789012";
        public string? Email { get; set; } = "test@example.com";
        public IEnumerable<string> Roles { get; set; } = new[] { "User" };
        public bool IsAuthenticated { get; set; } = true;
        public ClaimsPrincipal Principal { get; set; } = new ClaimsPrincipal();
        
        public TestCurrentUserService()
        {
            // Criar um principal com as claims necessárias
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, UserId!),
                new Claim(ClaimTypes.Email, Email!),
                new Claim(ClaimTypes.Role, "User")
            };
            
            var identity = new ClaimsIdentity(claims, "Test");
            Principal = new ClaimsPrincipal(identity);
        }
    }
}
