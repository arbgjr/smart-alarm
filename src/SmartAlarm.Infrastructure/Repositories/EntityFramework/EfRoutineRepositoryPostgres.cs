using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Infrastructure.Data;

namespace SmartAlarm.Infrastructure.Repositories.EntityFramework
{
    /// <summary>
    /// Implementação específica de IRoutineRepository para PostgreSQL.
    /// Herda de EfRoutineRepository, podendo customizar queries/mapeamentos se necessário.
    /// </summary>
    public class EfRoutineRepositoryPostgres : EfRoutineRepository
    {
        public EfRoutineRepositoryPostgres(SmartAlarmDbContext context) : base(context) { }
        // Customizações específicas para PostgreSQL podem ser adicionadas aqui.
    }
}
