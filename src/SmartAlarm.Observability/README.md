# SmartAlarm.Observability

Observability middleware for structured logging (Serilog), tracing (OpenTelemetry), and metrics (Prometheus/OTel).

## Purpose

Provide reusable components for observability in all Smart Alarm services, following Clean Architecture and SOLID principles.

## Technologies

- Serilog
- OpenTelemetry
- Prometheus (via OTel)

## How to use in other modules

1. **Add a reference to the Observability project:**

   In your module's `.csproj` file (e.g., Api, IntegrationService), include:

   ```xml
   <ProjectReference Include="..\SmartAlarm.Observability\SmartAlarm.Observability.csproj" />
   ```

2. **Add the required NuGet packages:**

   - `Serilog.AspNetCore`
   - `OpenTelemetry.Extensions.Hosting`
   - `OpenTelemetry.Instrumentation.AspNetCore`
   - `OpenTelemetry.Exporter.Prometheus`

3. **Configure Serilog and OpenTelemetry in `Program.cs`:**

   ```csharp
   using Serilog;
   using OpenTelemetry.Resources;
   using OpenTelemetry.Trace;
   using OpenTelemetry.Metrics;

   var builder = WebApplication.CreateBuilder(args);

   builder.Host.UseSerilog((context, services, configuration) =>
   {
       configuration
           .ReadFrom.Configuration(context.Configuration)
           .ReadFrom.Services(services)
           .Enrich.FromLogContext();
   });

   builder.Services.AddOpenTelemetry()
       .WithTracing(tracing => tracing
           .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MODULE_NAME"))
           .AddAspNetCoreInstrumentation()
           .AddHttpClientInstrumentation()
           .AddOtlpExporter())
       .WithMetrics(metrics => metrics
           .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MODULE_NAME"))
           .AddAspNetCoreInstrumentation()
           .AddPrometheusExporter());
   ```

4. **Add the middleware to the pipeline:**

   ```csharp
   app.UseObservability();
   app.UseOpenTelemetryPrometheusScrapingEndpoint();
   ```

5. **Configure `appsettings.json` for Serilog:**

   ```json
   {
     "Serilog": {
       "MinimumLevel": {
         "Default": "Information",
         "Override": {
           "Microsoft": "Warning",
           "System": "Warning"
         }
       },
       "WriteTo": [
         { "Name": "Console" }
       ],
       "Enrich": [ "FromLogContext" ]
     }
   }
   ```

## Notes

- Always follow Clean Architecture and SOLID standards.
- The middleware is extensible for custom logs, distributed tracing, and custom metrics.
- See `systemPatterns.md` to ensure architectural compliance.
- **📚 Complete documentation**: See `docs/architecture/observability-patterns.md` for detailed implementation patterns and examples.

## Implementation Status

**Etapa 4 concluída:**

- Observabilidade implementada e validada: tracing distribuído (OpenTelemetry), métricas customizadas (Prometheus), logs estruturados (Serilog), health endpoints e dashboards.
- Testes reais de integração para tracing e métricas presentes e passando.
- Todos os handlers críticos da Application Layer instrumentados conforme padrões (systemPatterns.md, observability-patterns.md).

Consulte `docs/architecture/observability-patterns.md` para exemplos e padrões detalhados.
