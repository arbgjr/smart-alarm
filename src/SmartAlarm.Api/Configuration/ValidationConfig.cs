using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using SmartAlarm.Api.Services;
using SmartAlarm.Application.Behaviors;
using SmartAlarm.Application.Validators;
using MediatR;

namespace SmartAlarm.Api.Configuration
{
    public static class ValidationConfig
    {
        public static IServiceCollection AddFluentValidationConfig(this IServiceCollection services)
        {
            // Registrar FluentValidation
            services.AddFluentValidationAutoValidation();
            services.AddFluentValidationClientsideAdapters();

            // Registrar todos os validadores automaticamente
            services.AddValidatorsFromAssemblyContaining<CreateAlarmDtoValidator>();

            // Registrar comportamento de validação no pipeline do MediatR
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            // Registrar serviço de mensagens de erro
            services.AddSingleton<IErrorMessageService, ErrorMessageService>();

            return services;
        }
    }
}
