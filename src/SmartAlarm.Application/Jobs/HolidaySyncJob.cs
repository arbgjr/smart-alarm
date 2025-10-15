using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Commands.Holiday;
using SmartAlarm.Domain.Repositories;

namespace SmartAlarm.Application.Jobs
{
    /// <summary>
    /// Job para sincronização automática de feriados
    /// </summary>
    public class HolidaySyncJob
    {
        private readonly IMediator _mediator;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<HolidaySyncJob> _logger;

        public HolidaySyncJob(
            IMediator mediator,
            IUserRepository userRepository,
            ILogger<HolidaySyncJob> logger)
        {
            _mediator = mediator;
            _userRepository = userRepository;
            _logger = logger;
        }

        /// <summary>
        /// Sincroniza feriados para o ano atual
        /// </summary>
        [JobDisplayName("Sincronizar feriados do ano atual")]
        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
        public async Task SyncCurrentYearHolidays()
        {
            var currentYear = DateTime.Now.Year;
            _logger.LogInformation("Iniciando sincronização automática de feriados para {Year}", currentYear);

            try
            {
                // Sincronizar feriados nacionais do Brasil
                await SyncHolidaysForCountry("BR", currentYear);

                // Sincronizar feriados dos próximos anos também
                await SyncHolidaysForCountry("BR", currentYear + 1);

                _logger.LogInformation("Sincronização de feriados concluída com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante sincronização automática de feriados");
                throw; // Re-throw para que o Hangfire marque como falha
            }
        }

        /// <summary>
        /// Sincroniza feriados específicos por usuário baseado em sua localização
        /// </summary>
        [JobDisplayName("Sincronizar feriados por localização de usuários")]
        [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 120, 600 })]
        public async Task SyncUserLocationHolidays()
        {
            _logger.LogInformation("Iniciando sincronização de feriados por localização de usuários");

            try
            {
                var users = await _userRepository.GetAllAsync();
                var currentYear = DateTime.Now.Year;
                var processedLocations = new HashSet<string>();

                foreach (var user in users.Where(u => u.IsActive && !string.IsNullOrEmpty(u.Country)))
                {
                    var locationKey = $"{user.Country}{(string.IsNullOrEmpty(user.State) ? "" : $"-{user.State}")}";
                    
                    // Evitar duplicação - processar cada localização apenas uma vez
                    if (processedLocations.Contains(locationKey))
                        continue;

                    processedLocations.Add(locationKey);

                    try
                    {
                        await SyncHolidaysForCountryAndState(user.Country!, user.State, currentYear);
                        await SyncHolidaysForCountryAndState(user.Country!, user.State, currentYear + 1);

                        _logger.LogDebug("Feriados sincronizados para {Location}", locationKey);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erro ao sincronizar feriados para {Location}", locationKey);
                        // Continuar com próximas localizações mesmo se uma falhar
                    }
                }

                _logger.LogInformation("Sincronização de feriados por localização concluída. {Count} localizações processadas", 
                    processedLocations.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante sincronização de feriados por localização");
                throw;
            }
        }

        /// <summary>
        /// Job para limpeza de cache expirado de feriados
        /// </summary>
        [JobDisplayName("Limpeza de cache de feriados")]
        [AutomaticRetry(Attempts = 1)]
        public async Task CleanExpiredHolidayCache()
        {
            _logger.LogInformation("Iniciando limpeza de cache de feriados expirado");

            try
            {
                // Este job é mais para logging - o Redis já gerencia expiração automaticamente
                // Mas podemos usar para limpar dados de anos muito antigos
                var oldYear = DateTime.Now.Year - 2;
                
                // Aqui poderíamos limpar feriados muito antigos do banco de dados se necessário
                _logger.LogInformation("Cache de feriados verificado. Dados anteriores a {Year} podem ser removidos", oldYear);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro durante limpeza de cache de feriados");
                // Não falhar o job por isso
            }
        }

        private async Task SyncHolidaysForCountry(string country, int year)
        {
            var command = new SyncHolidaysCommand(country, year);
            var result = await _mediator.Send(command);
            
            _logger.LogInformation("Sincronizados {Count} feriados para {Country} em {Year}", 
                result.TotalSynced, country, year);
        }

        private async Task SyncHolidaysForCountryAndState(string country, string? state, int year)
        {
            var command = new SyncHolidaysCommand(country, year, state);
            var result = await _mediator.Send(command);
            
            var location = string.IsNullOrEmpty(state) ? country : $"{country}-{state}";
            _logger.LogDebug("Sincronizados {Count} feriados para {Location} em {Year}", 
                result.TotalSynced, location, year);
        }
    }

    /// <summary>
    /// Configurações para agendamento dos jobs de sincronização
    /// </summary>
    public static class HolidaySyncJobScheduler
    {
        /// <summary>
        /// Agenda todos os jobs de sincronização de feriados
        /// </summary>
        public static void ScheduleJobs()
        {
            // Sincronização diária de feriados - executar às 2h da manhã
            RecurringJob.AddOrUpdate<HolidaySyncJob>(
                "sync-current-year-holidays",
                job => job.SyncCurrentYearHolidays(),
                "0 2 * * *", // Cron: todo dia às 2h
                TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time")); // Brasília

            // Sincronização por localização - executar semanalmente aos domingos às 3h
            RecurringJob.AddOrUpdate<HolidaySyncJob>(
                "sync-user-location-holidays", 
                job => job.SyncUserLocationHolidays(),
                "0 3 * * 0", // Cron: domingo às 3h
                TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"));

            // Limpeza de cache - executar mensalmente no dia 1 às 4h
            RecurringJob.AddOrUpdate<HolidaySyncJob>(
                "clean-expired-holiday-cache",
                job => job.CleanExpiredHolidayCache(),
                "0 4 1 * *", // Cron: dia 1 de cada mês às 4h
                TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"));
        }

        /// <summary>
        /// Executa sincronização imediata (para testes ou inicialização)
        /// </summary>
        public static void TriggerImmediateSync()
        {
            BackgroundJob.Enqueue<HolidaySyncJob>(job => job.SyncCurrentYearHolidays());
        }
    }
}