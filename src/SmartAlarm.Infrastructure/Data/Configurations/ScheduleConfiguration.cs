using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Entity Framework configuration for Schedule entity.
    /// </summary>
    public class ScheduleConfiguration : IEntityTypeConfiguration<Schedule>
    {
        public void Configure(EntityTypeBuilder<Schedule> builder)
        {
            builder.ToTable("Schedules");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Id)
                .IsRequired()
                .HasColumnName("Id");

            builder.Property(s => s.Time)
                .IsRequired()
                .HasColumnName("Time");

            builder.Property(s => s.Recurrence)
                .IsRequired()
                .HasColumnName("Recurrence")
                .HasConversion<int>();

            builder.Property(s => s.DaysOfWeek)
                .IsRequired()
                .HasColumnName("DaysOfWeek")
                .HasConversion<int>();

            builder.Property(s => s.IsActive)
                .IsRequired()
                .HasColumnName("IsActive");

            builder.Property(s => s.AlarmId)
                .IsRequired()
                .HasColumnName("AlarmId");

            // Index for AlarmId
            builder.HasIndex(s => s.AlarmId)
                .HasDatabaseName("IX_Schedules_AlarmId");

            // Index for active schedules
            builder.HasIndex(s => s.IsActive)
                .HasDatabaseName("IX_Schedules_IsActive");
        }
    }
}