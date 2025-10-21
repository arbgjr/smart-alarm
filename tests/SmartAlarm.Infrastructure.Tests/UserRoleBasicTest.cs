using Microsoft.EntityFrameworkCore;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Infrastructure.Data;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests;

/// <summary>
/// Teste básico para verificar se a configuração do UserRole está funcionando
/// </summary>
public class UserRoleBasicTest
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
        var role = new Role(roleId, "TestRole");
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
    }
}
