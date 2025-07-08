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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all entity configurations
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(SmartAlarmDbContext).Assembly);
        }
    }
}