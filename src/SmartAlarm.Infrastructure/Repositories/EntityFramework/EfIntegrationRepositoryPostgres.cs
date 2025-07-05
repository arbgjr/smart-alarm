using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Infrastructure.Data;

namespace SmartAlarm.Infrastructure.Repositories.EntityFramework
{
    /// <summary>
    /// Implementação específica de IIntegrationRepository para PostgreSQL.
    /// Herda de EfIntegrationRepository, podendo customizar queries/mapeamentos se necessário.
    /// </summary>
    public class EfIntegrationRepositoryPostgres : EfIntegrationRepository
    {
        public EfIntegrationRepositoryPostgres(SmartAlarmDbContext context) : base(context) { }
        // Customizações específicas para PostgreSQL podem ser adicionadas aqui.
    }
}
