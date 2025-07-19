using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Entity Framework configuration for Routine entity.
    /// </summary>
    public class RoutineConfiguration : IEntityTypeConfiguration<Routine>
    {
        public void Configure(EntityTypeBuilder<Routine> builder)
        {
            builder.ToTable("Routines");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Id)
                .IsRequired()
                .HasColumnName("Id");

            // Configure Name value object
            builder.Property(r => r.Name)
                .HasConversion(
                    name => name != null ? name.Value : string.Empty,
                    value => new Domain.ValueObjects.Name(value))
                .HasColumnName("Name")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(r => r.AlarmId)
                .IsRequired()
                .HasColumnName("AlarmId");

            builder.Property(r => r.IsActive)
                .IsRequired()
                .HasColumnName("IsActive");

            builder.Property(r => r.CreatedAt)
                .IsRequired()
                .HasColumnName("CreatedAt");

            // Configure Actions as JSON
            builder.Property(r => r.Actions)
                .HasConversion(
                    actions => System.Text.Json.JsonSerializer.Serialize(actions ?? new List<string>(), System.Text.Json.JsonSerializerOptions.Default),
                    json => string.IsNullOrWhiteSpace(json)
                        ? new List<string>()
                        : System.Text.Json.JsonSerializer.Deserialize<List<string>>(json, System.Text.Json.JsonSerializerOptions.Default) ?? new List<string>())
                .HasColumnName("Actions")
                .HasColumnType("text");

            // Index for AlarmId
            builder.HasIndex(r => r.AlarmId)
                .HasDatabaseName("IX_Routines_AlarmId");
        }
    }
}