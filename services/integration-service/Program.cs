using Serilog;
using SmartAlarm.Observability.Extensions;
using SmartAlarm.Infrastructure;
using SmartAlarm.Application;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/smartalarm-integration-service.log", 
            rollingInterval: RollingInterval.Day, 
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}");
});

// Adicionar observabilidade completa
builder.Services.AddObservability(builder.Configuration, "SmartAlarm.IntegrationService", "1.0.0");

// Registrar MediatR apontando para a Application Layer
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(SmartAlarm.Application.Commands.CreateAlarmCommand).Assembly));

// Registrar infraestrutura
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddSmartAlarmInfrastructure(builder.Configuration);
}

// Configurar HttpClient com Polly para resiliência
builder.Services.AddHttpClient("ExternalIntegrations", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());

// Configurar autenticação JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // TODO: Configurar JWT adequadamente
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
    });

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "SmartAlarm Integration Service API", 
        Version = "v1",
        Description = "Serviço de integrações externas do Smart Alarm"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartAlarm Integration Service v1"));
}

// Configurar observabilidade
app.UseObservability();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health checks
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/detail", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(x => new
            {
                name = x.Key,
                status = x.Value.Status.ToString(),
                description = x.Value.Description,
                duration = x.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
});

app.Run();

// Políticas de resiliência
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Console.WriteLine($"Retry {retryCount} after {timespan} seconds");
            });
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 3,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (exception, duration) =>
            {
                Console.WriteLine($"Circuit breaker opened for {duration}");
            },
            onReset: () =>
            {
                Console.WriteLine("Circuit breaker closed");
            });
}
