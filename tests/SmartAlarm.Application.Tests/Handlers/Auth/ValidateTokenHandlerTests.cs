using SmartAlarm.Domain.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MediatR;
using Moq;
using Xunit;
using SmartAlarm.Application.Handlers.Auth;
using SmartAlarm.Application.Queries.Auth;
using SmartAlarm.Application.DTOs.Auth;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.ValueObjects;

namespace SmartAlarm.Application.Tests.Handlers.Auth
{
    public class ValidateTokenHandlerTests
    {
        private readonly Mock<IJwtTokenService> _mockJwtTokenService;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<ILogger<ValidateTokenHandler>> _mockLogger;
        private readonly ValidateTokenHandler _handler;

        public ValidateTokenHandlerTests()
        {
            _mockJwtTokenService = new Mock<IJwtTokenService>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockLogger = new Mock<ILogger<ValidateTokenHandler>>();

            _handler = new ValidateTokenHandler(
                _mockJwtTokenService.Object,
                _mockUserRepository.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_ValidTokenAndActiveUser_ShouldReturnUserDto()
        {
            // Arrange
            var token = "valid-token";
            var userId = Guid.NewGuid();
            var query = new ValidateTokenQuery(token);

            var user = new User(
                userId,
                new Name("Test User"),
                new Email("test@example.com"),
                true);

            _mockJwtTokenService.Setup(x => x.ValidateTokenAsync(token))
                .ReturnsAsync(true);
            _mockJwtTokenService.Setup(x => x.GetUserIdFromTokenAsync(token))
                .ReturnsAsync(userId);
            _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Id, result.Id);
            Assert.Equal(user.Name.Value, result.Name);
            Assert.Equal(user.Email.Address, result.Email);
            Assert.True(result.IsActive);
        }

        [Fact]
        public async Task Handle_InvalidToken_ShouldReturnNull()
        {
            // Arrange
            var token = "invalid-token";
            var query = new ValidateTokenQuery(token);

            _mockJwtTokenService.Setup(x => x.ValidateTokenAsync(token))
                .ReturnsAsync(false);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Handle_ValidTokenButUserNotFound_ShouldReturnNull()
        {
            // Arrange
            var token = "valid-token";
            var userId = Guid.NewGuid();
            var query = new ValidateTokenQuery(token);

            _mockJwtTokenService.Setup(x => x.ValidateTokenAsync(token))
                .ReturnsAsync(true);
            _mockJwtTokenService.Setup(x => x.GetUserIdFromTokenAsync(token))
                .ReturnsAsync(userId);
            _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Handle_ValidTokenButInactiveUser_ShouldReturnNull()
        {
            // Arrange
            var token = "valid-token";
            var userId = Guid.NewGuid();
            var query = new ValidateTokenQuery(token);

            var user = new User(
                userId,
                new Name("Test User"),
                new Email("test@example.com"),
                true);
            
            // Simular usuário inativo
            user.Deactivate();

            _mockJwtTokenService.Setup(x => x.ValidateTokenAsync(token))
                .ReturnsAsync(true);
            _mockJwtTokenService.Setup(x => x.GetUserIdFromTokenAsync(token))
                .ReturnsAsync(userId);
            _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Handle_ExceptionThrown_ShouldReturnNull()
        {
            // Arrange
            var token = "valid-token";
            var query = new ValidateTokenQuery(token);

            _mockJwtTokenService.Setup(x => x.ValidateTokenAsync(token))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }
    }
}
