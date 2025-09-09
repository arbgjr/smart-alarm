using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Commands;
using SmartAlarm.Application.Commands.Import;
using SmartAlarm.Application.DTOs;
using SmartAlarm.Application.DTOs.Import;
using SmartAlarm.Application.Queries;
using SmartAlarm.Api.Services;
using SmartAlarm.Application.Services;

namespace SmartAlarm.Api.Controllers
{
    [ApiController]
    [Route("api/v1/alarms")]
    [Produces(MediaTypeNames.Application.Json)]
    [Authorize]
    public class AlarmController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<AlarmController> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAlarmEventService _alarmEventService;

        public AlarmController(
            IMediator mediator, 
            ILogger<AlarmController> logger, 
            ICurrentUserService currentUserService,
            IAlarmEventService alarmEventService)
        {
            _mediator = mediator;
            _logger = logger;
            _currentUserService = currentUserService;
            _alarmEventService = alarmEventService;
        }

        /// <summary>
        /// Create a new alarm.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,User")]
        [ProducesResponseType(typeof(AlarmResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateAlarmDto dto, CancellationToken cancellationToken)
        {
            // Preenche o UserId a partir do usuário autenticado
            _logger.LogInformation("[Create] Payload recebido: Name={Name}, Time={Time}, UserId={UserId}", dto.Name, dto.Time, dto.UserId);
            if (!_currentUserService.IsAuthenticated || string.IsNullOrEmpty(_currentUserService.UserId) || !Guid.TryParse(_currentUserService.UserId, out var userId))
                return Unauthorized();
            dto.UserId = userId;
            _logger.LogInformation("[Create] Após preencher UserId: Name={Name}, Time={Time}, UserId={UserId}", dto.Name, dto.Time, dto.UserId);
            try
            {
                var command = new CreateAlarmCommand(dto);
                var result = await _mediator.Send(command, cancellationToken);
                _logger.LogInformation("Alarm created: {AlarmId}", result.Id);
                
                // Automatically record alarm creation event
                try
                {
                    await _alarmEventService.RecordAlarmCreatedAsync(
                        result.Id, 
                        userId, 
                        $"Alarm '{result.Name}' created at {result.Time}", 
                        cancellationToken);
                    _logger.LogDebug("Alarm creation event recorded for: {AlarmId}", result.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to record alarm creation event for: {AlarmId}", result.Id);
                    // Don't fail the request if event recording fails
                }
                
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (FluentValidation.ValidationException ex)
            {
                _logger.LogWarning("[Create] ValidationException: {Errors}", string.Join("; ", ex.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));
                // Monta resposta de erro de validação no padrão da API
                var errorResponse = new SmartAlarm.Api.Models.ErrorResponse
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Title = "Erro de validação",
                    Detail = "Um ou mais campos estão inválidos.",
                    Type = "ValidationError",
                    TraceId = HttpContext.TraceIdentifier,
                    Timestamp = DateTime.UtcNow,
                    ValidationErrors = ex.Errors.Select(e => new SmartAlarm.Api.Models.ValidationError
                    {
                        Field = e.PropertyName,
                        Message = e.ErrorMessage,
                        Code = e.ErrorCode,
                        AttemptedValue = e.AttemptedValue
                    }).ToList()
                };
                return BadRequest(errorResponse);
            }
        }

        /// <summary>
        /// Get all alarms for the authenticated user.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<AlarmResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> List(CancellationToken cancellationToken)
        {
            if (!_currentUserService.IsAuthenticated || string.IsNullOrEmpty(_currentUserService.UserId) || !Guid.TryParse(_currentUserService.UserId, out var guid))
                return Unauthorized();
            var query = new ListAlarmsQuery(guid);
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Get alarm by id.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(AlarmResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetAlarmByIdQuery(id);
            var result = await _mediator.Send(query, cancellationToken);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        /// <summary>
        /// Update an alarm.
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,User")]
        [ProducesResponseType(typeof(AlarmResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAlarmCommand command, CancellationToken cancellationToken)
        {
            if (id != command.AlarmId)
                return BadRequest("Id mismatch");
            var result = await _mediator.Send(command, cancellationToken);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        /// <summary>
        /// Delete an alarm.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var command = new DeleteAlarmCommand(id);
            var result = await _mediator.Send(command, cancellationToken);
            if (!result)
                return NotFound();
            return NoContent();
        }

        /// <summary>
        /// Import alarms from a CSV file.
        /// </summary>
        /// <param name="file">CSV file containing alarms to import</param>
        /// <param name="overwriteExisting">Whether to overwrite existing alarms with the same name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Import result with success/failure counts and details</returns>
        [HttpPost("import")]
        [Authorize(Roles = "Admin,User")]
        [ProducesResponseType(typeof(ImportAlarmsResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
        [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
        public async Task<IActionResult> ImportAlarms(
            [FromForm] IFormFile file,
            [FromForm] bool overwriteExisting = false,
            CancellationToken cancellationToken = default)
        {
            // Obter userId do usuário autenticado
            if (!_currentUserService.IsAuthenticated || 
                string.IsNullOrEmpty(_currentUserService.UserId) || 
                !Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                return Unauthorized();
            }

            // Validações básicas do arquivo
            if (file == null || file.Length == 0)
            {
                return BadRequest(new SmartAlarm.Api.Models.ErrorResponse
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Title = "Arquivo obrigatório",
                    Detail = "Nenhum arquivo foi enviado ou o arquivo está vazio.",
                    Type = "ValidationError",
                    TraceId = HttpContext.TraceIdentifier,
                    Timestamp = DateTime.UtcNow
                });
            }

            // Validação do tamanho do arquivo (máximo 5MB)
            const long maxFileSize = 5 * 1024 * 1024; // 5MB
            if (file.Length > maxFileSize)
            {
                return StatusCode(StatusCodes.Status413PayloadTooLarge, new SmartAlarm.Api.Models.ErrorResponse
                {
                    StatusCode = StatusCodes.Status413PayloadTooLarge,
                    Title = "Arquivo muito grande",
                    Detail = $"O arquivo deve ter no máximo {maxFileSize / (1024 * 1024)}MB. Tamanho atual: {file.Length / (1024 * 1024)}MB.",
                    Type = "FileSizeError",
                    TraceId = HttpContext.TraceIdentifier,
                    Timestamp = DateTime.UtcNow
                });
            }

            // Validação do tipo de arquivo
            var allowedExtensions = new[] { ".csv" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return StatusCode(StatusCodes.Status415UnsupportedMediaType, new SmartAlarm.Api.Models.ErrorResponse
                {
                    StatusCode = StatusCodes.Status415UnsupportedMediaType,
                    Title = "Tipo de arquivo não suportado",
                    Detail = $"Apenas arquivos CSV são aceitos. Extensões permitidas: {string.Join(", ", allowedExtensions)}",
                    Type = "FileTypeError",
                    TraceId = HttpContext.TraceIdentifier,
                    Timestamp = DateTime.UtcNow
                });
            }

            _logger.LogInformation("Iniciando importação de alarmes. UserId: {UserId}, FileName: {FileName}, FileSize: {FileSize}", 
                userId, file.FileName, file.Length);

            try
            {
                using var stream = file.OpenReadStream();
                var command = new ImportAlarmsCommand(stream, file.FileName, userId, overwriteExisting);
                var result = await _mediator.Send(command, cancellationToken);

                _logger.LogInformation("Importação concluída. Total: {Total}, Sucessos: {Success}, Falhas: {Failed}, Atualizações: {Updated}", 
                    result.TotalRecords, result.SuccessfulImports, result.FailedImports, result.UpdatedImports);

                return Ok(result);
            }
            catch (FluentValidation.ValidationException ex)
            {
                _logger.LogWarning("Erro de validação na importação: {Errors}", 
                    string.Join("; ", ex.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));

                var errorResponse = new SmartAlarm.Api.Models.ErrorResponse
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Title = "Erro de validação",
                    Detail = "Um ou mais campos estão inválidos.",
                    Type = "ValidationError",
                    TraceId = HttpContext.TraceIdentifier,
                    Timestamp = DateTime.UtcNow,
                    ValidationErrors = ex.Errors.Select(e => new SmartAlarm.Api.Models.ValidationError
                    {
                        Field = e.PropertyName,
                        Message = e.ErrorMessage,
                        Code = e.ErrorCode,
                        AttemptedValue = e.AttemptedValue
                    }).ToList()
                };
                return BadRequest(errorResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado durante importação de alarmes");
                return StatusCode(StatusCodes.Status500InternalServerError, new SmartAlarm.Api.Models.ErrorResponse
                {
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Title = "Erro interno do servidor",
                    Detail = "Ocorreu um erro inesperado durante a importação.",
                    Type = "SystemError",
                    TraceId = HttpContext.TraceIdentifier,
                    Timestamp = DateTime.UtcNow
                });
            }
        }
    }
}
