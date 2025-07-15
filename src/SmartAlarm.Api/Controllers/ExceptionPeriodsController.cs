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
using SmartAlarm.Application.Commands.ExceptionPeriod;
using SmartAlarm.Application.DTOs.ExceptionPeriod;
using SmartAlarm.Application.Queries.ExceptionPeriod;
using SmartAlarm.Api.Services;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Api.Controllers
{
    /// <summary>
    /// Controller para gerenciamento de períodos de exceção de alarmes.
    /// </summary>
    [ApiController]
    [Route("api/v1/exception-periods")]
    [Produces(MediaTypeNames.Application.Json)]
    [Authorize]
    public class ExceptionPeriodsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ExceptionPeriodsController> _logger;
        private readonly ICurrentUserService _currentUserService;

        public ExceptionPeriodsController(
            IMediator mediator, 
            ILogger<ExceptionPeriodsController> logger, 
            ICurrentUserService currentUserService)
        {
            _mediator = mediator;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Cria um novo período de exceção.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,User")]
        [ProducesResponseType(typeof(ExceptionPeriodDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Create([FromBody] CreateExceptionPeriodDto dto, CancellationToken cancellationToken)
        {
            _logger.LogInformation("[Create] Payload recebido: Name={Name}, StartDate={StartDate}, EndDate={EndDate}, Type={Type}", 
                dto.Name, dto.StartDate, dto.EndDate, dto.Type);

            // Verifica autenticação e obtém UserId
            if (!_currentUserService.IsAuthenticated || 
                string.IsNullOrEmpty(_currentUserService.UserId) || 
                !Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                _logger.LogWarning("[Create] Usuário não autenticado ou UserId inválido");
                return Unauthorized();
            }

            try
            {
                var command = CreateExceptionPeriodCommand.FromDto(dto, userId);
                var result = await _mediator.Send(command, cancellationToken);
                
                _logger.LogInformation("Período de exceção criado: {PeriodId} para usuário {UserId}", result.Id, userId);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (FluentValidation.ValidationException ex)
            {
                _logger.LogWarning("[Create] ValidationException: {Errors}", 
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
            catch (ArgumentException ex)
            {
                _logger.LogWarning("[Create] ArgumentException: {Message}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Lista os períodos de exceção do usuário autenticado com filtros opcionais.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ExceptionPeriodDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> List(
            [FromQuery] ExceptionPeriodType? type,
            [FromQuery] DateTime? activeOnDate,
            [FromQuery] bool onlyActive = true,
            CancellationToken cancellationToken = default)
        {
            // Verifica autenticação
            if (!_currentUserService.IsAuthenticated || 
                string.IsNullOrEmpty(_currentUserService.UserId) || 
                !Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                _logger.LogWarning("[List] Usuário não autenticado ou UserId inválido");
                return Unauthorized();
            }

            _logger.LogInformation("[List] Listando períodos para usuário {UserId}, Type={Type}, ActiveOnDate={ActiveOnDate}, OnlyActive={OnlyActive}", 
                userId, type, activeOnDate, onlyActive);

            var query = new ListExceptionPeriodsQuery(userId)
            {
                Type = type,
                ActiveOnDate = activeOnDate,
                OnlyActive = onlyActive
            };

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Obtém um período de exceção específico por ID.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ExceptionPeriodDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            // Verifica autenticação
            if (!_currentUserService.IsAuthenticated || 
                string.IsNullOrEmpty(_currentUserService.UserId) || 
                !Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                _logger.LogWarning("[GetById] Usuário não autenticado ou UserId inválido");
                return Unauthorized();
            }

            _logger.LogInformation("[GetById] Buscando período {PeriodId} para usuário {UserId}", id, userId);

            var query = new GetExceptionPeriodByIdQuery(id, userId);
            var result = await _mediator.Send(query, cancellationToken);
            
            if (result == null)
            {
                _logger.LogWarning("[GetById] Período {PeriodId} não encontrado ou não pertence ao usuário {UserId}", id, userId);
                return NotFound();
            }

            return Ok(result);
        }

        /// <summary>
        /// Atualiza um período de exceção existente.
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,User")]
        [ProducesResponseType(typeof(ExceptionPeriodDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateExceptionPeriodDto dto, CancellationToken cancellationToken)
        {
            // Verifica autenticação
            if (!_currentUserService.IsAuthenticated || 
                string.IsNullOrEmpty(_currentUserService.UserId) || 
                !Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                _logger.LogWarning("[Update] Usuário não autenticado ou UserId inválido");
                return Unauthorized();
            }

            _logger.LogInformation("[Update] Atualizando período {PeriodId} para usuário {UserId}", id, userId);

            try
            {
                var command = UpdateExceptionPeriodCommand.FromDto(id, dto, userId);
                var result = await _mediator.Send(command, cancellationToken);
                
                if (result == null)
                {
                    _logger.LogWarning("[Update] Período {PeriodId} não encontrado ou não pertence ao usuário {UserId}", id, userId);
                    return NotFound();
                }

                _logger.LogInformation("Período de exceção atualizado: {PeriodId} para usuário {UserId}", id, userId);
                return Ok(result);
            }
            catch (FluentValidation.ValidationException ex)
            {
                _logger.LogWarning("[Update] ValidationException: {Errors}", 
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
            catch (ArgumentException ex)
            {
                _logger.LogWarning("[Update] ArgumentException: {Message}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Exclui um período de exceção.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,User")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            // Verifica autenticação
            if (!_currentUserService.IsAuthenticated || 
                string.IsNullOrEmpty(_currentUserService.UserId) || 
                !Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                _logger.LogWarning("[Delete] Usuário não autenticado ou UserId inválido");
                return Unauthorized();
            }

            _logger.LogInformation("[Delete] Excluindo período {PeriodId} para usuário {UserId}", id, userId);

            var command = new DeleteExceptionPeriodCommand(id, userId);
            var result = await _mediator.Send(command, cancellationToken);
            
            if (!result)
            {
                _logger.LogWarning("[Delete] Período {PeriodId} não encontrado ou não pertence ao usuário {UserId}", id, userId);
                return NotFound();
            }

            _logger.LogInformation("Período de exceção excluído: {PeriodId} para usuário {UserId}", id, userId);
            return NoContent();
        }

        /// <summary>
        /// Obtém períodos de exceção ativos em uma data específica.
        /// </summary>
        [HttpGet("active-on/{date}")]
        [ProducesResponseType(typeof(IEnumerable<ExceptionPeriodDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetActiveOnDate(DateTime date, CancellationToken cancellationToken)
        {
            // Verifica autenticação
            if (!_currentUserService.IsAuthenticated || 
                string.IsNullOrEmpty(_currentUserService.UserId) || 
                !Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                _logger.LogWarning("[GetActiveOnDate] Usuário não autenticado ou UserId inválido");
                return Unauthorized();
            }

            _logger.LogInformation("[GetActiveOnDate] Buscando períodos ativos em {Date} para usuário {UserId}", date, userId);

            var query = new GetActiveExceptionPeriodsOnDateQuery(userId, date);
            var result = await _mediator.Send(query, cancellationToken);
            
            return Ok(result);
        }
    }
}
