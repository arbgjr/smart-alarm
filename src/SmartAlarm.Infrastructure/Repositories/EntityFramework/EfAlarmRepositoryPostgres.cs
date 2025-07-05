using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Infrastructure.Data;

namespace SmartAlarm.Infrastructure.Repositories.EntityFramework
{
    /// <summary>
    /// Implementação específica de IAlarmRepository para PostgreSQL.
    /// Herda de EfAlarmRepository, podendo customizar queries/mapeamentos se necessário.
    /// </summary>
    public class EfAlarmRepositoryPostgres : EfAlarmRepository
    {
        public EfAlarmRepositoryPostgres(SmartAlarmDbContext context) : base(context) { }
        // Customizações específicas para PostgreSQL podem ser adicionadas aqui.
    }
}
