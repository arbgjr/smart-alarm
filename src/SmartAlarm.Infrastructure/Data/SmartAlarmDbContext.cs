using Microsoft.EntityFrameworkCore;
using SmartAlarm.Domain.Entities;

namespace SmartAlarm.Infrastructure.Data
{
    /// <summary>
    /// Entity Framework DbContext for Smart Alarm application.
    /// Provides database access for all entities following Clean Architecture.
    /// </summary>
    public class SmartAlarmDbContext : DbContext
    {
        public SmartAlarmDbContext(DbContextOptions<SmartAlarmDbContext> options) : base(options)
        {
        }

        public DbSet<Alarm> Alarms { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<Routine> Routines { get; set; }
        public DbSet<Integration> Integrations { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Holiday> Holidays { get; set; }
        public DbSet<ExceptionPeriod> ExceptionPeriods { get; set; }
        public DbSet<UserHolidayPreference> UserHolidayPreferences { get; set; }
        public DbSet<AlarmEvent> AlarmEvents { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<UserConsent> UserConsents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all entity configurations
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(SmartAlarmDbContext).Assembly);

            // Ensure UserRole has composite primary key (explicit configuration for tests)
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(ur => new { ur.UserId, ur.RoleId });

                entity.Property(ur => ur.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(ur => ur.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true);

                entity.Property(ur => ur.ExpiresAt)
                    .IsRequired(false);

                entity.HasOne(ur => ur.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(ur => ur.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ur => ur.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.ToTable("UserRoles");
            });
        }
    }
}
