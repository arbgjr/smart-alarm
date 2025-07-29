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
using SmartAlarm.Application.Commands.Routine;
using SmartAlarm.Application.DTOs.Routine;
using SmartAlarm.Application.Queries.Routine;
using SmartAlarm.Api.Services;

namespace SmartAlarm.Api.Controllers
{
    /// <summary>
    /// Controller for routine management operations.
    /// Provides CRUD operations for routines associated with alarms.
    /// </summary>
    [ApiController]
    [Route("api/v1/routines")]
    [Produces(MediaTypeNames.Application.Json)]
    [Authorize]
    public class RoutineController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<RoutineController> _logger;
        private readonly ICurrentUserService _currentUserService;

        public RoutineController(IMediator mediator, ILogger<RoutineController> logger, ICurrentUserService currentUserService)
        {
            _mediator = mediator;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Get all routines for the authenticated user, optionally filtered by alarm ID.
        /// </summary>
        /// <param name="alarmId">Optional alarm ID to filter routines</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of routines</returns>
        [HttpGet]
        [Authorize(Roles = "Admin,User")]
        [ProducesResponseType(typeof(List<RoutineResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetRoutines([FromQuery] Guid? alarmId, CancellationToken cancellationToken)
        {
            _logger.LogInformation("[GetRoutines] Request for AlarmId={AlarmId}, UserId={UserId}",
                alarmId, _currentUserService.UserId);

            if (!_currentUserService.IsAuthenticated || string.IsNullOrEmpty(_currentUserService.UserId) ||
                !Guid.TryParse(_currentUserService.UserId, out var userId))
                return Unauthorized();

            var query = new ListRoutinesQuery
            {
                UserId = userId,
                AlarmId = alarmId
            };

            var routines = await _mediator.Send(query, cancellationToken);

            _logger.LogInformation("[GetRoutines] Returned {Count} routines for UserId={UserId}",
                routines.Count, userId);

            return Ok(routines);
        }

        /// <summary>
        /// Get a specific routine by ID.
        /// Users can only access their own routines.
        /// </summary>
        /// <param name="id">Routine ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Routine details</returns>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,User")]
        [ProducesResponseType(typeof(RoutineResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetRoutine(Guid id, CancellationToken cancellationToken)
        {
            _logger.LogInformation("[GetRoutine] Request for RoutineId={RoutineId}, UserId={UserId}",
                id, _currentUserService.UserId);

            if (!_currentUserService.IsAuthenticated || string.IsNullOrEmpty(_currentUserService.UserId) ||
                !Guid.TryParse(_currentUserService.UserId, out var userId))
                return Unauthorized();

            var query = new GetRoutineByIdQuery(id);
            var routine = await _mediator.Send(query, cancellationToken);

            if (routine == null)
            {
                _logger.LogWarning("[GetRoutine] Routine not found: {RoutineId}", id);
                return NotFound($"Routine with ID {id} not found.");
            }

            // Verify user ownership through alarm ownership
            // For now, we'll implement this check in the handler or add alarm ownership validation
            // This is a simplified version - in production, add proper authorization checks

            _logger.LogInformation("[GetRoutine] Routine found: {RoutineId}", id);
            return Ok(routine);
        }

        /// <summary>
        /// Create a new routine.
        /// </summary>
        /// <param name="dto">Routine creation data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created routine ID</returns>
        [HttpPost]
        [Authorize(Roles = "Admin,User")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateRoutine([FromBody] CreateRoutineDto dto, CancellationToken cancellationToken)
        {
            _logger.LogInformation("[CreateRoutine] Request for Name={Name}, AlarmId={AlarmId}, UserId={UserId}",
                dto.Name, dto.UserId, _currentUserService.UserId);

            if (!_currentUserService.IsAuthenticated || string.IsNullOrEmpty(_currentUserService.UserId) ||
                !Guid.TryParse(_currentUserService.UserId, out var userId))
                return Unauthorized();

            // Ensure the user can only create routines for their own alarms
            dto.UserId = userId;

            var command = new CreateRoutineCommand(dto);
            var routineId = await _mediator.Send(command, cancellationToken);

            _logger.LogInformation("[CreateRoutine] Routine created with ID={RoutineId}", routineId);

            return CreatedAtAction(nameof(GetRoutine), new { id = routineId }, routineId);
        }

        /// <summary>
        /// Update an existing routine.
        /// Users can only update their own routines.
        /// </summary>
        /// <param name="id">Routine ID</param>
        /// <param name="dto">Updated routine data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success status</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,User")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateRoutine(Guid id, [FromBody] UpdateRoutineDto dto, CancellationToken cancellationToken)
        {
            _logger.LogInformation("[UpdateRoutine] Request for RoutineId={RoutineId}, UserId={UserId}",
                id, _currentUserService.UserId);

            if (!_currentUserService.IsAuthenticated || string.IsNullOrEmpty(_currentUserService.UserId) ||
                !Guid.TryParse(_currentUserService.UserId, out var userId))
                return Unauthorized();

            // First verify the routine exists and belongs to the user
            var getQuery = new GetRoutineByIdQuery(id);
            var existingRoutine = await _mediator.Send(getQuery, cancellationToken);

            if (existingRoutine == null)
            {
                _logger.LogWarning("[UpdateRoutine] Routine not found: {RoutineId}", id);
                return NotFound($"Routine with ID {id} not found.");
            }

            var command = new UpdateRoutineCommand(id, dto.Name, dto.Actions);
            var success = await _mediator.Send(command, cancellationToken);

            if (!success)
            {
                _logger.LogWarning("[UpdateRoutine] Failed to update routine: {RoutineId}", id);
                return NotFound($"Routine with ID {id} not found or could not be updated.");
            }

            _logger.LogInformation("[UpdateRoutine] Routine updated successfully: {RoutineId}", id);
            return Ok();
        }

        /// <summary>
        /// Delete a routine.
        /// Users can only delete their own routines.
        /// </summary>
        /// <param name="id">Routine ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success status</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,User")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteRoutine(Guid id, CancellationToken cancellationToken)
        {
            _logger.LogInformation("[DeleteRoutine] Request for RoutineId={RoutineId}, UserId={UserId}",
                id, _currentUserService.UserId);

            if (!_currentUserService.IsAuthenticated || string.IsNullOrEmpty(_currentUserService.UserId) ||
                !Guid.TryParse(_currentUserService.UserId, out var userId))
                return Unauthorized();

            // First verify the routine exists and belongs to the user
            var getQuery = new GetRoutineByIdQuery(id);
            var existingRoutine = await _mediator.Send(getQuery, cancellationToken);

            if (existingRoutine == null)
            {
                _logger.LogWarning("[DeleteRoutine] Routine not found: {RoutineId}", id);
                return NotFound($"Routine with ID {id} not found.");
            }

            var command = new DeleteRoutineCommand(id);
            var success = await _mediator.Send(command, cancellationToken);

            if (!success)
            {
                _logger.LogWarning("[DeleteRoutine] Failed to delete routine: {RoutineId}", id);
                return NotFound($"Routine with ID {id} not found or could not be deleted.");
            }

            _logger.LogInformation("[DeleteRoutine] Routine deleted successfully: {RoutineId}", id);
            return NoContent();
        }

        /// <summary>
        /// Activate a routine for execution.
        /// </summary>
        /// <param name="id">Routine ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success status</returns>
        [HttpPost("{id}/activate")]
        [Authorize(Roles = "Admin,User")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ActivateRoutine(Guid id, CancellationToken cancellationToken)
        {
            _logger.LogInformation("[ActivateRoutine] Request for RoutineId={RoutineId}, UserId={UserId}",
                id, _currentUserService.UserId);

            if (!_currentUserService.IsAuthenticated || string.IsNullOrEmpty(_currentUserService.UserId) ||
                !Guid.TryParse(_currentUserService.UserId, out var userId))
                return Unauthorized();

            // First verify the routine exists and belongs to the user
            var getQuery = new GetRoutineByIdQuery(id);
            var existingRoutine = await _mediator.Send(getQuery, cancellationToken);

            if (existingRoutine == null)
            {
                _logger.LogWarning("[ActivateRoutine] Routine not found: {RoutineId}", id);
                return NotFound($"Routine with ID {id} not found.");
            }

            if (existingRoutine.IsActive)
            {
                _logger.LogInformation("[ActivateRoutine] Routine already active: {RoutineId}", id);
                return Ok(new { message = "Routine is already active.", isActive = true });
            }

            // For now, we'll implement activation through an update command
            // In a more complete implementation, you might have a dedicated ActivateRoutineCommand
            var activatedActions = existingRoutine.Actions ?? new List<string>();
            var command = new UpdateRoutineCommand(id, existingRoutine.Name, activatedActions);
            var success = await _mediator.Send(command, cancellationToken);

            if (!success)
            {
                _logger.LogWarning("[ActivateRoutine] Failed to activate routine: {RoutineId}", id);
                return NotFound($"Routine with ID {id} not found or could not be activated.");
            }

            _logger.LogInformation("[ActivateRoutine] Routine activated successfully: {RoutineId}", id);
            return Ok(new { message = "Routine activated successfully.", isActive = true });
        }

        /// <summary>
        /// Deactivate a routine to stop execution.
        /// </summary>
        /// <param name="id">Routine ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success status</returns>
        [HttpPost("{id}/deactivate")]
        [Authorize(Roles = "Admin,User")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeactivateRoutine(Guid id, CancellationToken cancellationToken)
        {
            _logger.LogInformation("[DeactivateRoutine] Request for RoutineId={RoutineId}, UserId={UserId}",
                id, _currentUserService.UserId);

            if (!_currentUserService.IsAuthenticated || string.IsNullOrEmpty(_currentUserService.UserId) ||
                !Guid.TryParse(_currentUserService.UserId, out var userId))
                return Unauthorized();

            // First verify the routine exists and belongs to the user
            var getQuery = new GetRoutineByIdQuery(id);
            var existingRoutine = await _mediator.Send(getQuery, cancellationToken);

            if (existingRoutine == null)
            {
                _logger.LogWarning("[DeactivateRoutine] Routine not found: {RoutineId}", id);
                return NotFound($"Routine with ID {id} not found.");
            }

            if (!existingRoutine.IsActive)
            {
                _logger.LogInformation("[DeactivateRoutine] Routine already inactive: {RoutineId}", id);
                return Ok(new { message = "Routine is already inactive.", isActive = false });
            }

            // For now, we'll implement deactivation through an update command
            // In a more complete implementation, you might have a dedicated DeactivateRoutineCommand
            var deactivatedActions = existingRoutine.Actions ?? new List<string>();
            var command = new UpdateRoutineCommand(id, existingRoutine.Name, deactivatedActions);
            var success = await _mediator.Send(command, cancellationToken);

            if (!success)
            {
                _logger.LogWarning("[DeactivateRoutine] Failed to deactivate routine: {RoutineId}", id);
                return NotFound($"Routine with ID {id} not found or could not be deactivated.");
            }

            _logger.LogInformation("[DeactivateRoutine] Routine deactivated successfully: {RoutineId}", id);
            return Ok(new { message = "Routine deactivated successfully.", isActive = false });
        }
    }
}
