using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartAlarm.Api.DTOs;
using SmartAlarm.Api.Services;
using SmartAlarm.Application.Common;
using SmartAlarm.Application.DTOs;
using SmartAlarm.Application.Routines.Commands;
using SmartAlarm.Application.Routines.Queries;
using Swashbuckle.AspNetCore.Annotations;

namespace SmartAlarm.Api.Controllers
{
    /// <summary>
    /// Fornece endpoints para o gerenciamento completo de rotinas de alarme.
    /// </summary>
    [ApiController]
    [Route("api/v1/routines")]
    [Authorize]
    [Produces(MediaTypeNames.Application.Json)]
    [SwaggerTag("Gerenciamento de Rotinas")]
    public class RoutineController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUserService;

        public RoutineController(IMediator mediator, ICurrentUserService currentUserService)
        {
            _mediator = mediator;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Lista todas as rotinas pertencentes ao usuário autenticado.
        /// </summary>
        /// <remarks>Este endpoint suporta paginação para lidar com grandes volumes de dados de forma eficiente.</remarks>
        /// <param name="pageNumber">O número da página a ser retornada.</param>
        /// <param name="pageSize">O número de itens por página.</param>
        [HttpGet]
        [SwaggerOperation(Summary = "Listar rotinas do usuário com paginação", Description = "Retorna uma lista paginada das rotinas do usuário autenticado.")]
        [ProducesResponseType(typeof(PaginatedList<RoutineDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetRoutines([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var userId = _currentUserService.GetUserId();
            var query = new ListUserRoutinesQuery(
                userId,
                pageNumber,
                pageSize
            );
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Obtém os detalhes de uma rotina específica pelo seu ID.
        /// </summary>
        /// <param name="id">O ID da rotina.</param>
        [HttpGet("{id:guid}", Name = "GetRoutineById")]
        [ProducesResponseType(typeof(RoutineDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRoutineById(Guid id)
        {
            var query = new GetRoutineByIdQuery(id);
            var result = await _mediator.Send(query);
            return result != null ? Ok(result) : NotFound();
        }

        /// <summary>
        /// Cria uma nova rotina para o usuário autenticado.
        /// </summary>
        /// <param name="createDto">Os dados para a criação da rotina.</param>
        [HttpPost]
        [ProducesResponseType(typeof(RoutineDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateRoutine([FromBody] CreateRoutineDto createDto)
        {
            var userId = _currentUserService.GetUserId();
            var command = new CreateRoutineCommand(
                createDto.Name,
                createDto.Description,
                userId,
                createDto.AlarmIds.FirstOrDefault(), // Using first alarm ID
                new List<string>(), // Empty actions list for now
                true);

            var result = await _mediator.Send(command);

            return CreatedAtRoute("GetRoutineById", new { id = result.Id }, result);
        }

        /// <summary>
        /// Atualiza uma rotina existente.
        /// </summary>
        /// <param name="id">O ID da rotina a ser atualizada.</param>
        /// <param name="updateDto">Os novos dados para a rotina.</param>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateRoutine(Guid id, [FromBody] UpdateRoutineDto updateDto)
        {
            var command = new UpdateRoutineCommand(
                id,
                updateDto.Name,
                updateDto.Description,
                new List<string>(), // Empty actions list for now
                updateDto.IsActive);

            var result = await _mediator.Send(command);

            if (result == null)
                return NotFound();

            return NoContent();
        }        /// <summary>
        /// Exclui uma rotina específica.
        /// </summary>
        /// <param name="id">O ID da rotina a ser excluída.</param>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteRoutine(Guid id)
        {
            var command = new DeleteRoutineCommand(id);
            var result = await _mediator.Send(command);

            if (!result)
                return NotFound();

            return NoContent();
        }

        /// <summary>
        /// Ativa uma rotina, tornando-a habilitada para execução.
        /// </summary>
        /// <param name="id">O ID da rotina a ser ativada.</param>
        [HttpPost("{id:guid}/activate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ActivateRoutine(Guid id)
        {
            var command = new ActivateRoutineCommand(id);
            var result = await _mediator.Send(command);

            if (!result)
                return NotFound();

            return Ok(new { message = $"Rotina {id} ativada com sucesso." });
        }

        /// <summary>
        /// Desativa uma rotina, tornando-a desabilitada para execução.
        /// </summary>
        /// <param name="id">O ID da rotina a ser desativada.</param>
        [HttpPost("{id:guid}/deactivate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeactivateRoutine(Guid id)
        {
            var command = new DeactivateRoutineCommand(id);
            var result = await _mediator.Send(command);

            if (!result)
                return NotFound();

            return Ok(new { message = $"Rotina {id} desativada com sucesso." });
        }

        /// <summary>
        /// Executa uma ação em lote (ativar, desativar, excluir) em múltiplas rotinas.
        /// </summary>
        /// <remarks>Use este endpoint para aplicar uma mesma ação a vários IDs de rotina de uma só vez, otimizando as chamadas de API.</remarks>
        /// <param name="bulkUpdateDto">Os IDs das rotinas e a ação a ser executada.</param>
        [HttpPost("bulk-update")]
        [SwaggerOperation(Summary = "Executar ações em lote em rotinas", Description = "Permite ativar, desativar ou excluir múltiplas rotinas em uma única requisição.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> BulkUpdateRoutines([FromBody] BulkRoutineUpdateDto bulkUpdateDto)
        {
            var userId = _currentUserService.GetUserId();

            var command = new BulkUpdateRoutinesCommand(
                userId,
                bulkUpdateDto.RoutineIds,
                bulkUpdateDto.Action
            );

            await _mediator.Send(command);

            return Ok(new { message = $"{bulkUpdateDto.RoutineIds.Count} rotinas processadas com a ação '{bulkUpdateDto.Action}'." });
        }
    }
}
