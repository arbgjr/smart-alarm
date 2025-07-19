using System;
using System.ComponentModel.DataAnnotations;

namespace SmartAlarm.Application.DTOs.Common
{
    /// <summary>
    /// DTO para parâmetros de paginação.
    /// </summary>
    public class PaginationDto
    {
        /// <summary>
        /// Número da página (base 1).
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
        public int Page { get; set; } = 1;

        /// <summary>
        /// Tamanho da página (máximo 100).
        /// </summary>
        [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100")]
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// Campo para ordenação (opcional).
        /// </summary>
        public string? OrderBy { get; set; }

        /// <summary>
        /// Direção da ordenação (asc/desc).
        /// </summary>
        public string OrderDirection { get; set; } = "asc";

        /// <summary>
        /// Calcula o número de itens a pular.
        /// </summary>
        public int Skip => (Page - 1) * PageSize;

        /// <summary>
        /// Valida se a direção da ordenação é válida.
        /// </summary>
        public bool IsValidOrderDirection => 
            OrderDirection.Equals("asc", StringComparison.OrdinalIgnoreCase) ||
            OrderDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);
    }
}
