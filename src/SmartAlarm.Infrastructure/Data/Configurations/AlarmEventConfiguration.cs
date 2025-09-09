using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for AlarmEvent entity.
/// Seguindo padrões estabelecidos no projeto para configurações EF.
/// </summary>
public class AlarmEventConfiguration : IEntityTypeConfiguration<AlarmEvent>
{
    public void Configure(EntityTypeBuilder<AlarmEvent> builder)
    {
        builder.ToTable("AlarmEvents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(e => e.AlarmId)
            .IsRequired();

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.EventType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.Timestamp)
            .IsRequired();

        builder.Property(e => e.Metadata)
            .HasMaxLength(1000);

        builder.Property(e => e.Location)
            .HasMaxLength(200);

        // Índices para consultas otimizadas
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_AlarmEvents_UserId");

        builder.HasIndex(e => e.AlarmId)
            .HasDatabaseName("IX_AlarmEvents_AlarmId");

        builder.HasIndex(e => e.EventType)
            .HasDatabaseName("IX_AlarmEvents_EventType");

        builder.HasIndex(e => e.Timestamp)
            .HasDatabaseName("IX_AlarmEvents_Timestamp");

        // Índice composto para consultas por usuário e período
        builder.HasIndex(e => new { e.UserId, e.Timestamp })
            .HasDatabaseName("IX_AlarmEvents_UserId_Timestamp");

        // Índice composto para análise de eventos por alarme
        builder.HasIndex(e => new { e.AlarmId, e.EventType, e.Timestamp })
            .HasDatabaseName("IX_AlarmEvents_AlarmId_EventType_Timestamp");
    }
}