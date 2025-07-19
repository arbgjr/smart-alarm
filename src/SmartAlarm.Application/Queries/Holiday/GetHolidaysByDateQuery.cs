using System;
using System.Collections.Generic;
using MediatR;
using SmartAlarm.Application.DTOs.Holiday;

namespace SmartAlarm.Application.Queries.Holiday
{
    /// <summary>
    /// Query para buscar feriados por data específica.
    /// </summary>
    public class GetHolidaysByDateQuery : IRequest<IEnumerable<HolidayResponseDto>>
    {
        /// <summary>
        /// Data para buscar feriados.
        /// </summary>
        public DateTime Date { get; set; }

        public GetHolidaysByDateQuery() { }

        public GetHolidaysByDateQuery(DateTime date)
        {
            Date = date;
        }
    }
}
