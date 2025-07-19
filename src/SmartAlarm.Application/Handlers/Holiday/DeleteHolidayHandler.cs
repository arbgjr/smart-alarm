using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Commands.Holiday;
using SmartAlarm.Domain.Abstractions;

namespace SmartAlarm.Application.Handlers.Holiday
{
    /// <summary>
    /// Handler para deletar um feriado.
    /// </summary>
    public class DeleteHolidayHandler : IRequestHandler<DeleteHolidayCommand, bool>
    {
        private readonly IHolidayRepository _holidayRepository;
        private readonly ILogger<DeleteHolidayHandler> _logger;

        public DeleteHolidayHandler(
            IHolidayRepository holidayRepository,
            ILogger<DeleteHolidayHandler> logger)
        {
            _holidayRepository = holidayRepository;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteHolidayCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting holiday with ID: {HolidayId}", request.Id);

            var holiday = await _holidayRepository.GetByIdAsync(request.Id, cancellationToken);
            if (holiday == null)
            {
                _logger.LogWarning("Holiday with ID {HolidayId} not found", request.Id);
                return false;
            }

            await _holidayRepository.DeleteAsync(request.Id, cancellationToken);

            _logger.LogInformation("Holiday deleted successfully: {HolidayId}", request.Id);

            return true;
        }
    }
}
