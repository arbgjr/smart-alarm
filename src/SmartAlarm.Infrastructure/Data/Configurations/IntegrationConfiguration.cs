using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Entity Framework configuration for Integration entity.
    /// </summary>
    public class IntegrationConfiguration : IEntityTypeConfiguration<Integration>
    {
        public void Configure(EntityTypeBuilder<Integration> builder)
        {
            builder.ToTable("Integrations");

            builder.HasKey(i => i.Id);

            builder.Property(i => i.Id)
                .IsRequired()
                .HasColumnName("Id");

            // Configure Name value object
            builder.Property(i => i.Name)
                .HasConversion(
                    name => name.Value,
                    value => new Domain.ValueObjects.Name(value))
                .HasColumnName("Name")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(i => i.Provider)
                .IsRequired()
                .HasColumnName("Provider")
                .HasMaxLength(100);

            builder.Property(i => i.Configuration)
                .IsRequired()
                .HasColumnName("Configuration")
                .HasColumnType("CLOB");

            builder.Property(i => i.IsActive)
                .IsRequired()
                .HasColumnName("IsActive");

            builder.Property(i => i.CreatedAt)
                .IsRequired()
                .HasColumnName("CreatedAt");

            builder.Property(i => i.LastExecutedAt)
                .HasColumnName("LastExecutedAt");

            builder.Property(i => i.AlarmId)
                .IsRequired()
                .HasColumnName("AlarmId");

            // Index for Provider
            builder.HasIndex(i => i.Provider)
                .HasDatabaseName("IX_Integrations_Provider");

            // Index for AlarmId
            builder.HasIndex(i => i.AlarmId)
                .HasDatabaseName("IX_Integrations_AlarmId");
        }
    }
}