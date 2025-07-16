using FluentValidation;
using SmartAlarm.Application.Commands.Import;

namespace SmartAlarm.Application.Validators.Import;

/// <summary>
/// Validador para o comando de importação de alarmes
/// </summary>
public class ImportAlarmsCommandValidator : AbstractValidator<ImportAlarmsCommand>
{
    public ImportAlarmsCommandValidator()
    {
        RuleFor(x => x.FileStream)
            .NotNull()
            .WithMessage("Stream do arquivo é obrigatório");
            
        RuleFor(x => x.FileStream)
            .Must(stream => stream.CanRead)
            .WithMessage("Stream deve ser legível")
            .When(x => x.FileStream != null);
            
        RuleFor(x => x.FileStream)
            .Must(stream => stream.Length > 0)
            .WithMessage("Arquivo não pode estar vazio")
            .When(x => x.FileStream != null && x.FileStream.CanRead);

        RuleFor(x => x.FileName)
            .NotNull()
            .WithMessage("Nome do arquivo é obrigatório")
            .NotEmpty()
            .WithMessage("Nome do arquivo não pode estar vazio")
            .Must(fileName => !string.IsNullOrWhiteSpace(fileName))
            .WithMessage("Nome do arquivo deve ser válido")
            .Must(HasValidExtension)
            .WithMessage("Arquivo deve ter uma extensão válida (.csv)");

        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty)
            .WithMessage("ID do usuário é obrigatório");
    }

    private static bool HasValidExtension(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var validExtensions = new[] { ".csv" };
        
        return validExtensions.Contains(extension);
    }
}
