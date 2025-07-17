using Serilog;
using SmartAlarm.Observability.Extensions;
using SmartAlarm.Infrastructure;
using SmartAlarm.Application;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/smartalarm-ai-service.log", 
            rollingInterval: RollingInterval.Day, 
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}");
});

// Adicionar observabilidade completa
builder.Services.AddObservability(builder.Configuration, "SmartAlarm.AiService", "1.0.0");

// Registrar MediatR apontando para os handlers do AI Service e Application Layer
builder.Services.AddMediatR(cfg => 
{
    cfg.RegisterServicesFromAssembly(typeof(SmartAlarm.Application.Commands.CreateAlarmCommand).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(SmartAlarm.AiService.Application.Commands.AnalyzeAlarmPatternsCommand).Assembly);
});

// Registrar infraestrutura
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddSmartAlarmInfrastructure(builder.Configuration);
}

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "SmartAlarm AI Service API", 
        Version = "v1",
        Description = "Serviço de IA para análise comportamental e recomendações do Smart Alarm"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartAlarm AI Service v1"));
}

// Configurar observabilidade
app.UseObservability();

app.UseHttpsRedirection();
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
