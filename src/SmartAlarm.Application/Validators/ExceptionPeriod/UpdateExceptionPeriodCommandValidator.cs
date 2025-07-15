using FluentValidation;
using SmartAlarm.Application.Commands.ExceptionPeriod;
using SmartAlarm.Domain.Repositories;

namespace SmartAlarm.Application.Validators.ExceptionPeriod;

/// <summary>
/// Validador para atualização de período de exceção
/// </summary>
public class UpdateExceptionPeriodCommandValidator : AbstractValidator<UpdateExceptionPeriodCommand>
{
    private readonly IExceptionPeriodRepository _repository;

    public UpdateExceptionPeriodCommandValidator(IExceptionPeriodRepository repository)
    {
        _repository = repository;

        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("ID é obrigatório");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Nome é obrigatório")
            .Length(1, 100)
            .WithMessage("Nome deve ter entre 1 e 100 caracteres");

        RuleFor(x => x.StartDate)
            .NotEmpty()
            .WithMessage("Data de início é obrigatória")
            .GreaterThanOrEqualTo(DateTime.Today.AddDays(-30))
            .WithMessage("Data de início não pode ser muito antiga (máximo 30 dias no passado)");

        RuleFor(x => x.EndDate)
            .NotEmpty()
            .WithMessage("Data de fim é obrigatória");

        RuleFor(x => x)
            .Must(x => x.EndDate >= x.StartDate)
            .WithMessage("Data de fim deve ser posterior ou igual à data de início")
            .WithName("EndDate");

        RuleFor(x => x)
            .Must(x => (x.EndDate - x.StartDate).TotalDays <= 365)
            .WithMessage("Período não pode exceder 365 dias")
            .WithName("EndDate");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Tipo de período inválido");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("ID do usuário é obrigatório");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Descrição não pode exceder 500 caracteres");

        // Validação async para overlap (excluindo o próprio período)
        RuleFor(x => x)
            .MustAsync(async (command, cancellation) => 
                await NotHaveOverlappingPeriods(command.UserId, command.StartDate, command.EndDate, command.Id))
            .WithMessage("Já existe um período ativo que sobrepõe às datas informadas")
            .WithName("DateRange");

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

    private async Task<bool> NotHaveOverlappingPeriods(Guid userId, DateTime startDate, DateTime endDate, Guid excludeId)
    {
        var overlappingPeriods = await _repository.GetOverlappingPeriodsAsync(userId, startDate, endDate);
        
        // Exclui o próprio período da verificação de overlap
        overlappingPeriods = overlappingPeriods.Where(p => p.Id != excludeId);

        return !overlappingPeriods.Any();
    }
}
