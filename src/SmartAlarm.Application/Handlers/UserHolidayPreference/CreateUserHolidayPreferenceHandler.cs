using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Commands.UserHolidayPreference;
using SmartAlarm.Application.DTOs.UserHolidayPreference;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Application.Handlers.UserHolidayPreference
{
    /// <summary>
    /// Handler para criar uma nova preferência de feriado do usuário.
    /// </summary>
    public class CreateUserHolidayPreferenceHandler : IRequestHandler<CreateUserHolidayPreferenceCommand, UserHolidayPreferenceResponseDto>
    {
        private readonly IUserHolidayPreferenceRepository _userHolidayPreferenceRepository;
        private readonly IUserRepository _userRepository;
        private readonly IHolidayRepository _holidayRepository;
        private readonly ILogger<CreateUserHolidayPreferenceHandler> _logger;

        public CreateUserHolidayPreferenceHandler(
            IUserHolidayPreferenceRepository userHolidayPreferenceRepository,
            IUserRepository userRepository,
            IHolidayRepository holidayRepository,
            ILogger<CreateUserHolidayPreferenceHandler> logger)
        {
            _userHolidayPreferenceRepository = userHolidayPreferenceRepository;
            _userRepository = userRepository;
            _holidayRepository = holidayRepository;
            _logger = logger;
        }

        public async Task<UserHolidayPreferenceResponseDto> Handle(CreateUserHolidayPreferenceCommand request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            _logger.LogInformation("Creating user holiday preference for User: {UserId}, Holiday: {HolidayId}", 
                request.UserId, request.HolidayId);

            // Verificar se usuário existe
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                throw new ArgumentException($"Usuário com ID {request.UserId} não encontrado.");
            }

            // Verificar se feriado existe
            var holiday = await _holidayRepository.GetByIdAsync(request.HolidayId);
            if (holiday == null)
            {
                throw new ArgumentException($"Feriado com ID {request.HolidayId} não encontrado.");
            }

            // Verificar se já existe preferência para este usuário e feriado
            var existingPreference = await _userHolidayPreferenceRepository.GetByUserAndHolidayAsync(request.UserId, request.HolidayId);
            if (existingPreference != null)
            {
                throw new ArgumentException($"Já existe uma preferência para o usuário {request.UserId} e feriado {request.HolidayId}.");
            }

            // Validar DelayInMinutes quando action for Delay
            if (request.Action == HolidayPreferenceAction.Delay && (!request.DelayInMinutes.HasValue || request.DelayInMinutes <= 0))
            {
                throw new ArgumentException("DelayInMinutes é obrigatório e deve ser maior que zero quando a ação for Delay.");
            }

            // Criar nova preferência
            var preference = new Domain.Entities.UserHolidayPreference(
                Guid.NewGuid(),
                request.UserId,
                request.HolidayId,
                request.IsEnabled,
                request.Action,
                request.DelayInMinutes
            );

            await _userHolidayPreferenceRepository.AddAsync(preference);

            _logger.LogInformation("Created UserHolidayPreference successfully with ID: {PreferenceId}", preference.Id);

            // Retornar DTO com dados relacionados
            return MapToResponseDto(preference, user, holiday);
        }

        private static UserHolidayPreferenceResponseDto MapToResponseDto(Domain.Entities.UserHolidayPreference preference, Domain.Entities.User user, Domain.Entities.Holiday holiday)
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
                User = new Application.DTOs.User.UserResponseDto
                {
                    Id = user.Id,
                    Name = user.Name.Value,
                    Email = user.Email.Address,
                    IsActive = user.IsActive
                },
                Holiday = new Application.DTOs.Holiday.HolidayResponseDto
                {
                    Id = holiday.Id,
                    Date = holiday.Date,
                    Description = holiday.Description,
                    CreatedAt = holiday.CreatedAt
                }
            };
        }
    }
}
