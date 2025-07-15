using MediatR;

namespace SmartAlarm.Application.Commands.ExceptionPeriod;

/// <summary>
/// Command para deletar um período de exceção
/// </summary>
public class DeleteExceptionPeriodCommand : IRequest<bool>
{
    /// <summary>
    /// ID do período de exceção a ser deletado
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID do usuário solicitante (para validação de propriedade)
    /// </summary>
    public Guid UserId { get; set; }

    public DeleteExceptionPeriodCommand(Guid id, Guid userId)
    {
        Id = id;
        UserId = userId;
    }
}
