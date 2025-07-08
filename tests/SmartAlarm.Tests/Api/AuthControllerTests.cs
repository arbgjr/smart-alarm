using Xunit;
using SmartAlarm.Api.Controllers;
using SmartAlarm.Application.DTOs.Auth;
using Microsoft.Extensions.Logging;
using Moq;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using System.Threading;

namespace SmartAlarm.Tests.Api
{
    public class AuthControllerTests
    {
        private readonly AuthController _controller;
        private readonly Mock<ILogger<AuthController>> _loggerMock = new();
        private readonly Mock<IMediator> _mediatorMock = new();

        public AuthControllerTests()
        {
            _controller = new AuthController(_mediatorMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Login_ShouldReturnToken_WhenCredentialsAreValid()
        {
            // Arrange
            var loginDto = new LoginRequestDto { Email = "admin@test.com", Password = "admin123" };
            var expectedResponse = new AuthResponseDto 
            { 
                Success = true, 
                AccessToken = "test-token",
                User = new UserDto 
                { 
                    Id = Guid.NewGuid(), 
                    Name = "Admin", 
                    Email = "admin@test.com" 
                }
            };

            _mediatorMock.Setup(x => x.Send(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<AuthResponseDto>(okResult.Value);
            Assert.True(response.Success);
            Assert.False(string.IsNullOrEmpty(response.AccessToken));
        }

        [Fact]
        public async Task Login_ShouldReturnUnauthorized_WhenCredentialsAreInvalid()
        {
            // Arrange
            var loginDto = new LoginRequestDto { Email = "user@test.com", Password = "wrong" };
            var expectedResponse = new AuthResponseDto { Success = false, Message = "Credenciais inválidas" };

            _mediatorMock.Setup(x => x.Send(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            var response = Assert.IsType<AuthResponseDto>(unauthorizedResult.Value);
            Assert.False(response.Success);
        }
    }
}
