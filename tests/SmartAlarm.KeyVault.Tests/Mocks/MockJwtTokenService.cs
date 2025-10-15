using SmartAlarm.Domain.Abstractions;
using System;
using System.Threading.Tasks;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.KeyVault.Tests.Mocks
{
    /// <summary>
    /// Implementação mock simplificada do serviço JWT para testes do KeyVault
    /// </summary>
    public class MockJwtTokenService : IJwtTokenService
    {
        public Task<string> GenerateAccessTokenAsync(User user)
        {
            return Task.FromResult("test_access_token");
        }

        public Task<string> GenerateRefreshTokenAsync(User user)
        {
            return Task.FromResult("test_refresh_token");
        }

        public Task<Guid?> GetUserIdFromTokenAsync(string token)
        {
            return Task.FromResult<Guid?>(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        }

        public Task<string?> RefreshTokenAsync(string refreshToken)
        {
            return Task.FromResult<string?>("new_test_access_token");
        }

        public Task<bool> RevokeTokenAsync(string token)
        {
            return Task.FromResult(true);
        }

        public Task<bool> ValidateTokenAsync(string token)
        {
            return Task.FromResult(true);
        }
    }
}
