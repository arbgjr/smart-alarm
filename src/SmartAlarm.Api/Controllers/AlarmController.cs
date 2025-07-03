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
using SmartAlarm.Application.DTOs;
using SmartAlarm.Application.Queries;
using SmartAlarm.Api.Services;

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

        public AlarmController(IMediator mediator, ILogger<AlarmController> logger, ICurrentUserService currentUserService)
        {
            _mediator = mediator;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Create a new alarm.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(AlarmResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateAlarmDto dto, CancellationToken cancellationToken)
        {
            var command = new CreateAlarmCommand(dto);
            var result = await _mediator.Send(command, cancellationToken);
            _logger.LogInformation("Alarm created: {AlarmId}", result.Id);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
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
    }
}
