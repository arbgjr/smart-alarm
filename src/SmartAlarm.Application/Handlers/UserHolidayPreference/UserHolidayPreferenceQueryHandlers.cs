using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.DTOs.UserHolidayPreference;
using SmartAlarm.Application.Queries.UserHolidayPreference;
using SmartAlarm.Domain.Abstractions;

namespace SmartAlarm.Application.Handlers.UserHolidayPreference
{
    /// <summary>
    /// Handlers para queries de UserHolidayPreference.
    /// </summary>
    public class UserHolidayPreferenceQueryHandlers :
        IRequestHandler<GetUserHolidayPreferenceByIdQuery, UserHolidayPreferenceResponseDto?>,
        IRequestHandler<ListUserHolidayPreferencesQuery, IEnumerable<UserHolidayPreferenceResponseDto>>,
        IRequestHandler<GetApplicablePreferencesForDateQuery, IEnumerable<UserHolidayPreferenceResponseDto>>
    {
        private readonly IUserHolidayPreferenceRepository _userHolidayPreferenceRepository;
        private readonly ILogger<UserHolidayPreferenceQueryHandlers> _logger;

        public UserHolidayPreferenceQueryHandlers(
            IUserHolidayPreferenceRepository userHolidayPreferenceRepository,
            ILogger<UserHolidayPreferenceQueryHandlers> logger)
        {
            _userHolidayPreferenceRepository = userHolidayPreferenceRepository;
            _logger = logger;
        }

        public async Task<UserHolidayPreferenceResponseDto?> Handle(GetUserHolidayPreferenceByIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting user holiday preference by ID: {PreferenceId}", request.Id);

            var preference = await _userHolidayPreferenceRepository.GetByIdAsync(request.Id);
            return preference != null ? MapToResponseDto(preference) : null;
        }

        public async Task<IEnumerable<UserHolidayPreferenceResponseDto>> Handle(ListUserHolidayPreferencesQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Listing user holiday preferences for User: {UserId}, OnlyEnabled: {OnlyEnabled}", 
                request.UserId, request.OnlyEnabled);

            IEnumerable<Domain.Entities.UserHolidayPreference> preferences;

            if (request.OnlyEnabled.HasValue)
            {
                preferences = request.OnlyEnabled.Value
                    ? await _userHolidayPreferenceRepository.GetActiveByUserIdAsync(request.UserId)
                    : await _userHolidayPreferenceRepository.GetByUserIdAsync(request.UserId);
            }
            else
            {
                preferences = await _userHolidayPreferenceRepository.GetByUserIdAsync(request.UserId);
            }

            return preferences.Select(MapToResponseDto).ToList();
        }

        public async Task<IEnumerable<UserHolidayPreferenceResponseDto>> Handle(GetApplicablePreferencesForDateQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting applicable preferences for User: {UserId} on Date: {Date}", 
                request.UserId, request.Date);

            var preferences = await _userHolidayPreferenceRepository.GetApplicableForDateAsync(request.UserId, request.Date);
            return preferences.Select(MapToResponseDto).ToList();
        }

        private static UserHolidayPreferenceResponseDto MapToResponseDto(Domain.Entities.UserHolidayPreference preference)
        {
            return new UserHolidayPreferenceResponseDto
            {
                Id = preference.Id,
                UserId = preference.UserId,
                HolidayId = preference.HolidayId,
                IsEnabled = preference.IsEnabled,
                Action = preference.Action,
                DelayInMinutes = preference.DelayInMinutes,
                CreatedAt = preference.CreatedAt,
                UpdatedAt = preference.UpdatedAt,
                User = preference.User != null ? new Application.DTOs.User.UserResponseDto
                {
                    Id = preference.User.Id,
                    Name = preference.User.Name.Value,
                    Email = preference.User.Email.Address,
                    IsActive = preference.User.IsActive
                } : null,
                Holiday = preference.Holiday != null ? new Application.DTOs.Holiday.HolidayResponseDto
                {
                    Id = preference.Holiday.Id,
                    Date = preference.Holiday.Date,
                    Description = preference.Holiday.Description,
                    CreatedAt = preference.Holiday.CreatedAt
                } : null
            };
        }
    }
}
