using System;
using System.Collections.Generic;
using MediatR;
using SmartAlarm.Application.DTOs.UserHolidayPreference;

namespace SmartAlarm.Application.Queries.UserHolidayPreference
{
    /// <summary>
    /// Query para listar preferências de feriado de um usuário.
    /// </summary>
    public class ListUserHolidayPreferencesQuery : IRequest<IEnumerable<UserHolidayPreferenceResponseDto>>
    {
        /// <summary>
        /// ID do usuário para filtrar as preferências.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Indica se deve incluir apenas preferências ativas.
        /// </summary>
        public bool? OnlyEnabled { get; set; }

        /// <summary>
        /// Indica se deve incluir os dados relacionados (User e Holiday).
        /// </summary>
        public bool IncludeRelated { get; set; } = true;

        /// <summary>
        /// Construtor padrão.
        /// </summary>
        public ListUserHolidayPreferencesQuery()
        {
        }

        /// <summary>
        /// Construtor parametrizado.
        /// </summary>
        public ListUserHolidayPreferencesQuery(Guid userId, bool? onlyEnabled = null, bool includeRelated = true)
        {
            UserId = userId;
            OnlyEnabled = onlyEnabled;
            IncludeRelated = includeRelated;
        }
    }
}
