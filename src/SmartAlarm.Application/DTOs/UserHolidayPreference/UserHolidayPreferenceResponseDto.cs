using System;
using SmartAlarm.Application.DTOs.Holiday;
using SmartAlarm.Application.DTOs.User;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Application.DTOs.UserHolidayPreference
{
    /// <summary>
    /// DTO para resposta de uma preferência de feriado do usuário.
    /// </summary>
    public class UserHolidayPreferenceResponseDto
    {
        /// <summary>
        /// ID único da preferência.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// ID do usuário.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// ID do feriado.
        /// </summary>
        public Guid HolidayId { get; set; }

        /// <summary>
        /// Indica se a preferência está ativa.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Ação a ser executada no feriado.
        /// </summary>
        public HolidayPreferenceAction Action { get; set; }

        /// <summary>
        /// Atraso em minutos (quando Action = Delay).
        /// </summary>
        public int? DelayInMinutes { get; set; }

        /// <summary>
        /// Data de criação da preferência.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Data da última atualização.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Dados do usuário relacionado.
        /// </summary>
        public UserResponseDto? User { get; set; }

        /// <summary>
        /// Dados do feriado relacionado.
        /// </summary>
        public HolidayResponseDto? Holiday { get; set; }

        /// <summary>
        /// Nome da ação para exibição.
        /// </summary>
        public string ActionDisplayName => Action switch
        {
            HolidayPreferenceAction.Disable => "Desabilitar",
            HolidayPreferenceAction.Delay => $"Atrasar {DelayInMinutes} minutos",
            HolidayPreferenceAction.Skip => "Pular",
            _ => "Desconhecido"
        };
    }
}
