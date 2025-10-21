using Microsoft.EntityFrameworkCore;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Infrastructure.Data;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests;

/// <summary>
/// Simple test to verify UserRole primary key configuration works
/// </summary>
public class SimpleUserRoleTest
{
    [Fact]
    public async Task UserRole_WithCompositePrimaryKey_ShouldWork()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SmartAlarmDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new SmartAlarmDbContext(options);

        // Create test entities
        var user = new User(
            id: Guid.NewGuid(),
            name: "Test User",
            email: "test@example.com"
        );

        var role = new Role(Guid.NewGuid(), "TestRole", "Test Role Description");

        var userRole = new UserRole(user.Id, role.Id);

        // Act
        context.Users.Add(user);
        context.Roles.Add(role);
        context.UserRoles.Add(userRole);

        // This should not throw any primary key exceptions
        var result = await context.SaveChangesAsync();

        // Assert
        Assert.True(result > 0);
        Assert.True(await context.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id));
    }

    [Fact]
    public async Task UserRole_DuplicateCompositePrimaryKey_ShouldThrow()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SmartAlarmDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new SmartAlarmDbContext(options);

        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var user = new User(userId, "Test User", "test@example.com");
        var role = new Role(roleId, "TestRole");

        var userRole1 = new UserRole(userId, roleId);
        var userRole2 = new UserRole(userId, roleId); // Same composite key

        context.Users.Add(user);
        context.Roles.Add(role);
        context.UserRoles.Add(userRole1);
        await context.SaveChangesAsync();

        // Act & Assert
        context.UserRoles.Add(userRole2);
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }
}
