using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Commands.ExceptionPeriod;
using SmartAlarm.Domain.Repositories;

namespace SmartAlarm.Application.Handlers.ExceptionPeriod;

/// <summary>
/// Handler para exclusão de períodos de exceção
/// </summary>
public class DeleteExceptionPeriodHandler : IRequestHandler<DeleteExceptionPeriodCommand, bool>
{
    private readonly IExceptionPeriodRepository _repository;
    private readonly ILogger<DeleteExceptionPeriodHandler> _logger;

    public DeleteExceptionPeriodHandler(
        IExceptionPeriodRepository repository,
        ILogger<DeleteExceptionPeriodHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteExceptionPeriodCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Deleting exception period {Id} for user {UserId}",
            request.Id, request.UserId);

        try
        {
            // Buscar período existente
            var existingPeriod = await _repository.GetByIdAsync(request.Id);
            if (existingPeriod == null)
            {
                _logger.LogWarning("Exception period {Id} not found", request.Id);
                return false;
            }

            if (existingPeriod.UserId != request.UserId)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to delete exception period {Id} owned by {OwnerId}",
                    request.UserId, request.Id, existingPeriod.UserId);
                throw new UnauthorizedAccessException("Usuário não autorizado a excluir este período");
            }

            // Excluir período
            await _repository.DeleteAsync(request.Id);

            _logger.LogInformation(
                "Exception period deleted successfully: {Id} for user {UserId}",
                request.Id, request.UserId);

            return true;
        }
        catch (Exception ex) when (ex is not UnauthorizedAccessException)
        {
            _logger.LogError(ex,
                "Error deleting exception period {Id} for user {UserId}",
                request.Id, request.UserId);
            throw;
        }
    }
}
