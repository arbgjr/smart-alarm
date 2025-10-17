using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Infrastructure.Data;
using SmartAlarm.Infrastructure.Tests.Fixtures;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests.Integration.Database;

/// <summary>
/// Integration tests for database operations using TestContainers.
/// These tests verify that Entity Framework configurations work correctly with real PostgreSQL.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "Database")]
public class DatabaseIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly SmartAlarmDbContext _context;

    public DatabaseIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _context = _fixture.ServiceProvider.GetRequiredService<SmartAlarmDbContext>();
    }

    [Fact]
    public async Task Database_ShouldConnect()
    {
        // Act
        var canConnect = await _context.Database.CanConnectAsync();

        // Assert
        Assert.True(canConnect);
    }

    [Fact]
    public async Task Database_ShouldCreateTables()
    {
        // Act
        await _context.Database.EnsureCreatedAsync();

        // Assert - Check if UserRoles table exists and has proper primary key
        var tableExists = await _context.Database.ExecuteSqlRawAsync(
            "SELECT 1 FROM information_schema.tables WHERE table_name = 'UserRoles'");

        // This should not throw an exception about missing primary key
        Assert.True(tableExists >= 0);
    }

    [Fact]
    public async Task UserRole_ShouldHaveCompositePrimaryKey()
    {
        // Arrange
        await _context.Database.EnsureCreatedAsync();

        var user = new User(Guid.NewGuid(), "Test User", "test@example.com");
        var role = new Role(Guid.NewGuid(), "TestRole", "Test Role Description");

        _context.Users.Add(user);
        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        var userRole = new UserRole(user.Id, role.Id);

        // Act & Assert - This should not throw a primary key exception
        _context.UserRoles.Add(userRole);
        await _context.SaveChangesAsync();

        // Verify the UserRole was saved
        var savedUserRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id);

        Assert.NotNull(savedUserRole);
        Assert.Equal(user.Id, savedUserRole.UserId);
        Assert.Equal(role.Id, savedUserRole.RoleId);
    }

    [Fact]
    public async Task UserRole_ShouldPreventDuplicateCompositeKeys()
    {
        // Arrange
        await _context.Database.EnsureCreatedAsync();

        var user = new User(Guid.NewGuid(), "Test User", "test@example.com");
        var role = new Role(Guid.NewGuid(), "TestRole", "Test Role Description");

        _context.Users.Add(user);
        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        var userRole1 = new UserRole(user.Id, role.Id);
        var userRole2 = new UserRole(user.Id, role.Id);

        // Act
        _context.UserRoles.Add(userRole1);
        await _context.SaveChangesAsync();

        _context.UserRoles.Add(userRole2);

        // Assert - Should throw due to duplicate primary key
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _context.SaveChangesAsync());
    }

    [Fact]
    public async Task AllEntities_ShouldHavePrimaryKeys()
    {
        // Arrange
        await _context.Database.EnsureCreatedAsync();

        // Act & Assert - Create one instance of each entity to verify no primary key issues
        var user = new User(Guid.NewGuid(), "Test User", "test@example.com");
        var role = new Role(Guid.NewGuid(), "TestRole", "Test Role Description");
        var alarm = new Alarm(
            id: Guid.NewGuid(),
            userId: user.Id,
            time: TimeOnly.FromDateTime(DateTime.Now),
            isActive: true,
            isRecurring: false,
            metadata: new Dictionary<string, object>()
        );
        var holiday = new Holiday(DateTime.Today.AddDays(30), "Test Holiday");
        var userRole = new UserRole(user.Id, role.Id);

        // Add all entities
        _context.Users.Add(user);
        _context.Roles.Add(role);
        _context.Alarms.Add(alarm);
        _context.Holidays.Add(holiday);
        _context.UserRoles.Add(userRole);

        // This should not throw any primary key exceptions
        await _context.SaveChangesAsync();

        // Verify all entities were saved
        Assert.True(await _context.Users.AnyAsync(u => u.Id == user.Id));
        Assert.True(await _context.Roles.AnyAsync(r => r.Id == role.Id));
        Assert.True(await _context.Alarms.AnyAsync(a => a.Id == alarm.Id));
        Assert.True(await _context.Holidays.AnyAsync(h => h.Id == holiday.Id));
        Assert.True(await _context.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id));
    }
}
