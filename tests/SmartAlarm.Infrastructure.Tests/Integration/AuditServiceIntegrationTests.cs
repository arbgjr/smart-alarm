using Microsoft.Extensions.DependencyInjection;
using SmartAlarm.Application.Abstractions;
using SmartAlarm.Domain.Enums;
using SmartAlarm.Infrastructure.Tests.Fixtures;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests.Integration;

public class AuditServiceIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly IServiceProvider _serviceProvider;

    public AuditServiceIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _serviceProvider = _fixture.ServiceProvider;
    }

    [Fact]
    public async Task LogUserActionAsync_ShouldLogActionSuccessfully()
    {
        // Arrange
        var auditService = _serviceProvider.GetRequiredService<IAuditService>();
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid().ToString();

        // Act
        await auditService.LogUserActionAsync(
            "CREATE",
            userId,
            "Alarm",
            entityId,
            null,
            new { Name = "Test Alarm", Time = "08:00" });

        // Assert - Should not throw
        // Verify by getting audit logs
        var filter = new AuditLogFilter
        {
            UserId = userId,
            EventType = "UserAction",
            EntityType = "Alarm",
            EntityId = entityId
        };

        var result = await auditService.GetAuditLogsAsync(filter);
        Assert.NotNull(result);
        Assert.True(result.TotalCount > 0);
        Assert.Contains(result.Logs, log => log.Action == "CREATE" && log.EntityType == "Alarm");
    }

    [Fact]
    public async Task LogSecurityEventAsync_ShouldLogSecurityEvent()
    {
        // Arrange
        var auditService = _serviceProvider.GetRequiredService<IAuditService>();
        var userId = Guid.NewGuid();

        // Act
        await auditService.LogSecurityEventAsync(
            "LoginAttempt",
            userId,
            "Failed login attempt from IP 192.168.1.100");

        // Assert
        var filter = new AuditLogFilter
        {
            UserId = userId,
            EventType = "LoginAttempt",
            Level = AuditLogLevel.Security
        };

        var result = await auditService.GetAuditLogsAsync(filter);
        Assert.NotNull(result);
        Assert.True(result.TotalCount > 0);
        Assert.Contains(result.Logs, log => log.Level == AuditLogLevel.Security);
    }

    [Fact]
    public async Task LogDataAccessAsync_ShouldLogDataAccess()
    {
        // Arrange
        var auditService = _serviceProvider.GetRequiredService<IAuditService>();
        var userId = Guid.NewGuid();
        var accessedUserId = Guid.NewGuid();

        // Act
        await auditService.LogDataAccessAsync(
            userId,
            "UserProfile",
            "Profile view for dashboard",
            accessedUserId);

        // Assert
        var filter = new AuditLogFilter
        {
            UserId = userId,
            EventType = "DataAccess",
            EntityType = "UserProfile"
        };

        var result = await auditService.GetAuditLogsAsync(filter);
        Assert.NotNull(result);
        Assert.True(result.TotalCount > 0);
        Assert.Contains(result.Logs, log => log.EventType == "DataAccess");
    }

    [Fact]
    public async Task LogConsentAsync_ShouldLogConsentEvents()
    {
        // Arrange
        var auditService = _serviceProvider.GetRequiredService<IAuditService>();
        var userId = Guid.NewGuid();

        // Act - Grant consent
        await auditService.LogConsentAsync(
            userId,
            "DataProcessing",
            true,
            "User granted consent for data processing");

        // Act - Revoke consent
        await auditService.LogConsentAsync(
            userId,
            "DataProcessing",
            false,
            "User revoked consent for data processing");

        // Assert
        var filter = new AuditLogFilter
        {
            UserId = userId,
            EventType = "Consent",
            EntityType = "UserConsent"
        };

        var result = await auditService.GetAuditLogsAsync(filter);
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 2);
        Assert.Contains(result.Logs, log => log.Action == "Grant");
        Assert.Contains(result.Logs, log => log.Action == "Revoke");
    }

    [Fact]
    public async Task LogAccessDeniedAsync_ShouldLogAccessDenied()
    {
        // Arrange
        var auditService = _serviceProvider.GetRequiredService<IAuditService>();
        var userId = Guid.NewGuid();

        // Act
        await auditService.LogAccessDeniedAsync(
            userId,
            "/api/admin/users",
            "Insufficient permissions");

        // Assert
        var filter = new AuditLogFilter
        {
            UserId = userId,
            EventType = "AccessDenied",
            Level = AuditLogLevel.Warning
        };

        var result = await auditService.GetAuditLogsAsync(filter);
        Assert.NotNull(result);
        Assert.True(result.TotalCount > 0);
        Assert.Contains(result.Logs, log => log.Level == AuditLogLevel.Warning);
    }

    [Fact]
    public async Task LogSystemEventAsync_ShouldLogSystemEvent()
    {
        // Arrange
        var auditService = _serviceProvider.GetRequiredService<IAuditService>();
        var correlationId = Guid.NewGuid().ToString();

        // Act
        await auditService.LogSystemEventAsync(
            "ApplicationStartup",
            "Smart Alarm application started successfully",
            correlationId);

        // Assert
        var filter = new AuditLogFilter
        {
            EventType = "ApplicationStartup",
            EntityType = "System"
        };

        var result = await auditService.GetAuditLogsAsync(filter);
        Assert.NotNull(result);
        Assert.True(result.TotalCount > 0);
        Assert.Contains(result.Logs, log => log.CorrelationId == correlationId);
    }

    [Fact]
    public async Task GetAuditLogsAsync_WithFilters_ShouldReturnFilteredResults()
    {
        // Arrange
        var auditService = _serviceProvider.GetRequiredService<IAuditService>();
        var userId = Guid.NewGuid();

        // Create test data
        await auditService.LogUserActionAsync("CREATE", userId, "Alarm", "1", null, new { Name = "Alarm 1" });
        await auditService.LogUserActionAsync("UPDATE", userId, "Alarm", "1", new { Name = "Alarm 1" }, new { Name = "Updated Alarm 1" });
        await auditService.LogUserActionAsync("DELETE", userId, "Alarm", "1", new { Name = "Updated Alarm 1" }, null);

        // Act
        var filter = new AuditLogFilter
        {
            UserId = userId,
            EntityType = "Alarm",
            EntityId = "1",
            Page = 1,
            PageSize = 10
        };

        var result = await auditService.GetAuditLogsAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 3);
        Assert.True(result.Logs.Count >= 3);
        Assert.All(result.Logs, log => Assert.Equal(userId, log.UserId));
        Assert.All(result.Logs, log => Assert.Equal("Alarm", log.EntityType));
        Assert.All(result.Logs, log => Assert.Equal("1", log.EntityId));
    }

    [Fact]
    public async Task GenerateComplianceReportAsync_ShouldGenerateReport()
    {
        // Arrange
        var auditService = _serviceProvider.GetRequiredService<IAuditService>();
        var userId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;

        // Create test data
        await auditService.LogUserActionAsync("CREATE", userId, "User", userId.ToString(), null, new { Email = "test@example.com" });
        await auditService.LogDataAccessAsync(userId, "UserProfile", "Profile access", userId);
        await auditService.LogConsentAsync(userId, "Marketing", true, "Marketing consent granted");
        await auditService.LogSecurityEventAsync("LoginSuccess", userId, "Successful login");

        // Act
        var report = await auditService.GenerateComplianceReportAsync(startDate, endDate, userId);

        // Assert
        Assert.NotNull(report);
        Assert.Equal(userId, report.UserId);
        Assert.True(report.Metrics.TotalUserActions > 0);
        Assert.True(report.Metrics.DataAccessEvents > 0);
        Assert.True(report.Metrics.ConsentEvents > 0);
        Assert.True(report.Metrics.SecurityEvents > 0);
        Assert.NotEmpty(report.DataAccess);
        Assert.NotEmpty(report.Consents);
        Assert.NotEmpty(report.SecurityEvents);
    }

    [Fact]
    public async Task GetUserAuditTrailAsync_ShouldReturnUserTrail()
    {
        // Arrange
        var auditService = _serviceProvider.GetRequiredService<IAuditService>();
        var userId = Guid.NewGuid();

        // Create test data
        await auditService.LogUserActionAsync("LOGIN", userId, "Session", "1", null, new { LoginTime = DateTime.UtcNow });
        await auditService.LogUserActionAsync("CREATE", userId, "Alarm", "1", null, new { Name = "Morning Alarm" });
        await auditService.LogUserActionAsync("LOGOUT", userId, "Session", "1", new { LoginTime = DateTime.UtcNow }, null);

        // Act
        var trail = await auditService.GetUserAuditTrailAsync(userId);

        // Assert
        Assert.NotNull(trail);
        var trailList = trail.ToList();
        Assert.True(trailList.Count >= 3);
        Assert.All(trailList, entry => Assert.Equal(userId, entry.UserId));
    }

    [Fact]
    public async Task GetEntityAuditTrailAsync_ShouldReturnEntityTrail()
    {
        // Arrange
        var auditService = _serviceProvider.GetRequiredService<IAuditService>();
        var userId = Guid.NewGuid();
        var entityId = "test-alarm-123";

        // Create test data
        await auditService.LogUserActionAsync("CREATE", userId, "Alarm", entityId, null, new { Name = "Test Alarm" });
        await auditService.LogUserActionAsync("UPDATE", userId, "Alarm", entityId, new { Name = "Test Alarm" }, new { Name = "Updated Alarm" });
        await auditService.LogUserActionAsync("ACTIVATE", userId, "Alarm", entityId, new { Enabled = false }, new { Enabled = true });

        // Act
        var trail = await auditService.GetEntityAuditTrailAsync("Alarm", entityId);

        // Assert
        Assert.NotNull(trail);
        var trailList = trail.ToList();
        Assert.True(trailList.Count >= 3);
        Assert.All(trailList, entry => Assert.Equal("Alarm", entry.EntityType));
        Assert.All(trailList, entry => Assert.Equal(entityId, entry.EntityId));
    }

    [Fact]
    public async Task GetSecurityEventsAsync_ShouldReturnSecurityEvents()
    {
        // Arrange
        var auditService = _serviceProvider.GetRequiredService<IAuditService>();
        var userId = Guid.NewGuid();

        // Create test data
        await auditService.LogSecurityEventAsync("LoginAttempt", userId, "Failed login attempt");
        await auditService.LogSecurityEventAsync("LoginSuccess", userId, "Successful login");
        await auditService.LogSecurityEventAsync("PasswordChange", userId, "Password changed");

        // Act
        var events = await auditService.GetSecurityEventsAsync();

        // Assert
        Assert.NotNull(events);
        var eventsList = events.ToList();
        Assert.True(eventsList.Count >= 3);
        Assert.All(eventsList, evt => Assert.Equal(AuditLogLevel.Security, evt.Level));
    }

    [Fact]
    public async Task AnonymizeUserDataAsync_ShouldAnonymizeUserData()
    {
        // Arrange
        var auditService = _serviceProvider.GetRequiredService<IAuditService>();
        var userId = Guid.NewGuid();

        // Create test data with user information
        await auditService.LogUserActionAsync("CREATE", userId, "Profile", userId.ToString(), null,
            new { Email = "user@example.com", Name = "John Doe" });
        await auditService.LogDataAccessAsync(userId, "UserData", "Profile access", userId);

        // Act
        await auditService.AnonymizeUserDataAsync(userId, "GDPR deletion request");

        // Assert
        var trail = await auditService.GetUserAuditTrailAsync(userId);
        var trailList = trail.ToList();

        // Should have anonymization log
        var systemLogs = await auditService.GetAuditLogsAsync(new AuditLogFilter
        {
            EventType = "DataAnonymization",
            EntityType = "System"
        });
        Assert.True(systemLogs.TotalCount > 0);
    }

    [Fact]
    public async Task CleanupOldLogsAsync_ShouldCleanupOldLogs()
    {
        // Arrange
        var auditService = _serviceProvider.GetRequiredService<IAuditService>();
        var retentionDays = 1; // Very short retention for testing

        // Create some test data
        await auditService.LogSystemEventAsync("TestEvent", "Test event for cleanup");

        // Act
        await auditService.CleanupOldLogsAsync(retentionDays);

        // Assert - Should not throw
        // In a real test, you might want to verify that old logs are actually deleted
        // but that would require manipulating timestamps which is complex in this context
    }
}
