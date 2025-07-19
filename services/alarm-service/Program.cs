using Serilog;
using SmartAlarm.Observability.Extensions;
using SmartAlarm.Infrastructure;
using SmartAlarm.Application;
using Hangfire;
using Hangfire.PostgreSql;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/smartalarm-alarm-service.log", 
            rollingInterval: RollingInterval.Day, 
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}");
});

// Adicionar observabilidade completa
builder.Services.AddObservability(builder.Configuration, "SmartAlarm.AlarmService", "1.0.0");

// Registrar MediatR para handlers do próprio serviço
builder.Services.AddMediatR(cfg => 
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(SmartAlarm.Application.Commands.CreateAlarmCommand).Assembly);
});

// Registrar infraestrutura
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddSmartAlarmInfrastructure(builder.Configuration);
}

// Configurar Hangfire para background jobs
var connectionString = builder.Configuration.GetConnectionString("PostgresDb");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddHangfire(configuration => configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(connectionString));

    builder.Services.AddHangfireServer();
}

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "SmartAlarm Alarm Service API", 
        Version = "v1",
        Description = "Serviço principal de gerenciamento de alarmes do Smart Alarm"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartAlarm Alarm Service v1"));
}

// Configurar observabilidade
app.UseObservability();

// Configurar Hangfire Dashboard
if (!string.IsNullOrEmpty(connectionString))
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new Hangfire.Dashboard.LocalRequestsOnlyAuthorizationFilter() }
    });
}

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
