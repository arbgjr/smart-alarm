using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace SmartAlarm.Api.Services
{
    /// <summary>
    /// Serviço para controle de consentimento do usuário (LGPD).
    /// </summary>
    public class UserConsentService : IUserConsentService
    {
        private readonly ILogger<UserConsentService> _logger;
        private static readonly ConcurrentDictionary<string, bool> ConsentDatabase = new();

        public UserConsentService(ILogger<UserConsentService> logger)
        {
            _logger = logger;
        }

        public void RegisterConsent(string userId, bool consentGiven)
        {
            ConsentDatabase[userId] = consentGiven;
            _logger.LogInformation("[LGPD] Consentimento registrado para usuário {UserId}: {Consent}", userId, consentGiven);
        }

        public bool HasConsent(string userId)
        {
            return ConsentDatabase.TryGetValue(userId, out var consent) && consent;
        }
    }

    public interface IUserConsentService
    {
        void RegisterConsent(string userId, bool consentGiven);
        bool HasConsent(string userId);
    }
}
