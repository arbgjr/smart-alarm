namespace SmartAlarm.Application.DTOs.Import;

/// <summary>
/// DTO para representar um erro durante a importação
/// </summary>
public class ImportErrorDto
{
    /// <summary>
    /// Número da linha onde ocorreu o erro (opcional)
    /// </summary>
    public int? LineNumber { get; set; }

    /// <summary>
    /// Nome do campo que causou o erro (opcional)
    /// </summary>
    public string? FieldName { get; set; }

    /// <summary>
    /// Valor que causou o erro (opcional)
    /// </summary>
    public string? FieldValue { get; set; }

    /// <summary>
    /// Mensagem de erro detalhada
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de erro (Validation, Parsing, Business, etc.)
    /// </summary>
    public string ErrorType { get; set; } = "General";

    /// <summary>
    /// Código de erro para identificação programática
    /// </summary>
    public string? ErrorCode { get; set; }
}
