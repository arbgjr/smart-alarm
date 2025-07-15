using System;
using MediatR;
using SmartAlarm.Application.DTOs.UserHolidayPreference;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Application.Commands.UserHolidayPreference
{
    /// <summary>
    /// Command para atualizar uma preferência de feriado do usuário.
    /// </summary>
    public class UpdateUserHolidayPreferenceCommand : IRequest<UserHolidayPreferenceResponseDto>
    {
        /// <summary>
        /// ID da preferência a ser atualizada.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Indica se a preferência está ativa.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Ação a ser executada no feriado.
        /// </summary>
        public HolidayPreferenceAction Action { get; set; }

        /// <summary>
        /// Atraso em minutos (obrigatório quando Action = Delay).
        /// </summary>
        public int? DelayInMinutes { get; set; }

        /// <summary>
        /// Construtor padrão.
        /// </summary>
        public UpdateUserHolidayPreferenceCommand()
        {
        }

        /// <summary>
        /// Construtor parametrizado.
        /// </summary>
        public UpdateUserHolidayPreferenceCommand(Guid id, bool isEnabled, HolidayPreferenceAction action, int? delayInMinutes = null)
        {
            Id = id;
            IsEnabled = isEnabled;
            Action = action;
            DelayInMinutes = delayInMinutes;
        }
    }
}
