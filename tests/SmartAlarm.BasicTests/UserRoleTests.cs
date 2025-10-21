using Microsoft.EntityFrameworkCore;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Infrastructure.Data;
using Xunit;

namespace SmartAlarm.BasicTests;

public class UserRoleTests
{
    [Fact]
    public async Task UserRole_CompositePrimaryKey_ShouldWork()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SmartAlarmDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new SmartAlarmDbContext(options);

        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var user = new User(userId, "Test User", "test@example.com");
        var role = new Role { Id = roleId, Name = "TestRole" };
        var userRole = new UserRole(userId, roleId);

        // Act
        context.Users.Add(user);
        context.Roles.Add(role);
        context.UserRoles.Add(userRole);

        var result = await context.SaveChangesAsync();

        // Assert
        Assert.True(result > 0);

        var savedUserRole = await context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

        Assert.NotNull(savedUserRole);
        Assert.Equal(userId, savedUserRole.UserId);
        Assert.Equal(roleId, savedUserRole.RoleId);
        Assert.True(savedUserRole.IsActive);
    }

    [Fact]
    public async Task UserRole_CanHaveMultipleRolesPerUser()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SmartAlarmDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new SmartAlarmDbContext(options);

        var userId = Guid.NewGuid();
        var role1Id = Guid.NewGuid();
        var role2Id = Guid.NewGuid();

        var user = new User(userId, "Test User", "test@example.com");
        var role1 = new Role { Id = role1Id, Name = "Admin" };
        var role2 = new Role { Id = role2Id, Name = "User" };

        var userRole1 = new UserRole(userId, role1Id);
        var userRole2 = new UserRole(userId, role2Id); // Same user, different role

        // Act
        context.Users.Add(user);
        context.Roles.AddRange(role1, role2);
        context.UserRoles.AddRange(userRole1, userRole2);

        var result = await context.SaveChangesAsync();

        // Assert
        Assert.True(result > 0);

        var userRoles = await context.UserRoles
            .Where(ur => ur.UserId == userId)
            .ToListAsync();

        Assert.Equal(2, userRoles.Count);
        Assert.Contains(userRoles, ur => ur.RoleId == role1Id);
        Assert.Contains(userRoles, ur => ur.RoleId == role2Id);
    }

    [Fact]
    public async Task UserRole_Methods_ShouldWork()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SmartAlarmDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new SmartAlarmDbContext(options);

        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var user = new User(userId, "Test User", "test@example.com");
        var role = new Role { Id = roleId, Name = "TestRole" };
        var userRole = new UserRole(userId, roleId);

        context.Users.Add(user);
        context.Roles.Add(role);
        context.UserRoles.Add(userRole);
        await context.SaveChangesAsync();

        // Act & Assert
        Assert.True(userRole.IsValid());

        userRole.Deactivate();
        Assert.False(userRole.IsActive);
        Assert.False(userRole.IsValid());

        userRole.Reactivate();
        Assert.True(userRole.IsActive);
        Assert.True(userRole.IsValid());

        // Test expiration
        var futureDate = DateTime.UtcNow.AddDays(1);
        userRole.SetExpiration(futureDate);
        Assert.Equal(futureDate, userRole.ExpiresAt);
        Assert.True(userRole.IsValid());

        // Test that setting past expiration throws exception
        var pastDate = DateTime.UtcNow.AddDays(-1);
        Assert.Throws<ArgumentException>(() => userRole.SetExpiration(pastDate));
    }
}
