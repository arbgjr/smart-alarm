using MediatR;
using SmartAlarm.Application.DTOs.User;
using SmartAlarm.Application.DTOs.Common;
using System.Collections.Generic;

namespace SmartAlarm.Application.Queries.User
{
    /// <summary>
    /// Query para listar usuários com paginação.
    /// </summary>
    public class ListUsersQuery : IRequest<PaginatedResponseDto<UserResponseDto>>
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
        /// Filtro por email (opcional).
        /// </summary>
        public string? EmailFilter { get; set; }

        /// <summary>
        /// Construtor padrão.
        /// </summary>
        public ListUsersQuery()
        {
        }

        /// <summary>
        /// Construtor com parâmetros.
        /// </summary>
        public ListUsersQuery(PaginationDto pagination, bool? isActive = null, string? emailFilter = null)
        {
            Pagination = pagination;
            IsActive = isActive;
            EmailFilter = emailFilter;
        }
    }
}
