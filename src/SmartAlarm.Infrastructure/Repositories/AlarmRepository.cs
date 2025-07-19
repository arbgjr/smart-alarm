using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;

namespace SmartAlarm.Infrastructure.Repositories
{
    /// <summary>
    /// Oracle Autonomous DB implementation of IAlarmRepository.
    /// </summary>
    public class AlarmRepository : IAlarmRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<AlarmRepository> _logger;

        public AlarmRepository(string connectionString, ILogger<AlarmRepository> logger)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Alarm?> GetByIdAsync(Guid id)
        {
            const string sql = @"SELECT * FROM Alarms WHERE Id = :Id";
            try
            {
                using var connection = new OracleConnection(_connectionString);
                var alarm = await connection.QuerySingleOrDefaultAsync<Alarm>(sql, new { Id = id });
                return alarm;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching alarm by Id: {AlarmId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Alarm>> GetByUserIdAsync(Guid userId)
        {
            const string sql = @"SELECT * FROM Alarms WHERE UserId = :UserId";
            try
            {
                using var connection = new OracleConnection(_connectionString);
                var alarms = await connection.QueryAsync<Alarm>(sql, new { UserId = userId });
                return alarms;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching alarms by UserId: {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<Alarm>> GetAllEnabledAsync()
        {
            const string sql = @"SELECT * FROM Alarms WHERE Enabled = 1";
            try
            {
                using var connection = new OracleConnection(_connectionString);
                var alarms = await connection.QueryAsync<Alarm>(sql);
                _logger.LogDebug("Retrieved {Count} enabled alarms", alarms.Count());
                return alarms;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching enabled alarms");
                throw;
            }
        }

        public async Task<IEnumerable<Alarm>> GetDueForTriggeringAsync(DateTime now)
        {
            // Query otimizada para buscar apenas alarmes que devem ser disparados agora
            // Considera horário atual com margem de 1 minuto para acomodar processamento
            const string sql = @"
                SELECT a.* FROM Alarms a
                INNER JOIN Schedules s ON a.Id = s.AlarmId
                WHERE a.Enabled = 1 
                AND s.IsActive = 1
                AND EXTRACT(HOUR FROM s.Time) = :Hour
                AND EXTRACT(MINUTE FROM s.Time) = :Minute
                AND (
                    (s.DaysOfWeek IS NULL) OR 
                    (s.DaysOfWeek LIKE '%' || :DayOfWeek || '%')
                )";
            
            try
            {
                using var connection = new OracleConnection(_connectionString);
                var parameters = new
                {
                    Hour = now.Hour,
                    Minute = now.Minute,
                    DayOfWeek = ((int)now.DayOfWeek).ToString()
                };
                
                var alarms = await connection.QueryAsync<Alarm>(sql, parameters);
                _logger.LogDebug("Found {Count} alarms due for triggering at {Time}", alarms.Count(), now);
                return alarms;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching alarms due for triggering at {Time}", now);
                throw;
            }
        }

        public async Task AddAsync(Alarm alarm)
        {
            const string sql = @"INSERT INTO Alarms (Id, Name, Time, Enabled, UserId) VALUES (:Id, :Name, :Time, :Enabled, :UserId)";
            try
            {
                using var connection = new OracleConnection(_connectionString);
                await connection.ExecuteAsync(sql, new
                {
                    alarm.Id,
                    alarm.Name,
                    alarm.Time,
                    alarm.Enabled,
                    alarm.UserId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding alarm: {AlarmId}", alarm.Id);
                throw;
            }
        }

        public async Task UpdateAsync(Alarm alarm)
        {
            const string sql = @"UPDATE Alarms SET Name = :Name, Time = :Time, Enabled = :Enabled WHERE Id = :Id";
            try
            {
                using var connection = new OracleConnection(_connectionString);
                await connection.ExecuteAsync(sql, new
                {
                    alarm.Name,
                    alarm.Time,
                    alarm.Enabled,
                    alarm.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating alarm: {AlarmId}", alarm.Id);
                throw;
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            const string sql = @"DELETE FROM Alarms WHERE Id = :Id";
            try
            {
                using var connection = new OracleConnection(_connectionString);
                await connection.ExecuteAsync(sql, new { Id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting alarm: {AlarmId}", id);
                throw;
            }
        }
    }
}

