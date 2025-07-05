using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartAlarm.Api.Models;
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
        private readonly IConfiguration _configuration;
        private readonly ICurrentUserService _currentUserService;

        public AuthController(ILogger<AuthController> logger, IConfiguration configuration, ICurrentUserService currentUserService)
        {
            _logger = logger;
            _configuration = configuration;
            _currentUserService = currentUserService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto loginDto)
        {
            // Simulação de autenticação (mock)
            if (loginDto.Username == "admin" && loginDto.Password == "admin")
            {
                var token = GenerateJwtToken(loginDto.Username);
                return Ok(new LoginResponseDto { Token = token });
            }
            _logger.LogWarning("Tentativa de login inválida para o usuário {Username}", loginDto.Username);
            return Unauthorized(new ErrorResponse { StatusCode = 401, Title = "Credenciais inválidas", Detail = "Usuário ou senha incorretos." });
        }

        private string GenerateJwtToken(string username)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var secret = jwtSettings["Secret"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
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
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
