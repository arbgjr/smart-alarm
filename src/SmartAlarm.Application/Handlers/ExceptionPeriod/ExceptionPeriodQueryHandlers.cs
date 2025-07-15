using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.DTOs.ExceptionPeriod;
using SmartAlarm.Application.Queries.ExceptionPeriod;
using SmartAlarm.Domain.Repositories;

namespace SmartAlarm.Application.Handlers.ExceptionPeriod;

/// <summary>
/// Handler para consultas de períodos de exceção
/// </summary>
public class ExceptionPeriodQueryHandlers :
    IRequestHandler<GetExceptionPeriodByIdQuery, ExceptionPeriodDto?>,
    IRequestHandler<ListExceptionPeriodsQuery, IEnumerable<ExceptionPeriodDto>>,
    IRequestHandler<GetActiveExceptionPeriodsOnDateQuery, IEnumerable<ExceptionPeriodDto>>
{
    private readonly IExceptionPeriodRepository _repository;
    private readonly ILogger<ExceptionPeriodQueryHandlers> _logger;

    public ExceptionPeriodQueryHandlers(
        IExceptionPeriodRepository repository,
        ILogger<ExceptionPeriodQueryHandlers> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ExceptionPeriodDto?> Handle(GetExceptionPeriodByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Getting exception period {Id} for user {UserId}",
            request.Id, request.UserId);

        try
        {
            var period = await _repository.GetByIdAsync(request.Id);
            
            if (period == null)
            {
                _logger.LogWarning("Exception period {Id} not found", request.Id);
                return null;
            }

            if (period.UserId != request.UserId)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to access exception period {Id} owned by {OwnerId}",
                    request.UserId, request.Id, period.UserId);
                return null;
            }

            return ExceptionPeriodDto.FromEntity(period);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting exception period {Id} for user {UserId}",
                request.Id, request.UserId);
            throw;
        }
    }

    public async Task<IEnumerable<ExceptionPeriodDto>> Handle(ListExceptionPeriodsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Listing exception periods for user {UserId} (Type: {Type}, OnlyActive: {OnlyActive})",
            request.UserId, request.Type, request.OnlyActive);

        try
        {
            var periods = await _repository.GetByUserIdAsync(request.UserId);

            // Aplicar filtros
            if (request.Type.HasValue)
            {
                periods = periods.Where(p => p.Type == request.Type.Value);
            }

            if (request.OnlyActive)
            {
                periods = periods.Where(p => p.IsActive);
            }

            if (request.ActiveOnDate.HasValue)
            {
                var filterDate = request.ActiveOnDate.Value.Date;
                periods = periods.Where(p => p.IsActiveOnDate(filterDate));
            }

            var result = periods.Select(ExceptionPeriodDto.FromEntity).ToList();

            _logger.LogInformation(
                "Found {Count} exception periods for user {UserId}",
                result.Count, request.UserId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error listing exception periods for user {UserId}",
                request.UserId);
            throw;
        }
    }

    public async Task<IEnumerable<ExceptionPeriodDto>> Handle(GetActiveExceptionPeriodsOnDateQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Getting active exception periods for user {UserId} on {Date}",
            request.UserId, request.Date.Date);

        try
        {
            var periods = await _repository.GetActivePeriodsOnDateAsync(request.UserId, request.Date);
            var result = periods.Select(ExceptionPeriodDto.FromEntity).ToList();

            _logger.LogInformation(
                "Found {Count} active exception periods for user {UserId} on {Date}",
                result.Count, request.UserId, request.Date.Date);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting active exception periods for user {UserId} on {Date}",
                request.UserId, request.Date);
            throw;
        }
    }
}
