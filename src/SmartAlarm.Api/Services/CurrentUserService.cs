using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace SmartAlarm.Api.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? UserId => Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Principal?.FindFirst("sub")?.Value;
        public string? Email => Principal?.FindFirst(ClaimTypes.Email)?.Value;
        public IEnumerable<string> Roles => Principal?.FindAll(ClaimTypes.Role).Select(r => r.Value) ?? Enumerable.Empty<string>();
        public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;
        public ClaimsPrincipal Principal => _httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();

        public Guid GetUserId()
        {
            var userIdClaim = UserId;
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            // Lançar exceção ou retornar um valor padrão, dependendo da política de segurança.
            // Para endpoints protegidos por [Authorize], uma exceção é apropriada.
            throw new UnauthorizedAccessException("Não foi possível identificar o usuário autenticado.");
        }
    }
}
