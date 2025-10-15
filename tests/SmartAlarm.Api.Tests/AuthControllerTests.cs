using SmartAlarm.Domain.Abstractions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SmartAlarm.Application.DTOs.User;
using Xunit;

namespace SmartAlarm.Api.Tests
{
    public class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public AuthControllerTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Should_Return_Token_When_Valid_Login()
        {
            // Arrange
            var loginDto = new LoginRequestDto { Username = "admin", Password = "admin" };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginDto);
            var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Should().NotBeNull();
            result!.Token.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task Should_Return_Unauthorized_When_Invalid_Login()
        {
            // Arrange
            var loginDto = new LoginRequestDto { Username = "user", Password = "wrong" };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
