using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Infrastructure.Data;

namespace SmartAlarm.Infrastructure.Repositories.EntityFramework
{
    /// <summary>
    /// Implementação específica de IUserRepository para PostgreSQL.
    /// Herda de EfUserRepository, podendo customizar queries/mapeamentos se necessário.
    /// </summary>
    public class EfUserRepositoryPostgres : EfUserRepository
    {
        public EfUserRepositoryPostgres(SmartAlarmDbContext context) : base(context)
        {
        }

        // Adicione aqui customizações específicas para PostgreSQL, se necessário.
    }
}
