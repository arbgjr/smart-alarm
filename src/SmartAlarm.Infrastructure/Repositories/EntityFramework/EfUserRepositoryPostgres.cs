using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
    /// Implementação específica de IUserRepository para PostgreSQL.
    /// Herda de EfUserRepository, podendo customizar queries/mapeamentos se necessário.
    /// </summary>
    public class EfUserRepositoryPostgres : EfUserRepository
    {
        public EfUserRepositoryPostgres(
            SmartAlarmDbContext context,
            ILogger<EfUserRepository> logger,
            SmartAlarmMeter meter,
            ICorrelationContext correlationContext,
            SmartAlarmActivitySource activitySource) : base(context, logger, meter, correlationContext, activitySource)
        {
        }

        // Adicione aqui customizações específicas para PostgreSQL, se necessário.
    }
}
