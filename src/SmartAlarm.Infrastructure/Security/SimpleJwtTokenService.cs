using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SmartAlarm.Domain.Abstractions;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Infrastructure.Security;

/// <summary>
/// ImplementaÃ§Ã£o simplificada do serviÃ§o de tokens JWT que usa configuraÃ§Ãµes da aplicaÃ§Ã£o
/// Usado principalmente para testes e ambientes de desenvolvimento
/// </summary>
public class SimpleJwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SimpleJwtTokenService> _logger;
    private readonly HashSet<string> _revokedTokens = new();

    public SimpleJwtTokenService(IConfiguration configuration, ILogger<SimpleJwtTokenService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<string> GenerateTokenAsync(User user, IEnumerable<string> roles)
    {
        try
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            var secret = _configuration["Jwt:Secret"];
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];

            if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
            {
                _logger.LogError("JWT configuration is incomplete");
                throw new InvalidOperationException("JWT configuration is incomplete");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Name.ToString()),
                new(ClaimTypes.Email, user.Email.ToString()),
                new("email_verified", user.EmailVerified.ToString()),
                new("is_active", user.IsActive.ToString()),
                new("jti", Guid.NewGuid().ToString()) // JWT ID para controle de revogaÃ§Ã£o
            };

            // Adicionar roles como claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30), // 30 minutos
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            _logger.LogInformation("JWT token generated for user {UserId}", user.Id);
            return Task.FromResult(tokenString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating JWT token for user {UserId}", user?.Id);
            throw;
        }
    }

    public Task<string> GenerateRefreshTokenAsync(User user)
    {
        try
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            // Gerar refresh token seguro
            using var rng = RandomNumberGenerator.Create();
            var tokenBytes = new byte[32];
            rng.GetBytes(tokenBytes);
            var refreshToken = Convert.ToBase64String(tokenBytes);

            _logger.LogInformation("Refresh token generated for user {UserId}", user.Id);
            return Task.FromResult(refreshToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating refresh token for user {UserId}", user?.Id);
            throw;
        }
    }

    public async Task<IEnumerable<Claim>?> ValidateTokenAndGetClaimsAsync(string token)
    {
        await Task.CompletedTask;
        try
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            // Verificar se token foi revogado
            if (_revokedTokens.Contains(token))
            {
                _logger.LogWarning("Attempt to use revoked token");
                return null;
            }

            var secret = _configuration["Jwt:Secret"];
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];

            if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
            {
                _logger.LogError("JWT configuration is incomplete for validation");
                return null;
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var tokenHandler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.FromMinutes(2)
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            
            _logger.LogDebug("Token validated successfully");
            return principal.Claims;
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning("Token expired");
            return null;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Invalid token");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return null;
        }
    }

    public Task<Guid?> GetUserIdFromTokenAsync(string token)
    {
        try
        {
            var claims = ValidateTokenAndGetClaimsAsync(token).Result;
            if (claims == null)
                return Task.FromResult<Guid?>(null);

            var userIdClaim = claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Task.FromResult<Guid?>(userId);
            }

            return Task.FromResult<Guid?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting user ID from token");
            return Task.FromResult<Guid?>(null);
        }
    }

    public Task<bool> ValidateRefreshTokenAsync(string refreshToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return Task.FromResult(false);

            // Aqui implementarÃ­amos validaÃ§Ã£o com storage (Redis/Database)
            // Por simplicidade, validamos formato bÃ¡sico
            var tokenBytes = Convert.FromBase64String(refreshToken);
            return Task.FromResult(tokenBytes.Length == 32);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating refresh token");
            return Task.FromResult(false);
        }
    }

    public Task<bool> RevokeTokenAsync(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
                return Task.FromResult(false);

            // Adicionar token Ã  lista de revogados
            _revokedTokens.Add(token);
            
            _logger.LogInformation("Token revoked successfully");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token");
            return Task.FromResult(false);
        }
    }

    public Task<string> GenerateAccessTokenAsync(User user)
    {
        // Para esta implementaÃ§Ã£o simples, vamos usar uma lista de roles vazia
        // Em uma implementaÃ§Ã£o completa, buscarÃ­amos as roles do usuÃ¡rio
        return GenerateTokenAsync(user, Enumerable.Empty<string>());
    }

    public Task<bool> ValidateTokenAsync(string token)
    {
        var claims = ValidateTokenAndGetClaimsAsync(token).Result;
        return Task.FromResult(claims != null);
    }

    public Task<string?> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return Task.FromResult<string?>(null);

            // Validar refresh token (implementaÃ§Ã£o simplificada)
            if (!ValidateRefreshTokenAsync(refreshToken).Result)
                return Task.FromResult<string?>(null);

            // Por simplicidade, retornamos o refresh token. 
            // Em uma implementação real, buscaríamos o usuário associado e geraríamos um novo token
            _logger.LogInformation("Refresh token processed");
            return Task.FromResult<string?>(refreshToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refresh token");
            return Task.FromResult<string?>(null);
        }
    }
}


