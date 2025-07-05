using Serilog;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using SmartAlarm.Api.Middleware;
using SmartAlarm.Api.Configuration;
using SmartAlarm.KeyVault.Extensions;
using SmartAlarm.Infrastructure;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

// Configuração do Serilog
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
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
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSettings["Secret"])),
        ClockSkew = TimeSpan.FromMinutes(2)
    };
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<SmartAlarm.Api.Services.ICurrentUserService, SmartAlarm.Api.Services.CurrentUserService>();

// Configure KeyVault services
builder.Services.AddKeyVault(builder.Configuration);

var app = builder.Build();

// Observabilidade padrão
app.UseSerilogRequestLogging();

// KeyVault middleware
app.UseKeyVault();

// Tratamento global de erros
app.UseGlobalExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartAlarm API v1");
    options.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

public partial class Program { } // For testing purposes

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
