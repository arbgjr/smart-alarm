using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartAlarm.Api.Services;
using SmartAlarm.Application.Abstractions;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AuditController> _logger;

    public AuditController(
        IAuditService auditService,
        ICurrentUserService currentUserService,
        ILogger<AuditController> logger)
    {
        _auditService = auditService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpGet("my-activity")]
    public async Task<IActionResult> GetMyActivity([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            var auditLogs = await _auditService.GetUserAuditTrailAsync(userGuid, startDate, endDate);

            var result = auditLogs.Select(log => new
            {
                log.Id,
                log.Action,
                log.EntityType,
                log.EntityId,
                log.Timestamp,
                log.IpAddress,
                log.Level,
                HasOldValues = !string.IsNullOrEmpty(log.OldValues),
                HasNewValues = !string.IsNullOrEmpty(log.NewValues)
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user activity");
            return StatusCode(500, new { error = "Failed to retrieve activity logs" });
        }
    }

    [HttpGet("entity/{entityType}/{entityId}")]
    public async Task<IActionResult> GetEntityAuditTrail(string entityType, string entityId)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Basic authorization - users can only see audit trails for their own entities
            // In a more complex system, you might have role-based access control here
            var auditLogs = await _auditService.GetEntityAuditTrailAsync(entityType, entityId);

            // Filter to only show logs for the current user or system logs
            var filteredLogs = auditLogs.Where(log =>
                log.UserId?.ToString() == userId ||
                log.EntityType == "System");

            var result = filteredLogs.Select(log => new
            {
                log.Id,
                log.Action,
                log.EntityType,
                log.EntityId,
                log.Timestamp,
                log.IpAddress,
                log.Level,
                log.UserId,
                HasOldValues = !string.IsNullOrEmpty(log.OldValues),
                HasNewValues = !string.IsNullOrEmpty(log.NewValues)
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get entity audit trail for {EntityType} {EntityId}", entityType, entityId);
            return StatusCode(500, new { error = "Failed to retrieve audit trail" });
        }
    }

    [HttpGet("security-events")]
    [Authorize(Roles = "Admin")] // Only admins can view security events
    public async Task<IActionResult> GetSecurityEvents([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var securityEvents = await _auditService.GetSecurityEventsAsync(startDate, endDate);

            var result = securityEvents.Select(log => new
            {
                log.Id,
                log.Action,
                log.EntityType,
                log.EntityId,
                log.Timestamp,
                log.IpAddress,
                log.UserAgent,
                log.Level,
                log.UserId,
                log.NewValues // Contains the security event details
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get security events");
            return StatusCode(500, new { error = "Failed to retrieve security events" });
        }
    }

    [HttpPost("cleanup")]
    [Authorize(Roles = "Admin")] // Only admins can cleanup logs
    public async Task<IActionResult> CleanupOldLogs([FromBody] CleanupRequest request)
    {
        try
        {
            await _auditService.CleanupOldLogsAsync(request.RetentionDays);

            _logger.LogInformation("Audit log cleanup initiated with {RetentionDays} days retention", request.RetentionDays);
            return Ok(new { success = true, message = $"Cleanup initiated for logs older than {request.RetentionDays} days" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup audit logs");
            return StatusCode(500, new { error = "Failed to cleanup audit logs" });
        }
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetAuditStats([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            var auditLogs = await _auditService.GetUserAuditTrailAsync(userGuid, startDate, endDate);

            var stats = new
            {
                TotalLogs = auditLogs.Count(),
                LogsByLevel = auditLogs.GroupBy(log => log.Level)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                LogsByAction = auditLogs.GroupBy(log => log.Action)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .ToDictionary(g => g.Key, g => g.Count()),
                LogsByDay = auditLogs.GroupBy(log => log.Timestamp.Date)
                    .OrderBy(g => g.Key)
                    .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g => g.Count()),
                MostRecentActivity = auditLogs.OrderByDescending(log => log.Timestamp)
                    .Take(5)
                    .Select(log => new
                    {
                        log.Action,
                        log.EntityType,
                        log.Timestamp
                    })
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit stats");
            return StatusCode(500, new { error = "Failed to retrieve audit statistics" });
        }
    }
}

public class CleanupRequest
{
    public int RetentionDays { get; set; } = 365;
}
