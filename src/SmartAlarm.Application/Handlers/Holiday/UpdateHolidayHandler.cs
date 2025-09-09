using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Commands.Holiday;
using SmartAlarm.Application.DTOs.Holiday;
using SmartAlarm.Domain.Repositories;

namespace SmartAlarm.Application.Handlers.Holiday
{
    /// <summary>
    /// Handler para atualizar um feriado existente.
    /// </summary>
    public class UpdateHolidayHandler : IRequestHandler<UpdateHolidayCommand, HolidayResponseDto>
    {
        private readonly IHolidayRepository _holidayRepository;
        private readonly ILogger<UpdateHolidayHandler> _logger;

        public UpdateHolidayHandler(
            IHolidayRepository holidayRepository,
            ILogger<UpdateHolidayHandler> logger)
        {
            _holidayRepository = holidayRepository;
            _logger = logger;
        }

        public async Task<HolidayResponseDto> Handle(UpdateHolidayCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating holiday with ID: {HolidayId}", request.Id);

            var holiday = await _holidayRepository.GetByIdAsync(request.Id, cancellationToken);
            if (holiday == null)
            {
                _logger.LogWarning("Holiday with ID {HolidayId} not found", request.Id);
                throw new System.ArgumentException($"Holiday with ID {request.Id} not found");
            }

            // Criar novo feriado com os dados atualizados (imutabilidade)
            var updatedHoliday = new Domain.Entities.Holiday(request.Id, request.Date, request.Description);

            await _holidayRepository.UpdateAsync(updatedHoliday, cancellationToken);

            _logger.LogInformation("Holiday updated successfully: {HolidayId}", request.Id);

            return new HolidayResponseDto
            {
                Id = updatedHoliday.Id,
                Date = updatedHoliday.Date,
                Description = updatedHoliday.Description,
                CreatedAt = updatedHoliday.CreatedAt,
                IsRecurring = updatedHoliday.IsRecurring()
            };
        }
    }
}
