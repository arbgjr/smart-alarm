namespace SmartAlarm.Api.Configuration;

/// <summary>
/// Configuração de CORS para o Smart Alarm
/// </summary>
public static class CorsConfiguration
{
    public const string DefaultPolicyName = "SmartAlarmCorsPolicy";

    /// <summary>
    /// Configura CORS de forma segura
    /// </summary>
    public static IServiceCollection AddSmartAlarmCors(this IServiceCollection services, IConfiguration configuration)
    {
        var corsSettings = configuration.GetSection("Cors").Get<CorsSettings>() ?? new CorsSettings();

        services.AddCors(options =>
        {
            options.AddPolicy(DefaultPolicyName, policy =>
            {
                // Configurar origens permitidas
                if (corsSettings.AllowedOrigins?.Any() == true)
                {
                    policy.WithOrigins(corsSettings.AllowedOrigins.ToArray());
                }
                else
                {
                    // Em desenvolvimento, permitir localhost
                    policy.WithOrigins(
                        "http://localhost:3000",
                        "http://localhost:5173", // Vite dev server
                        "https://localhost:3000",
                        "https://localhost:5173"
                    );
                }

                // Configurar métodos HTTP permitidos
                if (corsSettings.AllowedMethods?.Any() == true)
                {
                    policy.WithMethods(corsSettings.AllowedMethods.ToArray());
                }
                else
                {
                    policy.WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS", "PATCH");
                }

                // Configurar headers permitidos
                if (corsSettings.AllowedHeaders?.Any() == true)
                {
                    policy.WithHeaders(corsSettings.AllowedHeaders.ToArray());
                }
                else
                {
                    policy.WithHeaders(
                        "Authorization",
                        "Content-Type",
                        "Accept",
                        "Origin",
                        "X-Requested-With",
                        "X-Correlation-ID",
                        "X-Client-Version"
                    );
                }

                // Configurar headers expostos
                if (corsSettings.ExposedHeaders?.Any() == true)
                {
                    policy.WithExposedHeaders(corsSettings.ExposedHeaders.ToArray());
                }
                else
                {
                    policy.WithExposedHeaders(
                        "X-Correlation-ID",
                        "X-RateLimit-Remaining",
                        "X-RateLimit-Reset",
                        "X-Total-Count"
                    );
                }

                // Configurar credenciais
                if (corsSettings.AllowCredentials)
                {
                    policy.AllowCredentials();
                }

                // Configurar preflight cache
                policy.SetPreflightMaxAge(TimeSpan.FromMinutes(corsSettings.PreflightMaxAgeMinutes));

                // Configurar política de origem para SignalR
                policy.SetIsOriginAllowed(origin =>
                {
                    if (string.IsNullOrEmpty(origin))
                        return false;

                    // Permitir origens configuradas
                    if (corsSettings.AllowedOrigins?.Contains(origin) == true)
                        return true;

                    // Em desenvolvimento, permitir localhost
                    if (corsSettings.AllowDevelopmentOrigins)
                    {
                        var uri = new Uri(origin);
                        return uri.Host == "localhost" || uri.Host == "127.0.0.1";
                    }

                    return false;
                });
            });

            // Política restritiva para endpoints administrativos
            options.AddPolicy("AdminPolicy", policy =>
            {
                policy.WithOrigins(corsSettings.AdminAllowedOrigins?.ToArray() ?? Array.Empty<string>())
                      .WithMethods("GET", "POST")
                      .WithHeaders("Authorization", "Content-Type")
                      .AllowCredentials();
            });

            // Política para APIs públicas (se houver)
            options.AddPolicy("PublicApiPolicy", policy =>
            {
                policy.AllowAnyOrigin()
                      .WithMethods("GET")
                      .WithHeaders("Content-Type", "Accept");
            });
        });

        return services;
    }
}

/// <summary>
/// Configurações de CORS
/// </summary>
public class CorsSettings
{
    public const string SectionName = "Cors";

    /// <summary>
    /// Origens permitidas
    /// </summary>
    public List<string> AllowedOrigins { get; set; } = new();

    /// <summary>
    /// Métodos HTTP permitidos
    /// </summary>
    public List<string> AllowedMethods { get; set; } = new();

    /// <summary>
    /// Headers permitidos
    /// </summary>
    public List<string> AllowedHeaders { get; set; } = new();

    /// <summary>
    /// Headers expostos ao cliente
    /// </summary>
    public List<string> ExposedHeaders { get; set; } = new();

    /// <summary>
    /// Permitir credenciais
    /// </summary>
    public bool AllowCredentials { get; set; } = true;

    /// <summary>
    /// Tempo de cache do preflight em minutos
    /// </summary>
    public int PreflightMaxAgeMinutes { get; set; } = 30;

    /// <summary>
    /// Permitir origens de desenvolvimento (localhost)
    /// </summary>
    public bool AllowDevelopmentOrigins { get; set; } = true;

    /// <summary>
    /// Origens permitidas para endpoints administrativos
    /// </summary>
    public List<string> AdminAllowedOrigins { get; set; } = new();
}
