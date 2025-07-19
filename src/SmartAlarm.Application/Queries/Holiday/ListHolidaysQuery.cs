using System.Collections.Generic;
using MediatR;
using SmartAlarm.Application.DTOs.Holiday;
using SmartAlarm.Application.DTOs.Common;
using System;

namespace SmartAlarm.Application.Queries.Holiday
{
    /// <summary>
    /// Query para listar todos os feriados com paginação.
    /// </summary>
    public class ListHolidaysQuery : IRequest<PaginatedResponseDto<HolidayResponseDto>>
    {
        /// <summary>
        /// Parâmetros de paginação.
        /// </summary>
        public PaginationDto Pagination { get; set; } = new();

        /// <summary>
        /// Filtro por data inicial (opcional).
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Filtro por data final (opcional).
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Filtro por descrição (busca parcial, opcional).
        /// </summary>
        public string? DescriptionFilter { get; set; }

        /// <summary>
        /// Filtro por feriados recorrentes (opcional).
        /// </summary>
        public bool? IsRecurring { get; set; }

        /// <summary>
        /// Construtor padrão.
        /// </summary>
        public ListHolidaysQuery()
        {
        }

        /// <summary>
        /// Construtor com parâmetros.
        /// </summary>
        public ListHolidaysQuery(PaginationDto pagination, DateTime? startDate = null, DateTime? endDate = null, 
            string? descriptionFilter = null, bool? isRecurring = null)
        {
            Pagination = pagination;
            StartDate = startDate;
            EndDate = endDate;
            DescriptionFilter = descriptionFilter;
            IsRecurring = isRecurring;
        }
    }
}
