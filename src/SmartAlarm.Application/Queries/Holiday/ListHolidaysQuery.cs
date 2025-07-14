using System.Collections.Generic;
using MediatR;
using SmartAlarm.Application.DTOs.Holiday;

namespace SmartAlarm.Application.Queries.Holiday
{
    /// <summary>
    /// Query para listar todos os feriados.
    /// </summary>
    public class ListHolidaysQuery : IRequest<IEnumerable<HolidayResponseDto>>
    {
    }
}
