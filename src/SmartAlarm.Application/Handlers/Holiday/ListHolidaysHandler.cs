using System.Collections.Generic;
using System.Linq;
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
    /// Handler para listar todos os feriados.
    /// </summary>
    public class ListHolidaysHandler : IRequestHandler<ListHolidaysQuery, IEnumerable<HolidayResponseDto>>
    {
        private readonly IHolidayRepository _holidayRepository;
        private readonly ILogger<ListHolidaysHandler> _logger;

        public ListHolidaysHandler(
            IHolidayRepository holidayRepository,
            ILogger<ListHolidaysHandler> logger)
        {
            _holidayRepository = holidayRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<HolidayResponseDto>> Handle(ListHolidaysQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Listing all holidays");

            var holidays = await _holidayRepository.GetAllAsync(cancellationToken);

            var result = holidays.Select(h => new HolidayResponseDto
            {
                Id = h.Id,
                Date = h.Date,
                Description = h.Description,
                CreatedAt = h.CreatedAt,
                IsRecurring = h.IsRecurring()
            }).ToList();

            _logger.LogInformation("Found {Count} holidays", result.Count);

            return result;
        }
    }
}
