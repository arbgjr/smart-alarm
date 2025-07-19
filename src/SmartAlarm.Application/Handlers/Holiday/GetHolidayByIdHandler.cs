using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.DTOs.Holiday;
using SmartAlarm.Application.Queries.Holiday;
using SmartAlarm.Domain.Abstractions;

namespace SmartAlarm.Application.Handlers.Holiday
{
    /// <summary>
    /// Handler para buscar um feriado por ID.
    /// </summary>
    public class GetHolidayByIdHandler : IRequestHandler<GetHolidayByIdQuery, HolidayResponseDto?>
    {
        private readonly IHolidayRepository _holidayRepository;
        private readonly ILogger<GetHolidayByIdHandler> _logger;

        public GetHolidayByIdHandler(
            IHolidayRepository holidayRepository,
            ILogger<GetHolidayByIdHandler> logger)
        {
            _holidayRepository = holidayRepository;
            _logger = logger;
        }

        public async Task<HolidayResponseDto?> Handle(GetHolidayByIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting holiday by ID: {HolidayId}", request.Id);

            var holiday = await _holidayRepository.GetByIdAsync(request.Id, cancellationToken);
            if (holiday == null)
            {
                _logger.LogInformation("Holiday with ID {HolidayId} not found", request.Id);
                return null;
            }

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
