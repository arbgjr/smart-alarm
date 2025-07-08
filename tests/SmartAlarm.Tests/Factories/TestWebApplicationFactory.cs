using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SmartAlarm.Api.Services;
using SmartAlarm.Tests.Mocks;

namespace SmartAlarm.Tests.Factories
{
    public class TestWebApplicationFactory : WebApplicationFactory<SmartAlarm.Api.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Substituir o CurrentUserService real pelo mock de teste
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICurrentUserService));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                
                services.AddScoped<ICurrentUserService, TestCurrentUserService>();
            });
        }
    }
}
