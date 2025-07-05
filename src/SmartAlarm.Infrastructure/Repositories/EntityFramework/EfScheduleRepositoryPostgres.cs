using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Infrastructure.Data;

namespace SmartAlarm.Infrastructure.Repositories.EntityFramework
{
    /// <summary>
    /// Implementação específica de IScheduleRepository para PostgreSQL.
    /// Herda de EfScheduleRepository, podendo customizar queries/mapeamentos se necessário.
    /// </summary>
    public class EfScheduleRepositoryPostgres : EfScheduleRepository
    {
        public EfScheduleRepositoryPostgres(SmartAlarmDbContext context) : base(context) { }
        // Customizações específicas para PostgreSQL podem ser adicionadas aqui.
    }
}
