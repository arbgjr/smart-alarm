using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Commands.Holiday;
using SmartAlarm.Application.DTOs.Holiday;
using SmartAlarm.Domain.Abstractions;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Application.Handlers.Holiday
{
    /// <summary>
    /// Handler para criar um novo feriado.
    /// </summary>
    public class CreateHolidayHandler : IRequestHandler<CreateHolidayCommand, HolidayResponseDto>
    {
        private readonly IHolidayRepository _holidayRepository;
        private readonly ILogger<CreateHolidayHandler> _logger;

        public CreateHolidayHandler(
            IHolidayRepository holidayRepository,
            ILogger<CreateHolidayHandler> logger)
        {
            _holidayRepository = holidayRepository;
            _logger = logger;
        }

        public async Task<HolidayResponseDto> Handle(CreateHolidayCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating new holiday: {Description} on {Date}", request.Description, request.Date);

            var holiday = new Domain.Entities.Holiday(request.Date, request.Description);

            await _holidayRepository.AddAsync(holiday, cancellationToken);

            _logger.LogInformation("Holiday created successfully with ID: {HolidayId}", holiday.Id);

            return new HolidayResponseDto
            {
                Id = holiday.Id,
                Date = holiday.Date,
                Description = holiday.Description,
                CreatedAt = holiday.CreatedAt,
                IsRecurring = holiday.IsRecurring()
            };
        }
    }
}
