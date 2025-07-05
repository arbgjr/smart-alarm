using System;

namespace SmartAlarm.Application.DTOs
{
    /// <summary>
    /// DTO para criação de um novo alarme.
    /// </summary>
    public class CreateAlarmDto
    {
        public string? Name { get; set; }
        public DateTime? Time { get; set; }
        /// <summary>
        /// Preenchido automaticamente pelo backend com o usuário autenticado. Não envie este campo no payload.
        /// Não marque como 'required' para evitar erro de binding.
        /// </summary>
        public Guid UserId { get; set; }
    }
}
