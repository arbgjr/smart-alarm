using System;
using MediatR;

namespace SmartAlarm.Application.Commands.UserHolidayPreference
{
    /// <summary>
    /// Command para deletar uma preferência de feriado do usuário.
    /// </summary>
    public class DeleteUserHolidayPreferenceCommand : IRequest<bool>
    {
        /// <summary>
        /// ID da preferência a ser deletada.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Construtor padrão.
        /// </summary>
        public DeleteUserHolidayPreferenceCommand()
        {
        }

        /// <summary>
        /// Construtor parametrizado.
        /// </summary>
        public DeleteUserHolidayPreferenceCommand(Guid id)
        {
            Id = id;
        }
    }
}
