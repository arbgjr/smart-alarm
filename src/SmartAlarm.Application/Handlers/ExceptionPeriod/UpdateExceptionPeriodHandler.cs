using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Commands.ExceptionPeriod;
using SmartAlarm.Application.DTOs.ExceptionPeriod;
using SmartAlarm.Domain.Repositories;

namespace SmartAlarm.Application.Handlers.ExceptionPeriod;

/// <summary>
/// Handler para atualização de períodos de exceção
/// </summary>
public class UpdateExceptionPeriodHandler : IRequestHandler<UpdateExceptionPeriodCommand, ExceptionPeriodDto>
{
    private readonly IExceptionPeriodRepository _repository;
    private readonly ILogger<UpdateExceptionPeriodHandler> _logger;

    public UpdateExceptionPeriodHandler(
        IExceptionPeriodRepository repository,
        ILogger<UpdateExceptionPeriodHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ExceptionPeriodDto> Handle(UpdateExceptionPeriodCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Updating exception period {Id} for user {UserId}: {Name}",
            request.Id, request.UserId, request.Name);

        try
        {
            // Buscar período existente
            var existingPeriod = await _repository.GetByIdAsync(request.Id);
            if (existingPeriod == null)
            {
                _logger.LogWarning("Exception period {Id} not found", request.Id);
                throw new KeyNotFoundException($"Período de exceção {request.Id} não encontrado");
            }

            if (existingPeriod.UserId != request.UserId)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to update exception period {Id} owned by {OwnerId}",
                    request.UserId, request.Id, existingPeriod.UserId);
                throw new UnauthorizedAccessException("Usuário não autorizado a atualizar este período");
            }

            // Atualizar propriedades
            existingPeriod.UpdateName(request.Name);
            existingPeriod.UpdatePeriod(request.StartDate, request.EndDate);
            existingPeriod.UpdateType(request.Type);
            existingPeriod.UpdateDescription(request.Description);

            if (!request.IsActive && existingPeriod.IsActive)
            {
                existingPeriod.Deactivate();
            }
            else if (request.IsActive && !existingPeriod.IsActive)
            {
                existingPeriod.Activate();
            }

            // Salvar alterações
            await _repository.UpdateAsync(existingPeriod);

            _logger.LogInformation(
                "Exception period updated successfully: {Id} for user {UserId}",
                request.Id, request.UserId);

            // Retornar DTO
            return ExceptionPeriodDto.FromEntity(existingPeriod);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not UnauthorizedAccessException)
        {
            _logger.LogError(ex,
                "Error updating exception period {Id} for user {UserId}",
                request.Id, request.UserId);
            throw;
        }
    }
}
