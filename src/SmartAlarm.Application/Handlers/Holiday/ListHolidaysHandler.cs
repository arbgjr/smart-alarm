using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.DTOs.Holiday;
using SmartAlarm.Application.DTOs.Common;
using SmartAlarm.Application.Queries.Holiday;
using SmartAlarm.Domain.Repositories;
using System;
using System.Diagnostics;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Logging;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;

namespace SmartAlarm.Application.Handlers.Holiday
{
    /// <summary>
    /// Handler para listar todos os feriados com paginação.
    /// </summary>
    public class ListHolidaysHandler : IRequestHandler<ListHolidaysQuery, PaginatedResponseDto<HolidayResponseDto>>
    {
        private readonly IHolidayRepository _holidayRepository;
        private readonly ILogger<ListHolidaysHandler> _logger;
        private readonly SmartAlarmActivitySource _activitySource;
        private readonly SmartAlarmMeter _meter;
        private readonly ICorrelationContext _correlationContext;

        public ListHolidaysHandler(
            IHolidayRepository holidayRepository,
            ILogger<ListHolidaysHandler> logger,
            SmartAlarmActivitySource activitySource,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext)
        {
            _holidayRepository = holidayRepository;
            _logger = logger;
            _activitySource = activitySource;
            _meter = meter;
            _correlationContext = correlationContext;
        }

        public async Task<PaginatedResponseDto<HolidayResponseDto>> Handle(ListHolidaysQuery request, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = _correlationContext.CorrelationId;

            _logger.LogInformation("Listing holidays with pagination - Page: {Page}, PageSize: {PageSize}, StartDate: {StartDate}, EndDate: {EndDate}, DescriptionFilter: {DescriptionFilter}, IsRecurring: {IsRecurring} - CorrelationId: {CorrelationId}",
                request.Pagination.Page, request.Pagination.PageSize, request.StartDate, request.EndDate, request.DescriptionFilter, request.IsRecurring, correlationId);

            using var activity = _activitySource.StartActivity("ListHolidaysHandler.Handle");
            activity?.SetTag("correlation.id", correlationId);
            activity?.SetTag("operation", "ListHolidays");
            activity?.SetTag("handler", "ListHolidaysHandler");
            activity?.SetTag("pagination.page", request.Pagination.Page.ToString());
            activity?.SetTag("pagination.pageSize", request.Pagination.PageSize.ToString());
            activity?.SetTag("filter.startDate", request.StartDate?.ToString("yyyy-MM-dd") ?? "null");
            activity?.SetTag("filter.endDate", request.EndDate?.ToString("yyyy-MM-dd") ?? "null");
            activity?.SetTag("filter.description", request.DescriptionFilter ?? "null");
            activity?.SetTag("filter.isRecurring", request.IsRecurring?.ToString() ?? "null");

            try
            {
                // Validar parâmetros de paginação
                if (!request.Pagination.IsValidOrderDirection)
                {
                    throw new ArgumentException($"Invalid order direction: {request.Pagination.OrderDirection}");
                }

                // Obter todos os feriados (implementaremos paginação no repositório depois)
                var holidays = await _holidayRepository.GetAllAsync(cancellationToken);

                // Aplicar filtros
                var filteredHolidays = holidays.AsEnumerable();

                if (request.StartDate.HasValue)
                {
                    filteredHolidays = filteredHolidays.Where(h => h.Date.Date >= request.StartDate.Value.Date);
                }

                if (request.EndDate.HasValue)
                {
                    filteredHolidays = filteredHolidays.Where(h => h.Date.Date <= request.EndDate.Value.Date);
                }

                if (!string.IsNullOrWhiteSpace(request.DescriptionFilter))
                {
                    filteredHolidays = filteredHolidays.Where(h =>
                        h.Description.Contains(request.DescriptionFilter, StringComparison.OrdinalIgnoreCase));
                }

                if (request.IsRecurring.HasValue)
                {
                    filteredHolidays = filteredHolidays.Where(h => h.IsRecurring() == request.IsRecurring.Value);
                }

                // Aplicar ordenação
                var orderedHolidays = request.Pagination.OrderBy switch
                {
                    "date" => request.Pagination.OrderDirection.Equals("desc", StringComparison.OrdinalIgnoreCase) ?
                        filteredHolidays.OrderByDescending(h => h.Date) :
                        filteredHolidays.OrderBy(h => h.Date),
                    "description" => request.Pagination.OrderDirection.Equals("desc", StringComparison.OrdinalIgnoreCase) ?
                        filteredHolidays.OrderByDescending(h => h.Description) :
                        filteredHolidays.OrderBy(h => h.Description),
                    _ => filteredHolidays.OrderBy(h => h.Date) // ordenação padrão por data
                };

                var holidaysList = orderedHolidays.ToList();
                var totalCount = holidaysList.Count;

                // Aplicar paginação
                var pagedHolidays = holidaysList
                    .Skip(request.Pagination.Skip)
                    .Take(request.Pagination.PageSize)
                    .Select(h => new HolidayResponseDto
                    {
                        Id = h.Id,
                        Date = h.Date,
                        Description = h.Description,
                        CreatedAt = h.CreatedAt,
                        IsRecurring = h.IsRecurring()
                    })
                    .ToList();

                stopwatch.Stop();
                activity?.SetTag("holidays.total_count", totalCount.ToString());
                activity?.SetTag("holidays.returned_count", pagedHolidays.Count.ToString());
                activity?.SetTag("holidays.recurring", pagedHolidays.Count(h => h.IsRecurring).ToString());
                activity?.SetStatus(ActivityStatusCode.Ok);

                // Métricas
                _meter.RecordDatabaseQueryDuration(stopwatch.ElapsedMilliseconds, "ListHolidays", "Holidays");

                _logger.LogInformation("Found {TotalCount} holidays, returning {ReturnedCount} for page {Page} - CorrelationId: {CorrelationId}",
                    totalCount, pagedHolidays.Count, request.Pagination.Page, correlationId);

                return new PaginatedResponseDto<HolidayResponseDto>(
                    items: pagedHolidays,
                    totalCount: totalCount,
                    page: request.Pagination.Page,
                    pageSize: request.Pagination.PageSize,
                    orderBy: request.Pagination.OrderBy,
                    orderDirection: request.Pagination.OrderDirection
                );
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _meter.IncrementErrorCount("QUERY", "Holidays", "ListError");

                _logger.LogError(ex, "Error listing holidays - CorrelationId: {CorrelationId}", correlationId);

                throw;
            }
        }
    }
}
