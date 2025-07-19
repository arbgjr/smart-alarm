using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;

namespace SmartAlarm.Domain.Services
{
    /// <summary>
    /// Implementação concreta do serviço de domínio para operações de negócio relacionadas a usuários.
    /// </summary>
    public class UserDomainService : IUserDomainService
    {
        private readonly IUserRepository _userRepository;
        private readonly IAlarmRepository _alarmRepository;
        private readonly ILogger<UserDomainService> _logger;

        public UserDomainService(IUserRepository userRepository, IAlarmRepository alarmRepository, ILogger<UserDomainService> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _alarmRepository = alarmRepository ?? throw new ArgumentNullException(nameof(alarmRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> IsEmailAlreadyInUseAsync(string email, Guid? excludeUserId = null)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            return user != null && (!excludeUserId.HasValue || user.Id != excludeUserId.Value);
        }

        public async Task<bool> CanActivateUserAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Usuário não encontrado: {UserId}", userId);
                return false;
            }
            return !user.IsActive;
        }

        public async Task<bool> CanDeactivateUserAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Usuário não encontrado: {UserId}", userId);
                return false;
            }
            // Exemplo: não permitir desativar se houver alarmes ativos
            var hasActiveAlarms = await HasActiveAlarmsAsync(userId);
            return user.IsActive && !hasActiveAlarms;
        }

        public async Task<bool> HasActiveAlarmsAsync(Guid userId)
        {
            var alarms = await _alarmRepository.GetByUserIdAsync(userId);
            return alarms != null && alarms.Any(a => a.Enabled);
        }
    }
}
