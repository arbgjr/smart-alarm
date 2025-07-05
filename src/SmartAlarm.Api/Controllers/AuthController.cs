using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartAlarm.Api.Models;
using SmartAlarm.Infrastructure.Configuration;
using SmartAlarm.Api.Services;
using SmartAlarm.Application.DTOs.User;
using System.Threading.Tasks;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace SmartAlarm.Api.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {

        private readonly ILogger<AuthController> _logger;
        private readonly IConfigurationResolver _configResolver;
        private readonly ICurrentUserService _currentUserService;

        public AuthController(ILogger<AuthController> logger, IConfigurationResolver configResolver, ICurrentUserService currentUserService)
        {
            _logger = logger;
            _configResolver = configResolver;
            _currentUserService = currentUserService;
        }

        [HttpPost("login")]

        public async Task<IActionResult> Login([FromBody] LoginRequestDto loginDto)
        {
            // Simulação de autenticação (mock)
            if (loginDto.Username == "admin" && loginDto.Password == "admin")
            {
                var token = await GenerateJwtTokenAsync(loginDto.Username);
                return Ok(new LoginResponseDto { Token = token });
            }
            _logger.LogWarning("Tentativa de login inválida para o usuário {Username}", loginDto.Username);
            return Unauthorized(new ErrorResponse { StatusCode = 401, Title = "Credenciais inválidas", Detail = "Usuário ou senha incorretos." });
        }

        private async Task<string> GenerateJwtTokenAsync(string username)
        {
            var secret = await _configResolver.GetConfigAsync("Jwt:Secret");
            var issuer = await _configResolver.GetConfigAsync("Jwt:Issuer");
            var audience = await _configResolver.GetConfigAsync("Jwt:Audience");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, username)
            };
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
