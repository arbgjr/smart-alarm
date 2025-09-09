using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Services;
using SmartAlarm.Application.Services.External;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;

namespace SmartAlarm.Infrastructure.Services
{
    /// <summary>
    /// Implementação do serviço principal de lógica inteligente de alarmes
    /// </summary>
    public class SmartAlarmService : ISmartAlarmService
    {
        private readonly IHolidayRepository _holidayRepository;
        private readonly IUserHolidayPreferenceRepository _userHolidayPreferenceRepository;
        private readonly IExceptionPeriodRepository _exceptionPeriodRepository;
        private readonly IHolidayCacheService _holidayCacheService;
        private readonly IGoogleCalendarService _googleCalendarService;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<SmartAlarmService> _logger;
        private readonly Dictionary<Guid, string> _disableReasons = new();

        public SmartAlarmService(
            IHolidayRepository holidayRepository,
            IUserHolidayPreferenceRepository userHolidayPreferenceRepository,
            IExceptionPeriodRepository exceptionPeriodRepository,
            IHolidayCacheService holidayCacheService,
            IGoogleCalendarService googleCalendarService,
            IUserRepository userRepository,
            ILogger<SmartAlarmService> logger)
        {
            _holidayRepository = holidayRepository;
            _userHolidayPreferenceRepository = userHolidayPreferenceRepository;
            _exceptionPeriodRepository = exceptionPeriodRepository;
            _holidayCacheService = holidayCacheService;
            _googleCalendarService = googleCalendarService;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<bool> ShouldAlarmTriggerAsync(Alarm alarm, CancellationToken cancellationToken = default)
        {
            if (!alarm.Enabled)
            {
                _logger.LogDebug("Alarme {AlarmId} está desabilitado", alarm.Id);
                return false;
            }

            var now = DateTime.Now;

            // 1. Verificar se está em período de exceção
            var hasExceptionPeriod = await _exceptionPeriodRepository.HasActivePeriodOnDateAsync(
                alarm.UserId, now);
            
            if (hasExceptionPeriod)
            {
                _logger.LogInformation("Alarme {AlarmId} desativado por período de exceção", alarm.Id);
                _disableReasons[alarm.Id] = "Período de exceção ativo";
                return false;
            }

            // 2. Verificar se hoje é feriado
            var isHoliday = await CheckHolidayAsync(alarm.UserId, now, cancellationToken);
            if (isHoliday)
            {
                var preference = await GetUserHolidayPreferenceAsync(alarm.UserId, now, cancellationToken);
                
                if (preference?.DisableAlarms == true)
                {
                    _logger.LogInformation("Alarme {AlarmId} desativado por feriado", alarm.Id);
                    _disableReasons[alarm.Id] = "Feriado nacional/estadual";
                    return false;
                }
            }

            // 3. Verificar calendário do usuário (férias/folga)
            var isOnVacation = await CheckUserCalendarAsync(alarm.UserId, now, cancellationToken);
            if (isOnVacation)
            {
                _logger.LogInformation("Alarme {AlarmId} desativado por férias/folga no calendário", alarm.Id);
                _disableReasons[alarm.Id] = "Férias ou dia de folga no calendário";
                return false;
            }

            // 4. Verificar schedules do alarme
            var shouldTrigger = alarm.Schedules.Any(s => 
                s.IsActive && 
                s.ShouldTriggerToday() &&
                s.Time.Hour == now.Hour && 
                s.Time.Minute == now.Minute);

            if (shouldTrigger)
            {
                _logger.LogInformation("Alarme {AlarmId} deve disparar agora", alarm.Id);
                _disableReasons.Remove(alarm.Id); // Limpar razão se existir
            }

            return shouldTrigger;
        }

        public async Task<string?> GetDisableReasonAsync(Guid alarmId, CancellationToken cancellationToken = default)
        {
            return _disableReasons.TryGetValue(alarmId, out var reason) ? reason : null;
        }

        public async Task<bool> IsTodayHolidayAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await CheckHolidayAsync(userId, DateTime.Today, cancellationToken);
        }

        public async Task<bool> IsUserOnVacationAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await CheckUserCalendarAsync(userId, DateTime.Today, cancellationToken);
        }

        private async Task<bool> CheckHolidayAsync(Guid userId, DateTime date, CancellationToken cancellationToken)
        {
            try
            {
                // Obter informações do usuário para determinar país/estado
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Usuário {UserId} não encontrado", userId);
                    return false;
                }

                // Verificar cache primeiro
                var country = user.Country ?? "BR"; // Default para Brasil
                var state = user.State;

                var isHoliday = await _holidayCacheService.IsHolidayAsync(date, country, state, cancellationToken);
                
                if (isHoliday)
                {
                    _logger.LogDebug("Data {Date} é feriado em {Country}{State}", 
                        date, country, string.IsNullOrEmpty(state) ? "" : $"-{state}");
                    return true;
                }

                // Verificar também no banco de dados local
                var holiday = await _holidayRepository.GetByDateAsync(date, cancellationToken);
                return holiday != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar feriado para usuário {UserId} na data {Date}", 
                    userId, date);
                return false;
            }
        }

        private async Task<UserHolidayPreference?> GetUserHolidayPreferenceAsync(
            Guid userId, 
            DateTime date, 
            CancellationToken cancellationToken)
        {
            try
            {
                var holiday = await _holidayRepository.GetByDateAsync(date, cancellationToken);
                if (holiday == null)
                    return null;

                return await _userHolidayPreferenceRepository.GetByUserAndHolidayAsync(
                    userId, holiday.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter preferência de feriado para usuário {UserId}", userId);
                return null;
            }
        }

        private async Task<bool> CheckUserCalendarAsync(Guid userId, DateTime date, CancellationToken cancellationToken)
        {
            try
            {
                // Verificar se o usuário tem integração com Google Calendar
                var isAuthorized = await _googleCalendarService.IsAuthorizedAsync(userId, cancellationToken);
                if (!isAuthorized)
                {
                    _logger.LogDebug("Usuário {UserId} não tem integração com Google Calendar", userId);
                    return false;
                }

                // Verificar se tem eventos de férias/folga
                var hasVacation = await _googleCalendarService.HasVacationOrDayOffAsync(
                    userId, date, cancellationToken);

                if (hasVacation)
                {
                    _logger.LogInformation("Usuário {UserId} tem evento de férias/folga em {Date}", 
                        userId, date);
                }

                return hasVacation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar calendário para usuário {UserId}", userId);
                return false; // Em caso de erro, não desativar o alarme
            }
        }
    }
}