using System;
using System.ComponentModel.DataAnnotations;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Application.DTOs.UserHolidayPreference
{
    /// <summary>
    /// DTO para atualização de uma preferência de feriado do usuário.
    /// </summary>
    public class UpdateUserHolidayPreferenceDto
    {
        /// <summary>
        /// Indica se a preferência está ativa.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Ação a ser executada no feriado.
        /// </summary>
        [Required(ErrorMessage = "A ação é obrigatória.")]
        public HolidayPreferenceAction Action { get; set; }

        /// <summary>
        /// Atraso em minutos (obrigatório quando Action = Delay).
        /// </summary>
        [Range(1, 1440, ErrorMessage = "O atraso deve estar entre 1 e 1440 minutos (24 horas).")]
        public int? DelayInMinutes { get; set; }
    }
}
