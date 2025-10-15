using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Infrastructure.Data.Configurations;

/// <summary>
/// Configuração Entity Framework para a entidade UserRole.
/// Define relacionamentos Many-to-Many entre User e Role para RBAC.
/// </summary>
public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        // Definir chave composta
        builder.HasKey(ur => new { ur.UserId, ur.RoleId });

        // Configurar propriedades
        builder.Property(ur => ur.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(ur => ur.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(ur => ur.ExpiresAt)
            .IsRequired(false);

        // Configurar relacionamentos
        builder.HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configurar índices
        builder.HasIndex(ur => ur.UserId)
            .HasDatabaseName("IX_UserRoles_UserId");

        builder.HasIndex(ur => ur.RoleId)
            .HasDatabaseName("IX_UserRoles_RoleId");

        builder.HasIndex(ur => ur.IsActive)
            .HasDatabaseName("IX_UserRoles_IsActive");

        builder.HasIndex(ur => new { ur.UserId, ur.IsActive })
            .HasDatabaseName("IX_UserRoles_UserId_IsActive");

        // Configurar tabela
        builder.ToTable("UserRoles");
    }
}