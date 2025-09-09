using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.ValueObjects;
using SmartAlarm.Application.Services;

namespace SmartAlarm.Infrastructure.Services;

/// <summary>
/// Implementação de parser para arquivos CSV
/// </summary>
public class CsvFileParser : IFileParser
{
    private readonly ILogger<CsvFileParser> _logger;
    private readonly string[] _supportedFormats = { ".csv" };

    public CsvFileParser(ILogger<CsvFileParser> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Alarm>> ParseAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("Nome do arquivo não pode ser vazio", nameof(fileName));

        if (!IsFormatSupported(fileName))
            throw new ArgumentException($"Formato do arquivo {fileName} não é suportado", nameof(fileName));

        _logger.LogInformation("Iniciando parsing do arquivo CSV: {FileName}", fileName);

        var alarms = new List<Alarm>();

        try
        {
            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, GetCsvConfiguration());

            var records = new List<AlarmCsvRecord>();
            await foreach (var record in csv.GetRecordsAsync<AlarmCsvRecord>(cancellationToken))
            {
                records.Add(record);
            }

            _logger.LogInformation("Lidos {Count} registros do arquivo CSV", records.Count);

            var validationErrors = new List<string>();
            var lineNumber = 2; // Começa em 2 por causa do cabeçalho

            foreach (var record in records)
            {
                try
                {
                    var alarm = ConvertToAlarm(record, lineNumber);
                    alarms.Add(alarm);
                }
                catch (Exception ex)
                {
                    var error = $"Erro na linha {lineNumber}: {ex.Message}";
                    validationErrors.Add(error);
                    _logger.LogWarning("Erro ao processar linha {LineNumber}: {Error}", lineNumber, ex.Message);
                }

                lineNumber++;
            }

            if (validationErrors.Any())
            {
                var allErrors = string.Join("; ", validationErrors);
                throw new InvalidOperationException($"Erros encontrados durante o parsing: {allErrors}");
            }

            _logger.LogInformation("Parsing concluído com sucesso. {Count} alarmes processados", alarms.Count);
            return alarms;
        }
        catch (Exception ex) when (!(ex is InvalidOperationException))
        {
            _logger.LogError(ex, "Erro durante o parsing do arquivo CSV: {FileName}", fileName);
            throw new InvalidOperationException($"Erro ao processar arquivo CSV: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public bool IsFormatSupported(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return _supportedFormats.Contains(extension);
    }

    /// <inheritdoc />
    public IEnumerable<string> GetSupportedFormats()
    {
        return _supportedFormats.AsEnumerable();
    }

    private static CsvConfiguration GetCsvConfiguration()
    {
        return new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            BadDataFound = null,
            MissingFieldFound = null,
            HeaderValidated = null
        };
    }

    private static Alarm ConvertToAlarm(AlarmCsvRecord record, int lineNumber)
    {
        // Validação de campos obrigatórios
        if (string.IsNullOrWhiteSpace(record.Name))
            throw new ArgumentException($"Nome é obrigatório (linha {lineNumber})");

        if (string.IsNullOrWhiteSpace(record.Time))
            throw new ArgumentException($"Horário é obrigatório (linha {lineNumber})");

        // Parse do horário
        if (!TimeOnly.TryParse(record.Time, out var time))
            throw new ArgumentException($"Horário inválido: {record.Time} (linha {lineNumber})");

        // Parse dos dias da semana
        var daysOfWeek = ParseDaysOfWeek(record.DaysOfWeek, lineNumber);

        // Parse do status (opcional, padrão é Active)
        var isActive = true;
        if (!string.IsNullOrWhiteSpace(record.IsActive))
        {
            if (!bool.TryParse(record.IsActive, out isActive))
                throw new ArgumentException($"Status inválido: {record.IsActive}. Use 'true' ou 'false' (linha {lineNumber})");
        }

        // Criar o alarme com DateTime baseado no TimeOnly (usando data de hoje)
        var alarmDateTime = DateTime.Today.Add(time.ToTimeSpan());
        var alarm = new Alarm(
            id: Guid.NewGuid(),
            name: new Name(record.Name.Trim()),
            time: alarmDateTime,
            enabled: isActive,
            userId: Guid.NewGuid() // Será redefinido pelo usuário que está importando
        );

        // Criar um schedule para o alarme com os dias da semana especificados
        var schedule = new Schedule(
            id: Guid.NewGuid(),
            time: time,
            recurrence: ScheduleRecurrence.Weekly, // Usamos Weekly para poder especificar dias
            daysOfWeek: daysOfWeek,
            alarmId: alarm.Id
        );

        alarm.AddSchedule(schedule);

        return alarm;
    }

    private static DaysOfWeek ParseDaysOfWeek(string daysOfWeekStr, int lineNumber)
    {
        if (string.IsNullOrWhiteSpace(daysOfWeekStr))
            throw new ArgumentException($"Dias da semana são obrigatórios (linha {lineNumber})");

        var result = DaysOfWeek.None;
        var days = daysOfWeekStr.Split(',', StringSplitOptions.RemoveEmptyEntries);

        foreach (var day in days)
        {
            var trimmedDay = day.Trim().ToLowerInvariant();
            
            switch (trimmedDay)
            {
                case "domingo" or "dom" or "sunday" or "sun":
                    result |= DaysOfWeek.Sunday;
                    break;
                case "segunda" or "seg" or "monday" or "mon":
                    result |= DaysOfWeek.Monday;
                    break;
                case "terça" or "ter" or "tuesday" or "tue":
                    result |= DaysOfWeek.Tuesday;
                    break;
                case "quarta" or "qua" or "wednesday" or "wed":
                    result |= DaysOfWeek.Wednesday;
                    break;
                case "quinta" or "qui" or "thursday" or "thu":
                    result |= DaysOfWeek.Thursday;
                    break;
                case "sexta" or "sex" or "friday" or "fri":
                    result |= DaysOfWeek.Friday;
                    break;
                case "sábado" or "sab" or "saturday" or "sat":
                    result |= DaysOfWeek.Saturday;
                    break;
                default:
                    throw new ArgumentException($"Dia da semana inválido: {day} (linha {lineNumber})");
            }
        }

        if (result == DaysOfWeek.None)
            throw new ArgumentException($"Pelo menos um dia da semana deve ser especificado (linha {lineNumber})");

        return result;
    }
}

/// <summary>
/// Modelo para representar um registro do arquivo CSV
/// </summary>
public class AlarmCsvRecord
{
    public string Name { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string DaysOfWeek { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IsActive { get; set; } = string.Empty;
}
