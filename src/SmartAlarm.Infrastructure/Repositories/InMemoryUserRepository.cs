using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;

namespace SmartAlarm.Infrastructure.Repositories
{
    /// <summary>
    /// In-memory implementation of IUserRepository for development and testing.
    /// </summary>
    public class InMemoryUserRepository : IUserRepository
    {
        private readonly ConcurrentDictionary<Guid, User> _users = new();

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            _users[user.Id] = user;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id)
        {
            _users.TryRemove(id, out _);
            return Task.CompletedTask;
        }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var user = _users.Values.FirstOrDefault(u => u.Email.Address == email);
            return Task.FromResult(user);
        }

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _users.TryGetValue(id, out var user);
            return Task.FromResult(user);
        }

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            _users[user.Id] = user;
            return Task.CompletedTask;
        }

        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var user = _users.Values.FirstOrDefault(u => u.Email.Address.Equals(email, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(user);
        }

        public Task<User?> FindByExternalProviderAsync(string provider, string providerId, CancellationToken cancellationToken = default)
        {
            var user = _users.Values.FirstOrDefault(u => 
                u.ExternalProvider == provider && 
                u.ExternalProviderId == providerId);
            return Task.FromResult(user);
        }

        public Task<IEnumerable<User>> GetAllAsync()
        {
            return Task.FromResult(_users.Values.AsEnumerable());
        }
    }
}

