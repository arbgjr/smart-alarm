using Fido2NetLib;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartAlarm.Domain.Abstractions;
using SmartAlarm.Infrastructure.Security;

namespace SmartAlarm.Infrastructure.Extensions;

/// <summary>
/// Extensões para configuração do FIDO2/WebAuthn
/// Seguindo padrões de DI e configuração do .NET Core
/// </summary>
public static class Fido2ServiceExtensions
{
    public static IServiceCollection AddFido2Services(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        // Configuração do FIDO2
        var fido2Config = new Fido2Configuration
        {
            ServerDomain = configuration["Fido2:ServerDomain"] ?? "localhost",
            ServerName = configuration["Fido2:ServerName"] ?? "Smart Alarm",
            Origins = configuration.GetSection("Fido2:Origins").Get<HashSet<string>>() 
                      ?? new HashSet<string> { "https://localhost:7042" },
            TimestampDriftTolerance = int.Parse(configuration["Fido2:TimestampDriftTolerance"] ?? "300000")
        };

        // Registrar IFido2 como singleton com a configuração
        services.AddSingleton<IFido2>(provider => new Fido2(fido2Config));

        // Registrar o serviço de FIDO2
        services.AddScoped<IFido2Service, SimpleFido2Service>();

        return services;
    }
}
