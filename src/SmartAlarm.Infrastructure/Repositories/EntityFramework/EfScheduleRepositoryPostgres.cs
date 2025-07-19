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
    /// Implementação específica de IScheduleRepository para PostgreSQL.
    /// Herda de EfScheduleRepository, podendo customizar queries/mapeamentos se necessário.
    /// </summary>
    public class EfScheduleRepositoryPostgres : EfScheduleRepository
    {
        public EfScheduleRepositoryPostgres(
            SmartAlarmDbContext context,
            ILogger<EfScheduleRepository> logger,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            SmartAlarmActivitySource activitySource) 
            : base(context, logger, meter, correlationContext, activitySource) 
        { 
        }
        
        // Customizações específicas para PostgreSQL podem ser adicionadas aqui.
    }
}
