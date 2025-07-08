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
using MediatR;
using FluentValidation;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddMediatR(typeof(SmartAlarm.Application.Commands.CreateAlarmCommand).Assembly);

// Registrar infraestrutura (repositories e serviços)
builder.Services.AddSmartAlarmInfrastructure(builder.Configuration);

// Configuração do OpenTelemetry (Tracing e Métricas)
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("SmartAlarm.Api"))
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("SmartAlarm.Api"))
        .AddAspNetCoreInstrumentation()
        .AddPrometheusExporter());

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


// LGPD: Serviço de consentimento do usuário
builder.Services.AddSingleton<SmartAlarm.Api.Services.IUserConsentService, SmartAlarm.Api.Services.UserConsentService>();

// Configure KeyVault services

// Registra IConfigurationResolver para injeção de dependência
builder.Services.AddScoped<SmartAlarm.Infrastructure.Configuration.IConfigurationResolver, SmartAlarm.Infrastructure.Configuration.ConfigurationResolver>();
builder.Services.AddKeyVault(builder.Configuration);

// Configurar MediatR
builder.Services.AddMediatR(typeof(SmartAlarm.Application.Handlers.Auth.LoginHandler).Assembly);

// Registrar serviços de autenticação
builder.Services.AddScoped<SmartAlarm.Domain.Abstractions.IJwtTokenService, SmartAlarm.Infrastructure.Security.JwtTokenService>();

// Configurar FIDO2
builder.Services.AddFido2Services(builder.Configuration);

var app = builder.Build();

// Observabilidade: logging estruturado e tracing
app.UseSerilogRequestLogging();
app.UseMiddleware<SmartAlarm.Observability.ObservabilityMiddleware>();

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
