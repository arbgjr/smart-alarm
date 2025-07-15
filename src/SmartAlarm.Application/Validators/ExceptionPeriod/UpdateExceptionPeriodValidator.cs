using FluentValidation;
using SmartAlarm.Application.Commands.ExceptionPeriod;
using SmartAlarm.Domain.Repositories;

namespace SmartAlarm.Application.Validators.ExceptionPeriod;

public class UpdateExceptionPeriodValidator : AbstractValidator<UpdateExceptionPeriodCommand>
{
    private readonly IExceptionPeriodRepository _repository;

    public UpdateExceptionPeriodValidator(IExceptionPeriodRepository repository)
    {
        _repository = repository;

        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("ID é obrigatório");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Nome é obrigatório")
            .MaximumLength(200)
            .WithMessage("Nome deve ter no máximo 200 caracteres");

        RuleFor(x => x.StartDate)
            .GreaterThanOrEqualTo(DateTime.Today)
            .WithMessage("Data de início deve ser hoje ou no futuro");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("Data de fim deve ser maior ou igual à data de início");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("ID do usuário é obrigatório");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Descrição deve ter no máximo 1000 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Tipo do período de exceção inválido");
    }
}
