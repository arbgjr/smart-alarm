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
using SmartAlarm.Application.Commands.UserHolidayPreference;
using SmartAlarm.Application.DTOs.UserHolidayPreference;
using SmartAlarm.Application.Queries.UserHolidayPreference;
using Swashbuckle.AspNetCore.Annotations;

namespace SmartAlarm.Api.Controllers
{
    /// <summary>
    /// Controller para gerenciamento de preferências de feriados dos usuários.
    /// Define como alarmes se comportam durante feriados específicos.
    /// Seguindo padrões Clean Architecture e SOLID.
    /// </summary>
    [ApiController]
    [Route("api/v1/user-holiday-preferences")]
    [Produces(MediaTypeNames.Application.Json)]
    [Authorize]
    [SwaggerTag("Preferências de Feriados dos Usuários")]
    public class UserHolidayPreferencesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<UserHolidayPreferencesController> _logger;

        public UserHolidayPreferencesController(IMediator mediator, ILogger<UserHolidayPreferencesController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Cria uma nova preferência de feriado para um usuário.
        /// </summary>
        /// <param name="dto">Dados da preferência a ser criada</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Preferência criada com dados relacionados</returns>
        [HttpPost]
        [SwaggerOperation(Summary = "Criar preferência de feriado", Description = "Cria uma nova preferência de feriado para o usuário")]
        [SwaggerResponse(StatusCodes.Status201Created, "Preferência criada com sucesso", typeof(UserHolidayPreferenceResponseDto))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Dados inválidos")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Não autorizado")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Preferência já existe para este usuário e feriado")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Erro interno do servidor")]
        public async Task<ActionResult<UserHolidayPreferenceResponseDto>> CreateUserHolidayPreference(
            [FromBody] CreateUserHolidayPreferenceDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Creating user holiday preference for User: {UserId}, Holiday: {HolidayId}", 
                    dto.UserId, dto.HolidayId);

                var command = new CreateUserHolidayPreferenceCommand
                {
                    UserId = dto.UserId,
                    HolidayId = dto.HolidayId,
                    Action = dto.Action,
                    DelayInMinutes = dto.DelayInMinutes,
                    IsEnabled = dto.IsEnabled
                };

                var result = await _mediator.Send(command, cancellationToken);

                _logger.LogInformation("User holiday preference created successfully with ID: {PreferenceId}", result.Id);

                return CreatedAtAction(
                    nameof(GetUserHolidayPreferenceById),
                    new { id = result.Id },
                    result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid data provided for creating user holiday preference");
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Conflict when creating user holiday preference");
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user holiday preference");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Obtém uma preferência de feriado por ID.
        /// </summary>
        /// <param name="id">ID da preferência</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Preferência com dados relacionados</returns>
        [HttpGet("{id:guid}")]
        [SwaggerOperation(Summary = "Obter preferência por ID", Description = "Obtém uma preferência de feriado específica pelo ID")]
        [SwaggerResponse(StatusCodes.Status200OK, "Preferência encontrada", typeof(UserHolidayPreferenceResponseDto))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Não autorizado")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Preferência não encontrada")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Erro interno do servidor")]
        public async Task<ActionResult<UserHolidayPreferenceResponseDto>> GetUserHolidayPreferenceById(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting user holiday preference by ID: {PreferenceId}", id);

                var query = new GetUserHolidayPreferenceByIdQuery { Id = id };
                var result = await _mediator.Send(query, cancellationToken);

                if (result == null)
                {
                    _logger.LogWarning("User holiday preference not found with ID: {PreferenceId}", id);
                    return NotFound(new { message = $"Preferência com ID {id} não encontrada" });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user holiday preference by ID: {PreferenceId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Lista todas as preferências de feriados de um usuário.
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Lista de preferências do usuário</returns>
        [HttpGet("user/{userId:guid}")]
        [SwaggerOperation(Summary = "Listar preferências por usuário", Description = "Lista todas as preferências de feriados de um usuário específico")]
        [SwaggerResponse(StatusCodes.Status200OK, "Lista de preferências", typeof(IEnumerable<UserHolidayPreferenceResponseDto>))]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Não autorizado")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Erro interno do servidor")]
        public async Task<ActionResult<IEnumerable<UserHolidayPreferenceResponseDto>>> GetUserHolidayPreferencesByUser(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting user holiday preferences for user: {UserId}", userId);

                var query = new ListUserHolidayPreferencesQuery { UserId = userId };
                var result = await _mediator.Send(query, cancellationToken);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user holiday preferences for user: {UserId}", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Obtém preferências aplicáveis para um usuário em uma data específica.
        /// </summary>
        /// <param name="userId">ID do usuário</param>
        /// <param name="date">Data para verificar (formato YYYY-MM-DD)</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Lista de preferências aplicáveis na data</returns>
        [HttpGet("user/{userId:guid}/applicable")]
        [SwaggerOperation(Summary = "Obter preferências aplicáveis", Description = "Obtém preferências de feriados aplicáveis para um usuário em uma data específica")]
        [SwaggerResponse(StatusCodes.Status200OK, "Lista de preferências aplicáveis", typeof(IEnumerable<UserHolidayPreferenceResponseDto>))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Data inválida")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Não autorizado")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Erro interno do servidor")]
        public async Task<ActionResult<IEnumerable<UserHolidayPreferenceResponseDto>>> GetApplicableUserHolidayPreferences(
            Guid userId,
            [FromQuery] DateTime date,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting applicable user holiday preferences for user: {UserId} on date: {Date}", 
                    userId, date);

                var query = new GetApplicablePreferencesForDateQuery 
                { 
                    UserId = userId, 
                    Date = date 
                };
                var result = await _mediator.Send(query, cancellationToken);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting applicable user holiday preferences for user: {UserId} on date: {Date}", 
                    userId, date);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Atualiza uma preferência de feriado existente.
        /// </summary>
        /// <param name="id">ID da preferência a ser atualizada</param>
        /// <param name="dto">Dados atualizados da preferência</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Preferência atualizada</returns>
        [HttpPut("{id:guid}")]
        [SwaggerOperation(Summary = "Atualizar preferência", Description = "Atualiza uma preferência de feriado existente")]
        [SwaggerResponse(StatusCodes.Status200OK, "Preferência atualizada com sucesso", typeof(UserHolidayPreferenceResponseDto))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Dados inválidos")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Não autorizado")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Preferência não encontrada")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Erro interno do servidor")]
        public async Task<ActionResult<UserHolidayPreferenceResponseDto>> UpdateUserHolidayPreference(
            Guid id,
            [FromBody] UpdateUserHolidayPreferenceDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Updating user holiday preference with ID: {PreferenceId}", id);

                var command = new UpdateUserHolidayPreferenceCommand
                {
                    Id = id,
                    Action = dto.Action,
                    DelayInMinutes = dto.DelayInMinutes,
                    IsEnabled = dto.IsEnabled
                };

                var result = await _mediator.Send(command, cancellationToken);

                if (result == null)
                {
                    _logger.LogWarning("User holiday preference not found for update with ID: {PreferenceId}", id);
                    return NotFound(new { message = $"Preferência com ID {id} não encontrada" });
                }

                _logger.LogInformation("User holiday preference updated successfully with ID: {PreferenceId}", id);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid data provided for updating user holiday preference with ID: {PreferenceId}", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user holiday preference with ID: {PreferenceId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Remove uma preferência de feriado.
        /// </summary>
        /// <param name="id">ID da preferência a ser removida</param>
        /// <param name="cancellationToken">Token de cancelamento</param>
        /// <returns>Resultado da operação</returns>
        [HttpDelete("{id:guid}")]
        [SwaggerOperation(Summary = "Remover preferência", Description = "Remove uma preferência de feriado do sistema")]
        [SwaggerResponse(StatusCodes.Status204NoContent, "Preferência removida com sucesso")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Não autorizado")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Preferência não encontrada")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Erro interno do servidor")]
        public async Task<IActionResult> DeleteUserHolidayPreference(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Deleting user holiday preference with ID: {PreferenceId}", id);

                var command = new DeleteUserHolidayPreferenceCommand { Id = id };
                var result = await _mediator.Send(command, cancellationToken);

                if (!result)
                {
                    _logger.LogWarning("User holiday preference not found for deletion with ID: {PreferenceId}", id);
                    return NotFound(new { message = $"Preferência com ID {id} não encontrada" });
                }

                _logger.LogInformation("User holiday preference deleted successfully with ID: {PreferenceId}", id);

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid ID provided for deleting user holiday preference: {PreferenceId}", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user holiday preference with ID: {PreferenceId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "Erro interno do servidor" });
            }
        }
    }
}
