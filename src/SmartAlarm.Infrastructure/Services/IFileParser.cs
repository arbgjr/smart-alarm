using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Infrastructure.Services;

/// <summary>
/// Interface para parsing de arquivos de importação de alarmes
/// </summary>
public interface IFileParser
{
    /// <summary>
    /// Faz o parsing de um arquivo e retorna uma lista de alarmes
    /// </summary>
    /// <param name="stream">Stream do arquivo a ser processado</param>
    /// <param name="fileName">Nome do arquivo para validação de formato</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Lista de alarmes extraídos do arquivo</returns>
    Task<IEnumerable<Alarm>> ParseAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Valida se o formato do arquivo é suportado
    /// </summary>
    /// <param name="fileName">Nome do arquivo</param>
    /// <returns>True se o formato é suportado</returns>
    bool IsFormatSupported(string fileName);

    /// <summary>
    /// Obtém os formatos de arquivo suportados
    /// </summary>
    /// <returns>Lista de extensões suportadas</returns>
    IEnumerable<string> GetSupportedFormats();
}
