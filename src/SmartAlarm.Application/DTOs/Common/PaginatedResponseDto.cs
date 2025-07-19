using System;
using System.Collections.Generic;

namespace SmartAlarm.Application.DTOs.Common
{
    /// <summary>
    /// DTO para resposta paginada.
    /// </summary>
    /// <typeparam name="T">Tipo dos dados</typeparam>
    public class PaginatedResponseDto<T>
    {
        /// <summary>
        /// Dados da página atual.
        /// </summary>
        public IEnumerable<T> Items { get; set; } = [];

        /// <summary>
        /// Total de itens disponíveis.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Página atual.
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Tamanho da página.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total de páginas.
        /// </summary>
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        /// <summary>
        /// Indica se há página anterior.
        /// </summary>
        public bool HasPreviousPage => Page > 1;

        /// <summary>
        /// Indica se há próxima página.
        /// </summary>
        public bool HasNextPage => Page < TotalPages;

        /// <summary>
        /// Campo de ordenação usado.
        /// </summary>
        public string? OrderBy { get; set; }

        /// <summary>
        /// Direção da ordenação.
        /// </summary>
        public string OrderDirection { get; set; } = "asc";

        /// <summary>
        /// Construtor padrão.
        /// </summary>
        public PaginatedResponseDto()
        {
        }

        /// <summary>
        /// Construtor com parâmetros.
        /// </summary>
        public PaginatedResponseDto(IEnumerable<T> items, int totalCount, int page, int pageSize, string? orderBy = null, string orderDirection = "asc")
        {
            Items = items;
            TotalCount = totalCount;
            Page = page;
            PageSize = pageSize;
            OrderBy = orderBy;
            OrderDirection = orderDirection;
        }
    }
}
