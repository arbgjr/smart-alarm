using Microsoft.EntityFrameworkCore;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Infrastructure.Data;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests;

public class UserRoleConfigurationTest
{
    [Fact]
    public void UserRole_ShouldHaveCompositePrimaryKey()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SmartAlarmDbContext>()
            .UseInMemoryDatabase(databaseName: "TestUserRoleKey")
            .Options;

        // Act & Assert - This should not throw a primary key exception
        using var context = new SmartAlarmDbContext(options);

        // Create test entities
        var user = new User(
            id: Guid.NewGuid(),
            name: "Test User",
            email: "test@example.com"
        );

        var role = new Role(Guid.NewGuid(), "TestRole", "Test Role Description");

        var userRole = new UserRole(user.Id, role.Id);

        context.Users.Add(user);
        context.Roles.Add(role);
        context.UserRoles.Add(userRole);

        // This should not throw any primary key exceptions
        var result = context.SaveChanges();

        Assert.True(result > 0);
    }
}
