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
using SmartAlarm.Application.Commands.Holiday;
using SmartAlarm.Application.DTOs.Holiday;
using SmartAlarm.Application.Queries.Holiday;
using Swashbuckle.AspNetCore.Annotations;

namespace SmartAlarm.Api.Controllers
{
    /// <summary>
    /// Controller para gerenciamento de feriados.
    /// Seguindo padrões Clean Architecture e SOLID.
    /// </summary>
    [ApiController]
    [Route("api/v1/holidays")]
    [Produces(MediaTypeNames.Application.Json)]
    [Authorize]
    [SwaggerTag("Gerenciamento de Feriados")]
    public class HolidaysController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<HolidaysController> _logger;

        public HolidaysController(IMediator mediator, ILogger<HolidaysController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Cria um novo feriado.
        /// </summary>
        /// <param name="dto">Dados do feriado a ser criado</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Feriado criado</returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Criar feriado", Description = "Cria um novo feriado no sistema")]
        [SwaggerResponse(StatusCodes.Status201Created, "Feriado criado com sucesso", typeof(HolidayResponseDto))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Dados inválidos")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Não autorizado")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Acesso negado")]
        [ProducesResponseType(typeof(HolidayResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Create([FromBody] CreateHolidayDto dto, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Creating holiday: {Description} on {Date}", dto.Description, dto.Date);

                var command = new CreateHolidayCommand(dto.Date, dto.Description);
                var result = await _mediator.Send(command, cancellationToken);

                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid holiday data: {Message}", ex.Message);
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating holiday");
                return StatusCode(500, new { Message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Lista todos os feriados.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Lista de feriados</returns>
        [HttpGet]
        [SwaggerOperation(Summary = "Listar feriados", Description = "Obtém lista de todos os feriados")]
        [SwaggerResponse(StatusCodes.Status200OK, "Lista de feriados", typeof(IEnumerable<HolidayResponseDto>))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Não autorizado")]
        [ProducesResponseType(typeof(IEnumerable<HolidayResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> List(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Listing all holidays");

                var query = new ListHolidaysQuery();
                var result = await _mediator.Send(query, cancellationToken);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing holidays");
                return StatusCode(500, new { Message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Busca um feriado por ID.
        /// </summary>
        /// <param name="id">ID do feriado</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Feriado encontrado</returns>
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Buscar feriado por ID", Description = "Obtém um feriado específico pelo ID")]
        [SwaggerResponse(StatusCodes.Status200OK, "Feriado encontrado", typeof(HolidayResponseDto))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Feriado não encontrado")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Não autorizado")]
        [ProducesResponseType(typeof(HolidayResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting holiday by ID: {HolidayId}", id);

                var query = new GetHolidayByIdQuery(id);
                var result = await _mediator.Send(query, cancellationToken);

                if (result == null)
                {
                    return NotFound(new { Message = "Feriado não encontrado" });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting holiday by ID: {HolidayId}", id);
                return StatusCode(500, new { Message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Atualiza um feriado existente.
        /// </summary>
        /// <param name="id">ID do feriado a ser atualizado</param>
        /// <param name="dto">Novos dados do feriado</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Feriado atualizado</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Atualizar feriado", Description = "Atualiza um feriado existente")]
        [SwaggerResponse(StatusCodes.Status200OK, "Feriado atualizado com sucesso", typeof(HolidayResponseDto))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Dados inválidos")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Feriado não encontrado")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Não autorizado")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Acesso negado")]
        [ProducesResponseType(typeof(HolidayResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateHolidayDto dto, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Updating holiday: {HolidayId}", id);

                var command = new UpdateHolidayCommand(id, dto.Date, dto.Description);
                var result = await _mediator.Send(command, cancellationToken);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid holiday data or holiday not found: {Message}", ex.Message);
                if (ex.Message.Contains("not found"))
                {
                    return NotFound(new { Message = ex.Message });
                }
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating holiday: {HolidayId}", id);
                return StatusCode(500, new { Message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Remove um feriado.
        /// </summary>
        /// <param name="id">ID do feriado a ser removido</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Confirmação da remoção</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Remover feriado", Description = "Remove um feriado do sistema")]
        [SwaggerResponse(StatusCodes.Status204NoContent, "Feriado removido com sucesso")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Feriado não encontrado")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Não autorizado")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Acesso negado")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Deleting holiday: {HolidayId}", id);

                var command = new DeleteHolidayCommand(id);
                var result = await _mediator.Send(command, cancellationToken);

                if (!result)
                {
                    return NotFound(new { Message = "Feriado não encontrado" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting holiday: {HolidayId}", id);
                return StatusCode(500, new { Message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Busca feriados por data específica.
        /// </summary>
        /// <param name="date">Data para buscar feriados (formato: yyyy-MM-dd)</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Lista de feriados na data especificada</returns>
        [HttpGet("by-date")]
        [SwaggerOperation(Summary = "Buscar feriados por data", Description = "Obtém feriados para uma data específica")]
        [SwaggerResponse(StatusCodes.Status200OK, "Lista de feriados na data", typeof(IEnumerable<HolidayResponseDto>))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Data inválida")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Não autorizado")]
        [ProducesResponseType(typeof(IEnumerable<HolidayResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetByDate([FromQuery] DateTime date, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting holidays for date: {Date}", date);

                var query = new GetHolidaysByDateQuery(date);
                var result = await _mediator.Send(query, cancellationToken);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting holidays by date: {Date}", date);
                return StatusCode(500, new { Message = "Erro interno do servidor" });
            }
        }
    }
}
