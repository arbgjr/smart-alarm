using Microsoft.Extensions.Logging;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Infrastructure.Data;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;

namespace SmartAlarm.Infrastructure.Repositories.EntityFramework
{
    /// <summary>
    /// Implementação específica de IRoutineRepository para PostgreSQL.
    /// Herda de EfRoutineRepository, podendo customizar queries/mapeamentos se necessário.
    /// </summary>
    public class EfRoutineRepositoryPostgres : EfRoutineRepository
    {
        public EfRoutineRepositoryPostgres(
            SmartAlarmDbContext context,
            ILogger<EfRoutineRepository> logger,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            SmartAlarmActivitySource activitySource) 
            : base(context, logger, meter, correlationContext, activitySource) 
        { 
        }
        
        // Customizações específicas para PostgreSQL podem ser adicionadas aqui.
    }
}
