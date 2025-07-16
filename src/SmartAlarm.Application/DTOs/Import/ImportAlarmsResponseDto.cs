namespace SmartAlarm.Application.DTOs.Import;

/// <summary>
/// DTO para resposta de importação de alarmes
/// </summary>
public class ImportAlarmsResponseDto
{
    /// <summary>
    /// Total de registros processados no arquivo
    /// </summary>
    public int TotalRecords { get; set; }

    /// <summary>
    /// Número de alarmes importados com sucesso
    /// </summary>
    public int SuccessfulImports { get; set; }

    /// <summary>
    /// Número de alarmes que falharam na importação
    /// </summary>
    public int FailedImports { get; set; }

    /// <summary>
    /// Número de alarmes que foram atualizados (sobrescritos)
    /// </summary>
    public int UpdatedImports { get; set; }

    /// <summary>
    /// Lista de erros encontrados durante a importação
    /// </summary>
    public List<ImportErrorDto> Errors { get; set; } = new();

    /// <summary>
    /// Lista de alarmes importados com sucesso
    /// </summary>
    public List<AlarmResponseDto> ImportedAlarms { get; set; } = new();

    /// <summary>
    /// Indica se a importação foi totalmente bem-sucedida
    /// </summary>
    public bool IsSuccess => FailedImports == 0;

    /// <summary>
    /// Mensagem resumo da importação
    /// </summary>
    public string Summary =>
        $"Importação concluída: {SuccessfulImports} sucessos, {UpdatedImports} atualizações, {FailedImports} falhas de {TotalRecords} registros.";
}
