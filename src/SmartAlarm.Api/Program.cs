using Serilog;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using SmartAlarm.Api.Middleware;
using SmartAlarm.Api.Configuration;
using SmartAlarm.KeyVault.Extensions;
using SmartAlarm.Infrastructure;
using SmartAlarm.Infrastructure.Extensions;
using SmartAlarm.Application.Behaviors;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using MediatR;
using FluentValidation;
using System.Reflection;
using SmartAlarm.Observability.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to remove Server header
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
});

// Configure services to suppress server header completely
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.IHttpResponseFeature>(options =>
{
    // This will be handled by middleware
});

// Add Observability
builder.Services.AddObservability(builder.Configuration, "SmartAlarm.Api", "1.0.0");

// Configuração do Serilog
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.File("logs/smartalarm-api.log", rollingInterval: RollingInterval.Day, outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}");
});

// Registrar MediatR apontando para a Application Layer
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(SmartAlarm.Application.Commands.CreateAlarmCommand).Assembly));

// Registrar infraestrutura (repositories e serviços) - evitar em ambiente de teste
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddSmartAlarmInfrastructure(builder.Configuration);
}

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers();

// Sobrescreve a resposta padrão de erro de modelo inválido
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var traceId = context.HttpContext.TraceIdentifier;
        var timestamp = DateTime.UtcNow;
        var errors = context.ModelState
            .Where(e => e.Value != null && e.Value.Errors != null && e.Value.Errors.Count > 0)
            .Select(e => new SmartAlarm.Api.Models.ValidationError
            {
                Field = e.Key,
                Message = e.Value?.Errors?.FirstOrDefault()?.ErrorMessage ?? "Campo inválido.",
                Code = "NotEmptyValidator", // Pode ser aprimorado para mapear o código correto
                AttemptedValue = e.Value?.AttemptedValue
            })
            .ToList();

        var errorResponse = new SmartAlarm.Api.Models.ErrorResponse
        {
            StatusCode = 400,
            Title = "Erro de validação",
            Detail = "Um ou mais campos estão inválidos.",
            Type = "ValidationError",
            TraceId = traceId,
            Timestamp = timestamp,
            ValidationErrors = errors
        };
        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(errorResponse);
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "SmartAlarm API", Version = "v1" });
    options.EnableAnnotations();
    // Remover IncludeXmlComments se não houver arquivo XML
});

// Configurar validação com FluentValidation
builder.Services.AddFluentValidationConfig();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "Bearer";
    options.DefaultChallengeScheme = "Bearer";
})
.AddJwtBearer("Bearer", options =>
{
    var jwtSettings = builder.Configuration.GetSection("Jwt");
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured"))),
        ClockSkew = TimeSpan.FromMinutes(2)
    };
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<SmartAlarm.Api.Services.ICurrentUserService, SmartAlarm.Api.Services.CurrentUserService>();

// Add Rate Limiting (disabled in testing environments)
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddRateLimiter(options =>
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                factory: partition => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 10, // Limite mais baixo para detectar nos testes
                    Window = TimeSpan.FromMinutes(1)
                }));
        
        options.OnRejected = async (context, token) =>
        {
            context.HttpContext.Response.StatusCode = 429;
            context.HttpContext.Response.Headers["Retry-After"] = "60";
            await context.HttpContext.Response.WriteAsync("Too Many Requests", token);
        };
    });
}


// LGPD: Serviço de consentimento do usuário
builder.Services.AddSingleton<SmartAlarm.Api.Services.IUserConsentService, SmartAlarm.Api.Services.UserConsentService>();

// Configure KeyVault services

// Registra IConfigurationResolver para injeção de dependência
builder.Services.AddScoped<SmartAlarm.Infrastructure.Configuration.IConfigurationResolver, SmartAlarm.Infrastructure.Configuration.ConfigurationResolver>();
builder.Services.AddKeyVault(builder.Configuration);

// Configure Infrastructure services
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddSmartAlarmInfrastructure(builder.Configuration);
}

// Configurar MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(SmartAlarm.Application.Handlers.Auth.LoginHandler).Assembly));

// Configurar FIDO2
builder.Services.AddFido2Services(builder.Configuration);

var app = builder.Build();

// Configure security headers middleware
app.Use(async (context, next) =>
{
    // Security headers for OWASP compliance - Set before processing
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    
    await next();
});

// Remove server headers middleware (must be after controllers)
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");
        return Task.CompletedTask;
    });
    
    await next();
});

// Observabilidade: logging estruturado e tracing
app.UseSerilogRequestLogging();
app.UseObservability();

// Rate limiting before authentication (disabled in testing environments)
if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseRateLimiter();
}

// Segurança: KeyVault, tratamento global de erros, autenticação e RBAC
app.UseKeyVault();
app.UseGlobalExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Swagger/OpenAPI
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartAlarm API v1");
    options.RoutePrefix = string.Empty;
});

// Endpoint Prometheus /metrics
app.MapPrometheusScrapingEndpoint("/metrics");

// Controllers
app.MapControllers();

app.Run();

public partial class Program { } // For testing purposes
