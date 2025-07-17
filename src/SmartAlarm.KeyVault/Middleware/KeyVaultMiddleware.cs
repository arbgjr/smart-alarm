using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SmartAlarm.KeyVault.Abstractions;

namespace SmartAlarm.KeyVault.Middleware
{
    /// <summary>
    /// Middleware to inject KeyVault service into HTTP context for easy access to secrets.
    /// </summary>
    public class KeyVaultMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<KeyVaultMiddleware> _logger;

        public KeyVaultMiddleware(RequestDelegate next, ILogger<KeyVaultMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IKeyVaultService keyVaultService)
        {
            try
            {
                // Add KeyVault service to HttpContext for easy access in controllers and services
                context.Items["KeyVaultService"] = keyVaultService;

                // Log de depuração para rastreamento de fluxo interno.
                // Não indica pendência ou débito técnico.
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    var availableProviders = await keyVaultService.GetAvailableProvidersAsync(context.RequestAborted);
                    _logger.LogDebug("Available KeyVault providers: {Providers}", string.Join(", ", availableProviders));
                }

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in KeyVault middleware");
                // Don't let KeyVault errors break the application flow
                await _next(context);
            }
        }
    }
}