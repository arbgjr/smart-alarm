using System;
using System.ComponentModel.DataAnnotations;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Application.DTOs.UserHolidayPreference
{
    /// <summary>
    /// DTO para criação de uma preferência de feriado do usuário.
    /// </summary>
    public class CreateUserHolidayPreferenceDto
    {
        /// <summary>
        /// ID do usuário.
        /// </summary>
        [Required(ErrorMessage = "O ID do usuário é obrigatório.")]
        public Guid UserId { get; set; }

        /// <summary>
        /// ID do feriado.
        /// </summary>
        [Required(ErrorMessage = "O ID do feriado é obrigatório.")]
        public Guid HolidayId { get; set; }

        /// <summary>
        /// Indica se a preferência está ativa.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

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
