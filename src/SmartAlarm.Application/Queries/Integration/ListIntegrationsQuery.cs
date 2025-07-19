using MediatR;
using SmartAlarm.Application.DTOs.Integration;
using SmartAlarm.Application.DTOs.Common;
using System.Collections.Generic;
using System;

namespace SmartAlarm.Application.Queries.Integration
{
    /// <summary>
    /// Query para listar integrações com paginação.
    /// </summary>
    public class ListIntegrationsQuery : IRequest<PaginatedResponseDto<IntegrationResponseDto>>
    {
        /// <summary>
        /// Parâmetros de paginação.
        /// </summary>
        public PaginationDto Pagination { get; set; } = new();

        /// <summary>
        /// Filtro por status ativo (opcional).
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// Filtro por provedor (opcional).
        /// </summary>
        public string? Provider { get; set; }

        /// <summary>
        /// Filtro por nome (busca parcial, opcional).
        /// </summary>
        public string? NameFilter { get; set; }

        /// <summary>
        /// Filtro por ID do alarme (opcional).
        /// </summary>
        public Guid? AlarmId { get; set; }

        /// <summary>
        /// Construtor padrão.
        /// </summary>
        public ListIntegrationsQuery()
        {
        }

        /// <summary>
        /// Construtor com parâmetros.
        /// </summary>
        public ListIntegrationsQuery(PaginationDto pagination, bool? isActive = null, 
            string? provider = null, string? nameFilter = null, Guid? alarmId = null)
        {
            Pagination = pagination;
            IsActive = isActive;
            Provider = provider;
            NameFilter = nameFilter;
            AlarmId = alarmId;
        }
    }
}
