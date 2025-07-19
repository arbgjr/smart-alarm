using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SmartAlarm.Infrastructure.Data
{
    /// <summary>
    /// Design-time factory for creating SmartAlarmDbContext during migrations.
    /// </summary>
    public class SmartAlarmDbContextFactory : IDesignTimeDbContextFactory<SmartAlarmDbContext>
    {
        public SmartAlarmDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SmartAlarmDbContext>();

            // Use SQLite for design-time migrations (Oracle syntax will be generated)
            optionsBuilder.UseSqlite("Data Source=smartalarm.db");

            return new SmartAlarmDbContext(optionsBuilder.Options);
        }
    }
}