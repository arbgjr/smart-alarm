using System;

namespace SmartAlarm.Application.DTOs.Holiday
{
    /// <summary>
    /// DTO para resposta de um feriado.
    /// </summary>
    public class HolidayResponseDto
    {
        /// <summary>
        /// ID único do feriado.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Data do feriado.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Nome do feriado.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Descrição do feriado.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Tipo do feriado.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// País do feriado.
        /// </summary>
        public string Country { get; set; } = string.Empty;

        /// <summary>
        /// Estado do feriado (opcional).
        /// </summary>
        public string? State { get; set; }

        /// <summary>
        /// Data de criação do feriado.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Indica se é um feriado recorrente (anual).
        /// </summary>
        public bool IsRecurring { get; set; }
    }
}
