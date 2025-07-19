using System;
using MediatR;
using SmartAlarm.Application.DTOs.UserHolidayPreference;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Application.Commands.UserHolidayPreference
{
    /// <summary>
    /// Command para criar uma nova preferência de feriado do usuário.
    /// </summary>
    public class CreateUserHolidayPreferenceCommand : IRequest<UserHolidayPreferenceResponseDto>
    {
        /// <summary>
        /// ID do usuário.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// ID do feriado.
        /// </summary>
        public Guid HolidayId { get; set; }

        /// <summary>
        /// Indica se a preferência está ativa.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Ação a ser executada no feriado.
        /// </summary>
        public HolidayPreferenceAction Action { get; set; }

        /// <summary>
        /// Atraso em minutos (obrigatório quando Action = Delay).
        /// </summary>
        public int? DelayInMinutes { get; set; }

        /// <summary>
        /// Construtor para criar command a partir de DTO.
        /// </summary>
        public CreateUserHolidayPreferenceCommand()
        {
        }

        /// <summary>
        /// Construtor parametrizado.
        /// </summary>
        public CreateUserHolidayPreferenceCommand(Guid userId, Guid holidayId, bool isEnabled, HolidayPreferenceAction action, int? delayInMinutes = null)
        {
            UserId = userId;
            HolidayId = holidayId;
            IsEnabled = isEnabled;
            Action = action;
            DelayInMinutes = delayInMinutes;
        }
    }
}
