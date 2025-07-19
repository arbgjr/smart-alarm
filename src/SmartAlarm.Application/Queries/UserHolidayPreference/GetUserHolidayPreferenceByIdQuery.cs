using System;
using MediatR;
using SmartAlarm.Application.DTOs.UserHolidayPreference;

namespace SmartAlarm.Application.Queries.UserHolidayPreference
{
    /// <summary>
    /// Query para buscar uma preferência de feriado do usuário por ID.
    /// </summary>
    public class GetUserHolidayPreferenceByIdQuery : IRequest<UserHolidayPreferenceResponseDto?>
    {
        /// <summary>
        /// ID da preferência a ser buscada.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Indica se deve incluir os dados relacionados (User e Holiday).
        /// </summary>
        public bool IncludeRelated { get; set; } = true;

        /// <summary>
        /// Construtor padrão.
        /// </summary>
        public GetUserHolidayPreferenceByIdQuery()
        {
        }

        /// <summary>
        /// Construtor parametrizado.
        /// </summary>
        public GetUserHolidayPreferenceByIdQuery(Guid id, bool includeRelated = true)
        {
            Id = id;
            IncludeRelated = includeRelated;
        }
    }
}
