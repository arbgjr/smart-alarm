using System;
using MediatR;
using SmartAlarm.Application.DTOs.UserHolidayPreference;

namespace SmartAlarm.Application.Queries.UserHolidayPreference
{
    /// <summary>
    /// Query para buscar uma preferência específica por usuário e feriado.
    /// </summary>
    public class GetUserHolidayPreferenceByUserAndHolidayQuery : IRequest<UserHolidayPreferenceResponseDto?>
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
        /// Indica se deve incluir os dados relacionados (User e Holiday).
        /// </summary>
        public bool IncludeRelated { get; set; } = true;

        /// <summary>
        /// Construtor padrão.
        /// </summary>
        public GetUserHolidayPreferenceByUserAndHolidayQuery()
        {
        }

        /// <summary>
        /// Construtor parametrizado.
        /// </summary>
        public GetUserHolidayPreferenceByUserAndHolidayQuery(Guid userId, Guid holidayId, bool includeRelated = true)
        {
            UserId = userId;
            HolidayId = holidayId;
            IncludeRelated = includeRelated;
        }
    }
}
