using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartAlarm.Api.Services;
using SmartAlarm.Tests.Mocks;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace SmartAlarm.Tests.Factories
{
    public class TestWebApplicationFactory : WebApplicationFactory<SmartAlarm.Api.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Substituir o CurrentUserService real pelo mock de teste
                var descriptors = services.Where(d => d.ServiceType == typeof(ICurrentUserService)).ToList();
                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }
                
                // Remover todos os outros serviços relacionados ao CurrentUserService
                var currentUserDescriptors = services.Where(d => d.ImplementationType == typeof(CurrentUserService)).ToList();
                foreach (var descriptor in currentUserDescriptors)
                {
                    services.Remove(descriptor);
                }
                
                services.AddScoped<ICurrentUserService, TestCurrentUserService>();
                
                // Adicionar o MockJwtTokenService para evitar dependências não resolvidas
                var jwtDescriptors = services.Where(d => d.ServiceType == typeof(SmartAlarm.Domain.Abstractions.IJwtTokenService)).ToList();
                foreach (var desc in jwtDescriptors)
                {
                    services.Remove(desc);
                }
                services.AddSingleton<SmartAlarm.Domain.Abstractions.IJwtTokenService, MockJwtTokenService>();
                
                // Remover autenticação JWT e configurar autenticação de teste
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", options => { });
            });
        }
    }

    public class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger, UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "12345678-1234-1234-1234-123456789012"),
                new Claim(ClaimTypes.Email, "test@example.com"),
                new Claim(ClaimTypes.Role, "User")
            };

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
