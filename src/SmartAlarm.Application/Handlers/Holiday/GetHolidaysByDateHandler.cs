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
    /// Handler para buscar feriados por data específica.
    /// </summary>
    public class GetHolidaysByDateHandler : IRequestHandler<GetHolidaysByDateQuery, IEnumerable<HolidayResponseDto>>
    {
        private readonly IHolidayRepository _holidayRepository;
        private readonly ILogger<GetHolidaysByDateHandler> _logger;

        public GetHolidaysByDateHandler(
            IHolidayRepository holidayRepository,
            ILogger<GetHolidaysByDateHandler> logger)
        {
            _holidayRepository = holidayRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<HolidayResponseDto>> Handle(GetHolidaysByDateQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting holidays for date: {Date}", request.Date);

            var holidays = await _holidayRepository.GetByDateAsync(DateOnly.FromDateTime(request.Date), cancellationToken);

            var result = holidays.Select(h => new HolidayResponseDto
            {
                Id = h.Id,
                Date = h.Date,
                Description = h.Description,
                CreatedAt = h.CreatedAt,
                IsRecurring = h.IsRecurring()
            }).ToList();

            _logger.LogInformation("Found {Count} holidays for date {Date}", result.Count, request.Date);

            return result;
        }
    }
}
