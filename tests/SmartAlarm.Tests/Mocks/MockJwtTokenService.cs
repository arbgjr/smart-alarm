using SmartAlarm.Domain.Abstractions;
using System.Threading.Tasks;
using Moq;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Tests.Mocks
{
    /// <summary>
    /// Implementação mock do serviço JWT para testes
    /// </summary>
    public class MockJwtTokenService : IJwtTokenService
    {
        private readonly Mock<IJwtTokenService> _mock;

        public MockJwtTokenService()
        {
            _mock = new Mock<IJwtTokenService>();
            
            // Configurações padrão para testes
            _mock.Setup(x => x.GenerateAccessTokenAsync(It.IsAny<User>()))
                .ReturnsAsync(() => $"valid_test_token_{DateTime.Now.Ticks}"); // Generate unique tokens
                
            _mock.Setup(x => x.GenerateRefreshTokenAsync(It.IsAny<User>()))
                .ReturnsAsync("valid_refresh_token_with_minimum_32_characters_required_by_validator");
                
            _mock.Setup(x => x.ValidateTokenAsync(It.IsAny<string>()))
                .ReturnsAsync(true);
                
            _mock.Setup(x => x.GetUserIdFromTokenAsync(It.IsAny<string>()))
                .ReturnsAsync(Guid.Parse("12345678-1234-1234-1234-123456789012")); // Test user ID
                
            _mock.Setup(x => x.RevokeTokenAsync(It.IsAny<string>()))
                .ReturnsAsync(true);
                
            _mock.Setup(x => x.RefreshTokenAsync(It.IsAny<string>()))
                .ReturnsAsync(() => $"new_valid_test_token_with_minimum_32_characters_for_validation_{DateTime.Now.Ticks}");
        }

        public Task<string> GenerateAccessTokenAsync(User user)
        {
            return _mock.Object.GenerateAccessTokenAsync(user);
        }

        public Task<string> GenerateRefreshTokenAsync(User user)
        {
            return _mock.Object.GenerateRefreshTokenAsync(user);
        }

        public Task<Guid?> GetUserIdFromTokenAsync(string token)
        {
            return _mock.Object.GetUserIdFromTokenAsync(token);
        }

        public Task<string?> RefreshTokenAsync(string refreshToken)
        {
            return _mock.Object.RefreshTokenAsync(refreshToken);
        }

        public Task<bool> RevokeTokenAsync(string token)
        {
            return _mock.Object.RevokeTokenAsync(token);
        }

        public Task<bool> ValidateTokenAsync(string token)
        {
            return _mock.Object.ValidateTokenAsync(token);
        }
    }
}
