using System;
using System.ComponentModel.DataAnnotations;

namespace SmartAlarm.Application.DTOs.Holiday
{
    /// <summary>
    /// DTO para atualização de um feriado.
    /// </summary>
    public class UpdateHolidayDto
    {
        /// <summary>
        /// Nova data do feriado.
        /// </summary>
        [Required(ErrorMessage = "Data é obrigatória")]
        public DateTime Date { get; set; }

        /// <summary>
        /// Nova descrição do feriado.
        /// </summary>
        [Required(ErrorMessage = "Descrição é obrigatória")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Descrição deve ter entre 2 e 100 caracteres")]
        public string Description { get; set; } = string.Empty;
    }
}
