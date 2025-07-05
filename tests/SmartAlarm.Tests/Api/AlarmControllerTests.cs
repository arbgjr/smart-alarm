using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SmartAlarm.Tests.Api
{
    public class AlarmControllerTests : IClassFixture<CustomWebApplicationFactory<SmartAlarm.Api.Program>>
    {
        private readonly CustomWebApplicationFactory<SmartAlarm.Api.Program> _factory;

        public AlarmControllerTests(CustomWebApplicationFactory<SmartAlarm.Api.Program> factory)
        {
            _factory = factory;
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
        public async Task CreateAlarm_ShouldReturn201()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = GenerateValidJwtToken();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var alarm = new { name = "Test Alarm", time = DateTime.UtcNow, enabled = true };

            // Act
            var response = await client.PostAsJsonAsync("/api/v1/alarms", alarm);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task ListAlarms_ShouldReturn200()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = GenerateValidJwtToken();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await client.GetAsync("/api/v1/alarms");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
