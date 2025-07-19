using System;
using MediatR;
using SmartAlarm.Application.DTOs.Holiday;

namespace SmartAlarm.Application.Queries.Holiday
{
    /// <summary>
    /// Query para buscar um feriado por ID.
    /// </summary>
    public class GetHolidayByIdQuery : IRequest<HolidayResponseDto?>
    {
        /// <summary>
        /// ID do feriado a ser buscado.
        /// </summary>
        public Guid Id { get; set; }

        public GetHolidayByIdQuery() { }

        public GetHolidayByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
