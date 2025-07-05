using Xunit;
using SmartAlarm.Api.Controllers;
using SmartAlarm.Application.DTOs.User;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using SmartAlarm.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace SmartAlarm.Tests.Api
{
    public class AuthControllerTests
    {
        private readonly AuthController _controller;
        private readonly Mock<ILogger<AuthController>> _loggerMock = new();
        private readonly Mock<SmartAlarm.Infrastructure.Configuration.IConfigurationResolver> _configResolverMock = new();
        private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();

        public AuthControllerTests()
        {
            _configResolverMock.Setup(x => x.GetConfigAsync("Jwt:Secret", It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync("TEST_SECRET_KEY_12345678901234567890123456789012");
            _configResolverMock.Setup(x => x.GetConfigAsync("Jwt:Issuer", It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync("TestIssuer");
            _configResolverMock.Setup(x => x.GetConfigAsync("Jwt:Audience", It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync("TestAudience");
            _controller = new AuthController(_loggerMock.Object, _configResolverMock.Object, _currentUserServiceMock.Object);
        }

        [Fact]
        public async Task Login_ShouldReturnToken_WhenCredentialsAreValid()
        {
            var loginDto = new LoginRequestDto { Username = "admin", Password = "admin" };
            var result = await _controller.Login(loginDto);
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<LoginResponseDto>(okResult.Value);
            Assert.False(string.IsNullOrEmpty(response.Token));
        }

        [Fact]
        public async Task Login_ShouldReturnUnauthorized_WhenCredentialsAreInvalid()
        {
            var loginDto = new LoginRequestDto { Username = "user", Password = "wrong" };
            var result = await _controller.Login(loginDto);
            Assert.IsType<UnauthorizedObjectResult>(result);
        }
    }
}
