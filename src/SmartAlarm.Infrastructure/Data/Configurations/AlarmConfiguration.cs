using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Entity Framework configuration for Alarm entity.
    /// </summary>
    public class AlarmConfiguration : IEntityTypeConfiguration<Alarm>
    {
        public void Configure(EntityTypeBuilder<Alarm> builder)
        {
            builder.ToTable("Alarms");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.Id)
                .IsRequired()
                .HasColumnName("Id");

            // Configure Name value object
            builder.Property(a => a.Name)
                .HasConversion(
                    name => name.Value,
                    value => new Domain.ValueObjects.Name(value))
                .HasColumnName("Name")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(a => a.Time)
                .IsRequired()
                .HasColumnName("Time");

            builder.Property(a => a.Enabled)
                .IsRequired()
                .HasColumnName("Enabled");

            builder.Property(a => a.UserId)
                .IsRequired()
                .HasColumnName("UserId");

            builder.Property(a => a.CreatedAt)
                .IsRequired()
                .HasColumnName("CreatedAt");

            builder.Property(a => a.LastTriggeredAt)
                .HasColumnName("LastTriggeredAt");

            // Configure relationships
            builder.HasMany(a => a.Routines)
                .WithOne()
                .HasForeignKey("AlarmId")
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(a => a.Integrations)
                .WithOne()
                .HasForeignKey("AlarmId")
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(a => a.Schedules)
                .WithOne()
                .HasForeignKey("AlarmId")
                .OnDelete(DeleteBehavior.Cascade);

            // Index for UserId
            builder.HasIndex(a => a.UserId)
                .HasDatabaseName("IX_Alarms_UserId");
        }
    }
}