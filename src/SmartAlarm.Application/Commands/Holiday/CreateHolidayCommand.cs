using System;
using MediatR;
using SmartAlarm.Application.DTOs.Holiday;

namespace SmartAlarm.Application.Commands.Holiday
{
    /// <summary>
    /// Command para criar um novo feriado.
    /// </summary>
    public class CreateHolidayCommand : IRequest<HolidayResponseDto>
    {
        /// <summary>
        /// Data do feriado.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Descrição do feriado.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        public CreateHolidayCommand() { }

        public CreateHolidayCommand(DateTime date, string description)
        {
            Date = date;
            Description = description;
        }
    }
}
