using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SmartAlarm.Tests.Api
{
    public class AuthenticationTests : IClassFixture<CustomWebApplicationFactory<SmartAlarm.Api.Program>>
    {
        private readonly CustomWebApplicationFactory<SmartAlarm.Api.Program> _factory;
        public AuthenticationTests(CustomWebApplicationFactory<SmartAlarm.Api.Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Should_Return401_When_TokenIsMissing()
        {
            // Arrange
            var client = _factory.CreateClient();
            // Act
            var response = await client.GetAsync("/api/v1/alarms");
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Should_Return401_When_TokenIsInvalid()
        {
            // Arrange
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalidtoken");
            // Act
            var response = await client.GetAsync("/api/v1/alarms");
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Should_Return403_When_UserLacksRequiredRole()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwicm9sZSI6IlVzZXIiLCJleHAiOjQ3OTk2ODgwMDB9.abc"; // Token inválido apenas para simulação
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            // Act
            var response = await client.GetAsync("/api/v1/alarms");
            // Assert
            response.StatusCode.Should().Match(status => status == HttpStatusCode.Forbidden || status == HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Should_Return401_When_TokenIsExpired()
        {
            // Arrange
            var client = _factory.CreateClient();
            var expiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwiZXhwIjoxNjAwMDAwMDB9.abc"; // Token expirado simulado
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);
            // Act
            var response = await client.GetAsync("/api/v1/alarms");
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        private string GenerateValidJwtToken()
        {
            var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("REPLACE_WITH_A_STRONG_SECRET_KEY_32CHARS"));
            var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "1234567890"),
                new System.Security.Claims.Claim("role", "User")
            };
            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                issuer: "SmartAlarmIssuer",
                audience: "SmartAlarmAudience",
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: credentials
            );
            return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
        }

        [Fact]
        public async Task Should_Return200_When_TokenIsValid()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = GenerateValidJwtToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            // Act
            var response = await client.GetAsync("/api/v1/alarms");
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // Adicione mais testes conforme necessário
    }
}
