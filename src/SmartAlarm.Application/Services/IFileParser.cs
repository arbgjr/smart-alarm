using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Application.Services;

/// <summary>
/// Interface para parsing de arquivos de alarmes
/// </summary>
public interface IFileParser
{
    /// <summary>
    /// Verifica se o formato do arquivo é suportado
    /// </summary>
    bool IsFormatSupported(string fileName);

    /// <summary>
    /// Obtém os formatos suportados
    /// </summary>
    IEnumerable<string> GetSupportedFormats();

    /// <summary>
    /// Faz o parsing do arquivo e retorna os alarmes
    /// </summary>
    Task<IEnumerable<Alarm>> ParseAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);
}