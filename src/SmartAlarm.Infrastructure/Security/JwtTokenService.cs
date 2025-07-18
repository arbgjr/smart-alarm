using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SmartAlarm.Domain.Abstractions;
using SmartAlarm.Domain.Entities;
using SmartAlarm.KeyVault.Abstractions;

namespace SmartAlarm.Infrastructure.Security;

/// <summary>
/// Implementação do serviço de tokens JWT
/// Seguindo princípios SOLID e Clean Architecture
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly IKeyVaultService _keyVault;
    private readonly ILogger<JwtTokenService> _logger;
    private readonly ITokenStorage _tokenStorage;

    public JwtTokenService(
        IKeyVaultService keyVault, 
        ILogger<JwtTokenService> logger,
        ITokenStorage tokenStorage)
    {
        _keyVault = keyVault ?? throw new ArgumentNullException(nameof(keyVault));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tokenStorage = tokenStorage ?? throw new ArgumentNullException(nameof(tokenStorage));
    }

    public async Task<string> GenerateTokenAsync(User user, IEnumerable<string> roles)
    {
        try
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            var secret = await _keyVault.GetSecretAsync("jwt-secret");
            var issuer = await _keyVault.GetSecretAsync("jwt-issuer");
            var audience = await _keyVault.GetSecretAsync("jwt-audience");

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
                new("jti", Guid.NewGuid().ToString()) // JWT ID para controle de revogação
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
            return tokenString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating JWT token for user {UserId}", user?.Id);
            throw;
        }
    }

    public async Task<string> GenerateRefreshTokenAsync(User user)
    {
        await Task.CompletedTask;
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
            return refreshToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating refresh token for user {UserId}", user?.Id);
            throw;
        }
    }

    public async Task<IEnumerable<Claim>?> ValidateTokenAndGetClaimsAsync(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            // Extrair JTI (Token ID) do token para verificar revogação
            var jwtHandler = new JwtSecurityTokenHandler();
            var jwt = jwtHandler.ReadJwtToken(token);
            var jti = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

            // Verificar se token foi revogado
            if (!string.IsNullOrEmpty(jti) && await _tokenStorage.IsTokenRevokedAsync(jti))
            {
                _logger.LogWarning("Attempt to use revoked token with JTI: {Jti}", jti);
                return null;
            }

            var secret = await _keyVault.GetSecretAsync("jwt-secret");
            var issuer = await _keyVault.GetSecretAsync("jwt-issuer");
            var audience = await _keyVault.GetSecretAsync("jwt-audience");

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

    public async Task<Guid?> GetUserIdFromTokenAsync(string token)
    {
        try
        {
            var claims = await ValidateTokenAndGetClaimsAsync(token);
            if (claims == null)
                return null;

            var userIdClaim = claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting user ID from token");
            return null;
        }
    }

    public async Task<bool> ValidateRefreshTokenAsync(string refreshToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return false;

            // Usar token storage para validação real
            return await _tokenStorage.ValidateRefreshTokenAsync(refreshToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating refresh token");
            return false;
        }
    }

    public async Task<bool> RevokeTokenAsync(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            // Extrair JTI do token para revogação
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwt = tokenHandler.ReadJwtToken(token);
            var jti = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
            
            if (string.IsNullOrEmpty(jti))
            {
                _logger.LogWarning("Token does not have JTI claim for revocation");
                return false;
            }

            // Calcular tempo até expiração do token para storage
            var expiration = jwt.ValidTo - DateTime.UtcNow;
            if (expiration.TotalMinutes < 1)
                expiration = TimeSpan.FromMinutes(1); // Mínimo de 1 minuto

            await _tokenStorage.RevokeTokenAsync(jti, expiration);
            
            _logger.LogInformation("Token with JTI {Jti} revoked successfully", jti);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token");
            return false;
        }
    }

    public async Task<string> GenerateAccessTokenAsync(User user)
    {
        // Para esta implementação simples, vamos usar uma lista de roles vazia
        // Em uma implementação completa, buscaríamos as roles do usuário
        return await GenerateTokenAsync(user, Enumerable.Empty<string>());
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        var claims = await ValidateTokenAndGetClaimsAsync(token);
        return claims != null;
    }

    public async Task<string?> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return null;

            // Validar refresh token (implementação simplificada)
            if (!await ValidateRefreshTokenAsync(refreshToken))
                return null;

            // Por simplicidade, retornamos o refresh token. 
            // Em uma implementação real, buscaríamos o usuário associado e geraríamos um novo token
            _logger.LogInformation("Refresh token processed");
            return refreshToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refresh token");
            return null;
        }
    }
}
