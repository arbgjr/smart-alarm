using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartAlarm.Api.Services;
using SmartAlarm.Application.Abstractions;
using SmartAlarm.Domain.Repositories;

namespace SmartAlarm.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AlarmSchedulingController : ControllerBase
{
    private readonly IAlarmTriggerService _alarmTriggerService;
    private readonly IAlarmRepository _alarmRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AlarmSchedulingController> _logger;

    public AlarmSchedulingController(
        IAlarmTriggerService alarmTriggerService,
        IAlarmRepository alarmRepository,
        ICurrentUserService currentUserService,
        ILogger<AlarmSchedulingController> logger)
    {
        _alarmTriggerService = alarmTriggerService;
        _alarmRepository = alarmRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpPost("{alarmId}/schedule")]
    public async Task<IActionResult> ScheduleAlarm(Guid alarmId)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var alarm = await _alarmRepository.GetByIdAsync(alarmId);
            if (alarm == null)
            {
                return NotFound(new { error = "Alarm not found" });
            }

            if (alarm.UserId.ToString() != userId)
            {
                return Forbid();
            }

            await _alarmTriggerService.ScheduleAlarmAsync(alarm);

            _logger.LogInformation("Scheduled alarm {AlarmId} for user {UserId}", alarmId, userId);
            return Ok(new { success = true, message = "Alarm scheduled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule alarm {AlarmId}", alarmId);
            return StatusCode(500, new { error = "Failed to schedule alarm" });
        }
    }

    [HttpPost("{alarmId}/cancel")]
    public async Task<IActionResult> CancelAlarm(Guid alarmId)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var alarm = await _alarmRepository.GetByIdAsync(alarmId);
            if (alarm == null)
            {
                return NotFound(new { error = "Alarm not found" });
            }

            if (alarm.UserId.ToString() != userId)
            {
                return Forbid();
            }

            await _alarmTriggerService.CancelAlarmAsync(alarmId);

            _logger.LogInformation("Cancelled alarm {AlarmId} for user {UserId}", alarmId, userId);
            return Ok(new { success = true, message = "Alarm cancelled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel alarm {AlarmId}", alarmId);
            return StatusCode(500, new { error = "Failed to cancel alarm" });
        }
    }

    [HttpPost("{alarmId}/reschedule")]
    public async Task<IActionResult> RescheduleAlarm(Guid alarmId)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var alarm = await _alarmRepository.GetByIdAsync(alarmId);
            if (alarm == null)
            {
                return NotFound(new { error = "Alarm not found" });
            }

            if (alarm.UserId.ToString() != userId)
            {
                return Forbid();
            }

            await _alarmTriggerService.RescheduleAlarmAsync(alarm);

            _logger.LogInformation("Rescheduled alarm {AlarmId} for user {UserId}", alarmId, userId);
            return Ok(new { success = true, message = "Alarm rescheduled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reschedule alarm {AlarmId}", alarmId);
            return StatusCode(500, new { error = "Failed to reschedule alarm" });
        }
    }

    [HttpPost("{alarmId}/snooze")]
    public async Task<IActionResult> SnoozeAlarm(Guid alarmId, [FromBody] SnoozeAlarmRequest request)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var alarm = await _alarmRepository.GetByIdAsync(alarmId);
            if (alarm == null)
            {
                return NotFound(new { error = "Alarm not found" });
            }

            if (alarm.UserId.ToString() != userId)
            {
                return Forbid();
            }

            // Record snooze event
            var alarmEventService = HttpContext.RequestServices.GetRequiredService<SmartAlarm.Application.Services.IAlarmEventService>();
            await alarmEventService.RecordAlarmSnoozedAsync(alarmId, alarm.UserId, request.SnoozeMinutes);

            // Schedule alarm to trigger again after snooze period
            var backgroundJobService = HttpContext.RequestServices.GetRequiredService<IBackgroundJobService>();
            backgroundJobService.ScheduleJob<IAlarmTriggerService>(
                service => service.TriggerAlarmAsync(alarmId, CancellationToken.None),
                DateTimeOffset.UtcNow.AddMinutes(request.SnoozeMinutes));

            _logger.LogInformation("Snoozed alarm {AlarmId} for {SnoozeMinutes} minutes for user {UserId}",
                alarmId, request.SnoozeMinutes, userId);

            return Ok(new { success = true, message = $"Alarm snoozed for {request.SnoozeMinutes} minutes" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to snooze alarm {AlarmId}", alarmId);
            return StatusCode(500, new { error = "Failed to snooze alarm" });
        }
    }

    [HttpPost("{alarmId}/dismiss")]
    public async Task<IActionResult> DismissAlarm(Guid alarmId)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var alarm = await _alarmRepository.GetByIdAsync(alarmId);
            if (alarm == null)
            {
                return NotFound(new { error = "Alarm not found" });
            }

            if (alarm.UserId.ToString() != userId)
            {
                return Forbid();
            }

            // Record dismiss event
            var alarmEventService = HttpContext.RequestServices.GetRequiredService<SmartAlarm.Application.Services.IAlarmEventService>();
            await alarmEventService.RecordAlarmDismissedAsync(alarmId, alarm.UserId);

            _logger.LogInformation("Dismissed alarm {AlarmId} for user {UserId}", alarmId, userId);
            return Ok(new { success = true, message = "Alarm dismissed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dismiss alarm {AlarmId}", alarmId);
            return StatusCode(500, new { error = "Failed to dismiss alarm" });
        }
    }
}

public class SnoozeAlarmRequest
{
    public int SnoozeMinutes { get; set; } = 5;
}
