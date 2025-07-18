using System;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartAlarm.Infrastructure.Security;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;

namespace SmartAlarm.Infrastructure.Middleware
{
    /// <summary>
    /// Middleware para verificar se JWT tokens estão na blocklist
    /// </summary>
    public class JwtBlocklistMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JwtBlocklistMiddleware> _logger;

        public JwtBlocklistMiddleware(RequestDelegate next, ILogger<JwtBlocklistMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Verificar se há token JWT no request
            var token = ExtractTokenFromRequest(context);
            
            if (!string.IsNullOrEmpty(token))
            {
                var blocklistService = context.RequestServices.GetService<IJwtBlocklistService>();
                var meter = context.RequestServices.GetService<SmartAlarmMeter>();
                var activitySource = context.RequestServices.GetService<SmartAlarmActivitySource>();

                if (blocklistService != null)
                {
                    using var activity = activitySource?.StartActivity("JwtBlocklist.CheckMiddleware");
                    activity?.SetTag("jwt.middleware", "blocklist_check");

                    var stopwatch = Stopwatch.StartNew();

                    try
                    {
                        var tokenId = ExtractTokenId(token);
                        
                        if (!string.IsNullOrEmpty(tokenId))
                        {
                            activity?.SetTag("jwt.token_id", tokenId);
                            
                            var isBlocked = await blocklistService.IsTokenBlockedAsync(tokenId);
                            
                            stopwatch.Stop();
                            meter?.RecordExternalServiceCallDuration(stopwatch.ElapsedMilliseconds, "JwtBlocklist", "CheckMiddleware", !isBlocked);

                            if (isBlocked)
                            {
                                _logger.LogWarning("Blocked JWT token detected in request. TokenId: {TokenId}, Path: {Path}", 
                                    tokenId, context.Request.Path);
                                
                                activity?.SetStatus(ActivityStatusCode.Error, "Token is blocked");
                                
                                context.Response.StatusCode = 401;
                                context.Response.ContentType = "application/json";
                                
                                var errorResponse = new
                                {
                                    error = "unauthorized",
                                    message = "Token has been revoked",
                                    timestamp = DateTimeOffset.UtcNow
                                };

                                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(errorResponse));
                                return;
                            }

                            activity?.SetStatus(ActivityStatusCode.Ok, "Token is valid");
                        }
                        else
                        {
                            _logger.LogDebug("No token ID found in JWT, skipping blocklist check");
                        }
                    }
                    catch (Exception ex)
                    {
                        stopwatch.Stop();
                        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                        meter?.IncrementErrorCount("JWT_BLOCKLIST", "Middleware", "CheckError");

                        _logger.LogError(ex, "Error checking JWT blocklist in middleware");
                        
                        // Em caso de erro na verificação, permitir que o request continue
                        // para não quebrar a aplicação por problemas de infraestrutura
                    }
                }
            }

            await _next(context);
        }

        /// <summary>
        /// Extrai o token JWT do header Authorization
        /// </summary>
        private string? ExtractTokenFromRequest(HttpContext context)
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            
            if (authHeader != null && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return authHeader.Substring("Bearer ".Length).Trim();
            }

            return null;
        }

        /// <summary>
        /// Extrai o ID do token (claim 'jti') do JWT
        /// </summary>
        private string? ExtractTokenId(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                
                if (tokenHandler.CanReadToken(token))
                {
                    var jwtToken = tokenHandler.ReadJwtToken(token);
                    return jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract token ID from JWT");
            }

            return null;
        }
    }
}
