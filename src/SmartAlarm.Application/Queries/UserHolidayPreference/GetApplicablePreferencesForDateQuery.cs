using System;
using System.Collections.Generic;
using MediatR;
using SmartAlarm.Application.DTOs.UserHolidayPreference;

namespace SmartAlarm.Application.Queries.UserHolidayPreference
{
    /// <summary>
    /// Query para buscar preferências aplicáveis a uma data específica.
    /// </summary>
    public class GetApplicablePreferencesForDateQuery : IRequest<IEnumerable<UserHolidayPreferenceResponseDto>>
    {
        /// <summary>
        /// ID do usuário.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Data para verificar preferências aplicáveis.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Indica se deve incluir os dados relacionados (User e Holiday).
        /// </summary>
        public bool IncludeRelated { get; set; } = true;

        /// <summary>
        /// Construtor padrão.
        /// </summary>
        public GetApplicablePreferencesForDateQuery()
        {
        }

        /// <summary>
        /// Construtor parametrizado.
        /// </summary>
        public GetApplicablePreferencesForDateQuery(Guid userId, DateTime date, bool includeRelated = true)
        {
            UserId = userId;
            Date = date;
            IncludeRelated = includeRelated;
        }
    }
}
