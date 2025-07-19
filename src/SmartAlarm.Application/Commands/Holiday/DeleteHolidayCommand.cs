using System;
using MediatR;

namespace SmartAlarm.Application.Commands.Holiday
{
    /// <summary>
    /// Command para deletar um feriado.
    /// </summary>
    public class DeleteHolidayCommand : IRequest<bool>
    {
        /// <summary>
        /// ID do feriado a ser deletado.
        /// </summary>
        public Guid Id { get; set; }

        public DeleteHolidayCommand() { }

        public DeleteHolidayCommand(Guid id)
        {
            Id = id;
        }
    }
}
