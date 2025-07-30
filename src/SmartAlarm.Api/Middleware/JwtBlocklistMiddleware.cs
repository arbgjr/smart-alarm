using Microsoft.AspNetCore.Http;
using SmartAlarm.Infrastructure.Security;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SmartAlarm.Api.Middleware
{
    /// <summary>
    /// Middleware para verificar tokens JWT na blocklist.
    /// Intercepta requisições autenticadas e verifica se o token está bloqueado.
    /// </summary>
    public class JwtBlocklistMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JwtBlocklistMiddleware> _logger;

        public JwtBlocklistMiddleware(
            RequestDelegate next,
            ILogger<JwtBlocklistMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Verificar se a requisição tem um token de autorização
            if (context.Request.Headers.ContainsKey("Authorization"))
            {
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                {
                    var token = authHeader.Substring("Bearer ".Length).Trim();

                    // Resolver o IJwtBlocklistService do contexto de requisição
                    var blocklistService = context.RequestServices.GetService<IJwtBlocklistService>();

                    if (blocklistService != null)
                    {
                        try
                        {
                            // Extrair o JTI (token ID) do token JWT
                            var tokenHandler = new JwtSecurityTokenHandler();
                            var jsonToken = tokenHandler.ReadJwtToken(token);
                            var jti = jsonToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value;

                            if (!string.IsNullOrEmpty(jti))
                            {
                                // Verificar se o token está na blocklist
                                var isBlocked = await blocklistService.IsTokenBlockedAsync(jti);

                                if (isBlocked)
                                {
                                    _logger.LogWarning("Blocked token attempt: {TokenId} from {RemoteIpAddress}",
                                        jti, context.Connection.RemoteIpAddress);

                                    context.Response.StatusCode = 401;
                                    context.Response.ContentType = "application/json";

                                    await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
                                    {
                                        StatusCode = 401,
                                        Title = "Token revogado",
                                        Detail = "O token de acesso foi revogado",
                                        Type = "TokenBlocked",
                                        TraceId = context.TraceIdentifier
                                    }));

                                    return; // Não continuar o pipeline
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error checking token blocklist for request to {Path}",
                                context.Request.Path);

                            // Em caso de erro, continuar o pipeline (fail-open)
                            // Isso evita que problemas na blocklist quebrem toda a aplicação
                        }
                    }
                }
            }

            // Continuar para o próximo middleware
            await _next(context);
        }
    }

    /// <summary>
    /// Extensão para facilitar o registro do middleware
    /// </summary>
    public static class JwtBlocklistMiddlewareExtensions
    {
        public static IApplicationBuilder UseJwtBlocklist(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<JwtBlocklistMiddleware>();
        }
    }
}
