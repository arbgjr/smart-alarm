using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Commands.ExceptionPeriod;
using SmartAlarm.Application.DTOs.ExceptionPeriod;
using SmartAlarm.Domain.Repositories;

namespace SmartAlarm.Application.Handlers.ExceptionPeriod;

/// <summary>
/// Handler para criação de períodos de exceção
/// </summary>
public class CreateExceptionPeriodHandler : IRequestHandler<CreateExceptionPeriodCommand, ExceptionPeriodDto>
{
    private readonly IExceptionPeriodRepository _repository;
    private readonly ILogger<CreateExceptionPeriodHandler> _logger;

    public CreateExceptionPeriodHandler(
        IExceptionPeriodRepository repository,
        ILogger<CreateExceptionPeriodHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ExceptionPeriodDto> Handle(CreateExceptionPeriodCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Creating exception period for user {UserId}: {Name} from {StartDate} to {EndDate}",
            request.UserId, request.Name, request.StartDate, request.EndDate);

        try
        {
            // Criar entidade de domínio
            var exceptionPeriod = new Domain.Entities.ExceptionPeriod(
                Guid.NewGuid(),
                request.Name,
                request.StartDate,
                request.EndDate,
                request.Type,
                request.UserId,
                request.Description ?? string.Empty);

            // Salvar no repositório
            await _repository.AddAsync(exceptionPeriod);

            _logger.LogInformation(
                "Exception period created successfully: {Id} for user {UserId}",
                exceptionPeriod.Id, request.UserId);

            // Retornar DTO
            return ExceptionPeriodDto.FromEntity(exceptionPeriod);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error creating exception period for user {UserId}: {Name}",
                request.UserId, request.Name);
            throw;
        }
    }
}
