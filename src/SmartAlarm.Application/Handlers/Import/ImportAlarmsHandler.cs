using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Commands.Import;
using SmartAlarm.Application.DTOs;
using SmartAlarm.Application.DTOs.Import;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Application.Services;

namespace SmartAlarm.Application.Handlers.Import;

/// <summary>
/// Handler para importação de alarmes a partir de arquivos
/// </summary>
public class ImportAlarmsHandler : IRequestHandler<ImportAlarmsCommand, ImportAlarmsResponseDto>
{
    private readonly IFileParser _fileParser;
    private readonly IAlarmRepository _alarmRepository;
    private readonly ILogger<ImportAlarmsHandler> _logger;

    public ImportAlarmsHandler(
        IFileParser fileParser,
        IAlarmRepository alarmRepository,
        ILogger<ImportAlarmsHandler> logger)
    {
        _fileParser = fileParser ?? throw new ArgumentNullException(nameof(fileParser));
        _alarmRepository = alarmRepository ?? throw new ArgumentNullException(nameof(alarmRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ImportAlarmsResponseDto> Handle(ImportAlarmsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando importação de alarmes. Usuário: {UserId}, Arquivo: {FileName}", 
            request.UserId, request.FileName);

        var response = new ImportAlarmsResponseDto();

        try
        {
            // Validar formato do arquivo
            if (!_fileParser.IsFormatSupported(request.FileName))
            {
                var supportedFormats = string.Join(", ", _fileParser.GetSupportedFormats());
                response.Errors.Add(new ImportErrorDto
                {
                    ErrorMessage = $"Formato de arquivo não suportado. Formatos aceitos: {supportedFormats}",
                    ErrorType = "Validation",
                    ErrorCode = "UNSUPPORTED_FORMAT"
                });
                
                _logger.LogWarning("Formato de arquivo não suportado: {FileName}", request.FileName);
                return response;
            }

            // Fazer parsing do arquivo
            IEnumerable<Alarm> alarmsFromFile;
            try
            {
                alarmsFromFile = await _fileParser.ParseAsync(request.FileStream, request.FileName, cancellationToken);
            }
            catch (Exception ex)
            {
                response.Errors.Add(new ImportErrorDto
                {
                    ErrorMessage = $"Erro ao processar arquivo: {ex.Message}",
                    ErrorType = "Parsing",
                    ErrorCode = "PARSING_ERROR"
                });
                
                _logger.LogError(ex, "Erro ao fazer parsing do arquivo: {FileName}", request.FileName);
                return response;
            }

            var alarmsList = alarmsFromFile.ToList();
            response.TotalRecords = alarmsList.Count;

            _logger.LogInformation("Arquivo processado com sucesso. {Count} alarmes encontrados", alarmsList.Count);

            // Obter alarmes existentes do usuário para verificar duplicatas
            var existingAlarms = await _alarmRepository.GetByUserIdAsync(request.UserId);
            var existingAlarmsByName = existingAlarms.ToDictionary(a => a.Name.Value, a => a);

            // Processar cada alarme
            foreach (var (alarm, index) in alarmsList.Select((a, i) => (a, i)))
            {
                try
                {
                    // Atualizar o UserId para o usuário que está importando
                    var alarmForUser = new Alarm(
                        Guid.NewGuid(),
                        alarm.Name,
                        alarm.Time,
                        alarm.Enabled,
                        request.UserId
                    );

                    // Copiar schedules do alarme original
                    foreach (var schedule in alarm.Schedules)
                    {
                        var newSchedule = new Schedule(
                            Guid.NewGuid(),
                            schedule.Time,
                            schedule.Recurrence,
                            schedule.DaysOfWeek,
                            alarmForUser.Id
                        );
                        alarmForUser.AddSchedule(newSchedule);
                    }

                    // Verificar se já existe um alarme com o mesmo nome
                    if (existingAlarmsByName.TryGetValue(alarm.Name.Value, out var existingAlarm))
                    {
                        if (request.OverwriteExisting)
                        {
                            // Atualizar o alarme existente
                            existingAlarm.UpdateName(alarm.Name);
                            existingAlarm.UpdateTime(alarm.Time);
                            if (alarm.Enabled)
                                existingAlarm.Enable();
                            else
                                existingAlarm.Disable();

                            await _alarmRepository.UpdateAsync(existingAlarm);
                            response.UpdatedImports++;
                            
                            response.ImportedAlarms.Add(new AlarmResponseDto(existingAlarm));
                            
                            _logger.LogDebug("Alarme atualizado: {AlarmName}", alarm.Name.Value);
                        }
                        else
                        {
                            response.Errors.Add(new ImportErrorDto
                            {
                                LineNumber = index + 2, // +2 porque começamos em 1 e temos cabeçalho
                                ErrorMessage = $"Alarme '{alarm.Name.Value}' já existe. Use a opção de sobrescrever para atualizar.",
                                ErrorType = "Business",
                                ErrorCode = "DUPLICATE_ALARM"
                            });
                            response.FailedImports++;
                        }
                    }
                    else
                    {
                        // Criar novo alarme
                        await _alarmRepository.AddAsync(alarmForUser);
                        response.SuccessfulImports++;
                        
                        response.ImportedAlarms.Add(new AlarmResponseDto(alarmForUser));
                        
                        _logger.LogDebug("Novo alarme criado: {AlarmName}", alarm.Name.Value);
                    }
                }
                catch (Exception ex)
                {
                    response.Errors.Add(new ImportErrorDto
                    {
                        LineNumber = index + 2,
                        ErrorMessage = $"Erro ao processar alarme '{alarm.Name?.Value}': {ex.Message}",
                        ErrorType = "Processing",
                        ErrorCode = "PROCESSING_ERROR"
                    });
                    response.FailedImports++;
                    
                    _logger.LogWarning(ex, "Erro ao processar alarme na linha {LineNumber}", index + 2);
                }
            }

            _logger.LogInformation("Importação concluída. {Success} sucessos, {Updated} atualizações, {Failed} falhas", 
                response.SuccessfulImports, response.UpdatedImports, response.FailedImports);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado durante importação de alarmes");
            
            response.Errors.Add(new ImportErrorDto
            {
                ErrorMessage = $"Erro inesperado: {ex.Message}",
                ErrorType = "System",
                ErrorCode = "SYSTEM_ERROR"
            });
            
            return response;
        }
    }
}
