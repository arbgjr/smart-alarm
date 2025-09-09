using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Entity Framework configuration for User entity.
    /// </summary>
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Id)
                .IsRequired()
                .HasColumnName("Id");

            // Configure Name value object
            builder.Property(u => u.Name)
                .HasConversion(
                    name => name.Value,
                    value => new Domain.ValueObjects.Name(value))
                .HasColumnName("Name")
                .HasMaxLength(255)
                .IsRequired();

            // Configure Email value object
            builder.Property(u => u.Email)
                .HasConversion(
                    email => email.Address,
                    value => new Domain.ValueObjects.Email(value))
                .HasColumnName("Email")
                .HasMaxLength(320)
                .IsRequired();

            builder.Property(u => u.IsActive)
                .IsRequired()
                .HasColumnName("IsActive");

            builder.Property(u => u.CreatedAt)
                .IsRequired()
                .HasColumnName("CreatedAt");

            builder.Property(u => u.LastLoginAt)
                .HasColumnName("LastLoginAt");

            builder.Property(u => u.PasswordHash)
                .HasColumnName("PasswordHash")
                .HasMaxLength(500)
                .IsRequired(false); // OAuth users might not have password

            builder.Property(u => u.EmailVerified)
                .IsRequired()
                .HasColumnName("EmailVerified");

            builder.Property(u => u.UpdatedAt)
                .HasColumnName("UpdatedAt");

            // OAuth2 properties
            builder.Property(u => u.ExternalProvider)
                .HasColumnName("ExternalProvider")
                .HasMaxLength(50)
                .IsRequired(false);

            builder.Property(u => u.ExternalProviderId)
                .HasColumnName("ExternalProviderId")
                .HasMaxLength(255)
                .IsRequired(false);

            // Relationships
            builder.HasMany(u => u.HolidayPreferences)
                .WithOne(uhp => uhp.User)
                .HasForeignKey(uhp => uhp.UserId);

            // Index for unique email
            builder.HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("IX_Users_Email");

            // Index for external provider (for efficient OAuth lookups)
            builder.HasIndex(u => new { u.ExternalProvider, u.ExternalProviderId })
                .HasDatabaseName("IX_Users_ExternalProvider");
        }
    }
}