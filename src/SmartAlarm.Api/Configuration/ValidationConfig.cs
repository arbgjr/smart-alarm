using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;

namespace SmartAlarm.Api.Configuration
{
    public static class ValidationConfig
    {
        public static IServiceCollection AddFluentValidationConfig(this IServiceCollection services)
        {
            services.AddFluentValidationAutoValidation();
            return services;
        }
    }
}
