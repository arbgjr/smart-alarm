using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartAlarm.Observability.Configuration;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Extensions.Logging;
using SmartAlarm.Observability.Logging;
using SmartAlarm.Observability.Logging.Formatters;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Middleware;
using SmartAlarm.Observability.Tracing;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Exporter;
using Serilog;
using Serilog.Events;
using Serilog.Core;
using Serilog.Sinks.File;
using Serilog.Formatting.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace SmartAlarm.Observability.Extensions
{
    /// <summary>
    /// Extensões para configurar observabilidade na aplicação
    /// </summary>
    public static class ObservabilityExtensions
    {
        /// <summary>
        /// Adiciona serviços de observabilidade à aplicação
        /// </summary>
        /// <param name="services">Coleção de serviços</param>
        /// <param name="configuration">Configuração da aplicação</param>
        /// <param name="serviceName">Nome do serviço para instrumentação</param>
        /// <param name="serviceVersion">Versão do serviço</param>
        /// <returns>Coleção de serviços configurada</returns>
        public static IServiceCollection AddObservability(
            this IServiceCollection services,
            IConfiguration configuration,
            string serviceName = "SmartAlarm",
            string serviceVersion = "1.0.0")
        {
            // Configuração da observabilidade
            var observabilityConfig = new ObservabilityConfiguration();
            configuration.GetSection("Observability").Bind(observabilityConfig);
            services.AddSingleton(observabilityConfig);

            // Contexto de correlação
            services.AddScoped<ICorrelationContext, CorrelationContext>();

            // Logging
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog();
            });

            // Configurar Serilog
            ConfigureSerilog(configuration, serviceName);

            // OpenTelemetry
            services.AddOpenTelemetry()
                .WithTracing(tracerProvider =>
                {
                    tracerProvider
                        .SetResourceBuilder(ResourceBuilder.CreateDefault()
                            .AddService(serviceName, serviceVersion)
                            .AddAttributes(new Dictionary<string, object>
                            {
                                ["service.environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
                                ["service.instance.id"] = Environment.MachineName,
                                ["deployment.environment"] = observabilityConfig.Environment ?? "unknown"
                            }))
                        .AddAspNetCoreInstrumentation(options =>
                        {
                            options.RecordException = true;
                            options.EnrichWithHttpRequest = (activity, request) =>
                            {
                                activity.SetTag("http.request.size", request.ContentLength);
                                activity.SetTag("http.request.host", request.Host.Value);
                            };
                            options.EnrichWithHttpResponse = (activity, response) =>
                            {
                                activity.SetTag("http.response.size", response.ContentLength);
                            };
                        })
                        .AddHttpClientInstrumentation(options =>
                        {
                            options.RecordException = true;
                            options.EnrichWithHttpRequestMessage = (activity, request) =>
                            {
                                activity.SetTag("http.client.request.size", request.Content?.Headers?.ContentLength);
                            };
                            options.EnrichWithHttpResponseMessage = (activity, response) =>
                            {
                                activity.SetTag("http.client.response.size", response.Content?.Headers?.ContentLength);
                            };
                        })
                        .AddSource(SmartAlarmActivitySource.Name);

                    // Configurar exportadores
                    if (observabilityConfig.Tracing?.Console?.Enabled == true)
                    {
                        tracerProvider.AddConsoleExporter();
                    }

                    if (observabilityConfig.Tracing?.Otlp?.Enabled == true)
                    {
                        tracerProvider.AddOtlpExporter(options =>
                        {
                            options.Endpoint = new Uri(observabilityConfig.Tracing.Otlp.Endpoint);
                        });
                    }

                    // Configurar OTLP exporter (Jaeger via OTLP)
                    if (observabilityConfig.Tracing?.Jaeger?.Enabled == true)
                    {
                        tracerProvider.AddOtlpExporter(options =>
                        {
                            options.Endpoint = new Uri($"http://{observabilityConfig.Tracing.Jaeger.AgentHost}:4317");
                        });
                    }
                })
                .WithMetrics(meterProvider =>
                {
                    meterProvider
                        .SetResourceBuilder(ResourceBuilder.CreateDefault()
                            .AddService(serviceName, serviceVersion))
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddMeter(SmartAlarmMeter.Name);

                    // Configurar exportadores
                    if (observabilityConfig.Metrics?.Console?.Enabled == true)
                    {
                        meterProvider.AddConsoleExporter();
                    }

                    if (observabilityConfig.Metrics?.Prometheus?.Enabled == true)
                    {
                        meterProvider.AddPrometheusExporter();
                    }
                });

            // Métricas customizadas
            services.AddSingleton<SmartAlarmMeter>();
            services.AddSingleton<BusinessMetrics>();

            // Tracing customizado
            services.AddSingleton<SmartAlarmActivitySource>();
            services.AddScoped<IDistributedTracingService, DistributedTracingService>();

            // Middleware de observabilidade não deve ser registrado como serviço
            // Ele será criado diretamente pelo UseMiddleware<>()

            // Informações de versão
            services.AddSingleton<IVersionInfo, VersionInfo>();

            // Health Checks
            services.AddSmartAlarmHealthChecks(configuration);

            return services;
        }

        /// <summary>
        /// Configura o middleware de observabilidade na aplicação
        /// </summary>
        /// <param name="app">Application builder</param>
        /// <returns>Application builder configurado</returns>
        public static IApplicationBuilder UseObservability(this IApplicationBuilder app)
        {
            // Middleware de correlação deve vir antes de outros middlewares
            app.UseMiddleware<ObservabilityMiddleware>();

            // Health checks endpoints
            app.UseHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = _ => true,
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";
                    var response = new
                    {
                        status = report.Status.ToString(),
                        totalDuration = report.TotalDuration.TotalMilliseconds,
                        entries = report.Entries.Select(entry => new
                        {
                            name = entry.Key,
                            status = entry.Value.Status.ToString(),
                            duration = entry.Value.Duration.TotalMilliseconds,
                            description = entry.Value.Description
                        })
                    };
                    await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                }
            });

            // Liveness probe (health check básico)
            app.UseHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("basic") || check.Tags.Contains("ready")
            });

            // Readiness probe (dependências)
            app.UseHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("readiness") || check.Tags.Contains("database")
            });

            // Middleware de métricas do Prometheus (se habilitado)
            var observabilityConfig = app.ApplicationServices.GetService<ObservabilityConfiguration>();
            if (observabilityConfig?.Metrics?.Prometheus?.Enabled == true)
            {
                app.UseOpenTelemetryPrometheusScrapingEndpoint();
            }

            return app;
        }

        /// <summary>
        /// Configura o Serilog para a aplicação
        /// </summary>
        /// <param name="configuration">Configuração da aplicação</param>
        /// <param name="serviceName">Nome do serviço</param>
        private static void ConfigureSerilog(IConfiguration configuration, string serviceName)
        {
            var loggerConfig = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.WithThreadId()
                .Enrich.WithEnvironmentName()
                .Enrich.WithProcessId()
                .Enrich.WithProperty("ServiceName", serviceName)
                .Enrich.WithProperty("ServiceVersion", "1.0.0");

            // Console sink com formatação estruturada
            loggerConfig.WriteTo.Console(new JsonFormatter());

            // File sink com rotação
            var logPath = configuration.GetValue<string>("Observability:Logging:FilePath") ?? "logs/app-.log";
            loggerConfig.WriteTo.File(
                new JsonFormatter(),
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                shared: true);

            // Configurar nível de log
            var logLevel = configuration.GetValue<string>("Observability:Logging:Level");
            if (Enum.TryParse<LogEventLevel>(logLevel, out var level))
            {
                loggerConfig.MinimumLevel.Is(level);
            }
            else
            {
                loggerConfig.MinimumLevel.Information();
            }

            // Override para bibliotecas específicas
            loggerConfig
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .MinimumLevel.Override("System", LogEventLevel.Warning);

            Log.Logger = loggerConfig.CreateLogger();
        }
    }
}
