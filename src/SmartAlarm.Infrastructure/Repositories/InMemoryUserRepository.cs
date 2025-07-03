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

        public Task AddAsync(User user)
        {
            _users[user.Id] = user;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id)
        {
            _users.TryRemove(id, out _);
            return Task.CompletedTask;
        }

        public Task<User> GetByEmailAsync(string email)
        {
            var user = _users.Values.FirstOrDefault(u => u.Email == email);
            return Task.FromResult(user);
        }

        public Task<User> GetByIdAsync(Guid id)
        {
            _users.TryGetValue(id, out var user);
            return Task.FromResult(user);
        }

        public Task UpdateAsync(User user)
        {
            _users[user.Id] = user;
            return Task.CompletedTask;
        }
    }
}
