using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Commands.UserHolidayPreference;
using SmartAlarm.Application.DTOs.UserHolidayPreference;
using SmartAlarm.Domain.Abstractions;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Application.Handlers.UserHolidayPreference
{
    /// <summary>
    /// Handler para atualizar uma preferência de feriado do usuário.
    /// </summary>
    public class UpdateUserHolidayPreferenceHandler : IRequestHandler<UpdateUserHolidayPreferenceCommand, UserHolidayPreferenceResponseDto>
    {
        private readonly IUserHolidayPreferenceRepository _userHolidayPreferenceRepository;
        private readonly ILogger<UpdateUserHolidayPreferenceHandler> _logger;

        public UpdateUserHolidayPreferenceHandler(
            IUserHolidayPreferenceRepository userHolidayPreferenceRepository,
            ILogger<UpdateUserHolidayPreferenceHandler> logger)
        {
            _userHolidayPreferenceRepository = userHolidayPreferenceRepository;
            _logger = logger;
        }

        public async Task<UserHolidayPreferenceResponseDto> Handle(UpdateUserHolidayPreferenceCommand request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            _logger.LogInformation("Updating user holiday preference with ID: {PreferenceId}", request.Id);

            // Buscar preferência existente
            var preference = await _userHolidayPreferenceRepository.GetByIdAsync(request.Id);
            if (preference == null)
            {
                throw new ArgumentException($"Preferência com ID {request.Id} não encontrada.");
            }

            // Validar DelayInMinutes quando action for Delay
            if (request.Action == HolidayPreferenceAction.Delay && (!request.DelayInMinutes.HasValue || request.DelayInMinutes <= 0))
            {
                throw new ArgumentException("DelayInMinutes é obrigatório e deve ser maior que zero quando a ação for Delay.");
            }

            // Atualizar propriedades
            preference.UpdateAction(request.Action, request.DelayInMinutes);
            
            if (request.IsEnabled)
            {
                preference.Enable();
            }
            else
            {
                preference.Disable();
            }

            await _userHolidayPreferenceRepository.UpdateAsync(preference);

            _logger.LogInformation("Updated UserHolidayPreference successfully with ID: {PreferenceId}", preference.Id);

            // Retornar DTO atualizado com dados relacionados
            var updatedPreference = await _userHolidayPreferenceRepository.GetByIdAsync(preference.Id);
            return MapToResponseDto(updatedPreference!);
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
