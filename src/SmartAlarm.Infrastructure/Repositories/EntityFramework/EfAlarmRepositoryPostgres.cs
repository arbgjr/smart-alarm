using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;

namespace SmartAlarm.Infrastructure.Repositories.EntityFramework
{
    /// <summary>
    /// Implementação específica de IAlarmRepository para PostgreSQL.
    /// Herda de EfAlarmRepository, podendo customizar queries/mapeamentos se necessário.
    /// </summary>
    public class EfAlarmRepositoryPostgres : EfAlarmRepository
    {
        public EfAlarmRepositoryPostgres(
            SmartAlarmDbContext context,
            ILogger<EfAlarmRepository> logger,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            SmartAlarmActivitySource activitySource) : base(context, logger, meter, correlationContext, activitySource) 
        { 
            // Customizações específicas para PostgreSQL podem ser adicionadas aqui.
        }
    }
}
