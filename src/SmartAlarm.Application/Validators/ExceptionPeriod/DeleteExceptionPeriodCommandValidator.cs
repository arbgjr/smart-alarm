using FluentValidation;
using SmartAlarm.Application.Commands.ExceptionPeriod;
using SmartAlarm.Domain.Repositories;

namespace SmartAlarm.Application.Validators.ExceptionPeriod;

/// <summary>
/// Validador para exclusão de período de exceção
/// </summary>
public class DeleteExceptionPeriodCommandValidator : AbstractValidator<DeleteExceptionPeriodCommand>
{
    private readonly IExceptionPeriodRepository _repository;

    public DeleteExceptionPeriodCommandValidator(IExceptionPeriodRepository repository)
    {
        _repository = repository;

        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("ID é obrigatório");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("ID do usuário é obrigatório");

        // Validação para verificar se o período existe e pertence ao usuário
        RuleFor(x => x)
            .MustAsync(async (command, cancellation) =>
            {
                var period = await _repository.GetByIdAsync(command.Id);
                return period != null && period.UserId == command.UserId;
            })
            .WithMessage("Período não encontrado ou não pertence ao usuário")
            .WithName("Id");
    }
}
