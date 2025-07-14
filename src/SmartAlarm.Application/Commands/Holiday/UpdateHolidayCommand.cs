using System;
using MediatR;
using SmartAlarm.Application.DTOs.Holiday;

namespace SmartAlarm.Application.Commands.Holiday
{
    /// <summary>
    /// Command para atualizar um feriado existente.
    /// </summary>
    public class UpdateHolidayCommand : IRequest<HolidayResponseDto>
    {
        /// <summary>
        /// ID do feriado a ser atualizado.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Nova data do feriado.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Nova descrição do feriado.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        public UpdateHolidayCommand() { }

        public UpdateHolidayCommand(Guid id, DateTime date, string description)
        {
            Id = id;
            Date = date;
            Description = description;
        }
    }
}
