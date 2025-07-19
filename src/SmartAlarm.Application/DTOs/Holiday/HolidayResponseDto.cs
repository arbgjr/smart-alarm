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
        /// Descrição do feriado.
        /// </summary>
        public string Description { get; set; } = string.Empty;

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
