using MediatR;
using SmartAlarm.Application.DTOs.Import;

namespace SmartAlarm.Application.Commands.Import;

/// <summary>
/// Comando para importar alarmes a partir de um arquivo
/// </summary>
public class ImportAlarmsCommand : IRequest<ImportAlarmsResponseDto>
{
    /// <summary>
    /// Stream do arquivo a ser importado
    /// </summary>
    public Stream FileStream { get; }

    /// <summary>
    /// Nome do arquivo para validação
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// ID do usuário que está importando os alarmes
    /// </summary>
    public Guid UserId { get; }

    /// <summary>
    /// Se deve sobrescrever alarmes existentes com o mesmo nome
    /// </summary>
    public bool OverwriteExisting { get; }

    public ImportAlarmsCommand(Stream fileStream, string fileName, Guid userId, bool overwriteExisting = false)
    {
        FileStream = fileStream;
        FileName = fileName;
        UserId = userId;
        OverwriteExisting = overwriteExisting;
    }
}
