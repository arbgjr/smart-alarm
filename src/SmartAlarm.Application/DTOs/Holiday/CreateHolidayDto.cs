using System;
using System.ComponentModel.DataAnnotations;

namespace SmartAlarm.Application.DTOs.Holiday
{
    /// <summary>
    /// DTO para criação de um novo feriado.
    /// </summary>
    public class CreateHolidayDto
    {
        /// <summary>
        /// Data do feriado. Para feriados recorrentes, use o ano 0001.
        /// </summary>
        [Required(ErrorMessage = "Data é obrigatória")]
        public DateTime Date { get; set; }

        /// <summary>
        /// Descrição do feriado (ex: "Natal", "Ano Novo").
        /// </summary>
        [Required(ErrorMessage = "Descrição é obrigatória")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Descrição deve ter entre 2 e 200 caracteres")]
        public string Description { get; set; } = string.Empty;
    }
}
