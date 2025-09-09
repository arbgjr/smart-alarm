using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Commands.Holiday;
using SmartAlarm.Application.DTOs.Holiday;
using SmartAlarm.Application.Services;
using SmartAlarm.Application.Services.External;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;

namespace SmartAlarm.Application.Handlers.Holiday
{
    /// <summary>
    /// Handler para sincronização de feriados
    /// </summary>
    public class SyncHolidaysCommandHandler : IRequestHandler<SyncHolidaysCommand, SyncHolidaysResultDto>
    {
        private readonly IHolidayRepository _holidayRepository;
        private readonly ICalendarificService _calendarificService;
        private readonly IHolidayCacheService _cacheService;
        private readonly ILogger<SyncHolidaysCommandHandler> _logger;

        public SyncHolidaysCommandHandler(
            IHolidayRepository holidayRepository,
            ICalendarificService calendarificService,
            IHolidayCacheService cacheService,
            ILogger<SyncHolidaysCommandHandler> logger)
        {
            _holidayRepository = holidayRepository;
            _calendarificService = calendarificService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<SyncHolidaysResultDto> Handle(SyncHolidaysCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Iniciando sincronização de feriados para {Country}{State} - {Year}", 
                request.Country, 
                string.IsNullOrEmpty(request.State) ? "" : $"-{request.State}", 
                request.Year);

            var result = new SyncHolidaysResultDto();
            List<Domain.Entities.Holiday> holidays;

            // Verificar cache primeiro (a menos que forceRefresh seja true)
            if (!request.ForceRefresh)
            {
                var cachedHolidays = await _cacheService.GetHolidaysAsync(
                    request.Country, 
                    request.Year, 
                    request.State, 
                    cancellationToken);

                if (cachedHolidays != null && cachedHolidays.Any())
                {
                    _logger.LogInformation("Feriados obtidos do cache: {Count} feriados", cachedHolidays.Count);
                    holidays = cachedHolidays;
                    result.FromCache = true;
                }
                else
                {
                    holidays = await FetchFromExternalService(request, cancellationToken);
                    result.FromCache = false;
                }
            }
            else
            {
                holidays = await FetchFromExternalService(request, cancellationToken);
                result.FromCache = false;
                
                // Invalidar cache existente
                await _cacheService.InvalidateAsync(
                    request.Country, 
                    request.Year, 
                    request.State, 
                    cancellationToken);
            }

            // Sincronizar com banco de dados
            if (holidays.Any() && !result.FromCache)
            {
                var syncResult = await SyncWithDatabase(holidays, cancellationToken);
                result.NewHolidays = syncResult.NewCount;
                result.UpdatedHolidays = syncResult.UpdatedCount;

                // Armazenar no cache
                await _cacheService.SetHolidaysAsync(
                    holidays, 
                    request.Country, 
                    request.Year, 
                    request.State, 
                    TimeSpan.FromDays(30), 
                    cancellationToken);
            }

            // Preparar resultado
            result.TotalSynced = holidays.Count;
            result.Holidays = holidays.Select(h => new HolidayResponseDto
            {
                Id = h.Id,
                Date = h.Date,
                Description = h.Description,
                Name = h.Name,
                Type = h.Type.ToString(),
                Country = h.Country,
                State = h.State
            }).ToList();

            result.Message = result.FromCache 
                ? $"Feriados obtidos do cache: {result.TotalSynced} feriados"
                : $"Sincronização concluída: {result.TotalSynced} feriados, {result.NewHolidays} novos, {result.UpdatedHolidays} atualizados";

            _logger.LogInformation(result.Message);

            return result;
        }

        private async Task<List<Domain.Entities.Holiday>> FetchFromExternalService(
            SyncHolidaysCommand request, 
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Buscando feriados do serviço externo (Calendarific)");

            List<Domain.Entities.Holiday> holidays;

            if (string.IsNullOrEmpty(request.State))
            {
                holidays = await _calendarificService.GetHolidaysAsync(
                    request.Country, 
                    request.Year, 
                    cancellationToken);
            }
            else
            {
                holidays = await _calendarificService.GetHolidaysAsync(
                    request.Country, 
                    request.State, 
                    request.Year, 
                    cancellationToken);
            }

            _logger.LogInformation("Obtidos {Count} feriados do serviço externo", holidays.Count);

            return holidays;
        }

        private async Task<(int NewCount, int UpdatedCount)> SyncWithDatabase(
            List<Domain.Entities.Holiday> holidays, 
            CancellationToken cancellationToken)
        {
            var newCount = 0;
            var updatedCount = 0;

            foreach (var holiday in holidays)
            {
                // Verificar se já existe no banco
                var existing = await _holidayRepository.GetByDateAndCountryAsync(
                    holiday.Date, 
                    holiday.Country, 
                    holiday.State);

                if (existing == null)
                {
                    // Adicionar novo feriado
                    await _holidayRepository.AddAsync(holiday);
                    newCount++;
                    _logger.LogDebug("Novo feriado adicionado: {Name} - {Date}", holiday.Name, holiday.Date);
                }
                else if (existing.Name != holiday.Name || existing.Description != holiday.Description)
                {
                    // Atualizar feriado existente
                    existing.UpdateDescription(holiday.Description);
                    existing.MarkAsSynced();
                    await _holidayRepository.UpdateAsync(existing);
                    updatedCount++;
                    _logger.LogDebug("Feriado atualizado: {Name} - {Date}", holiday.Name, holiday.Date);
                }
                else
                {
                    // Apenas marcar como sincronizado
                    existing.MarkAsSynced();
                    await _holidayRepository.UpdateAsync(existing);
                }
            }

            await _holidayRepository.SaveChangesAsync();

            return (newCount, updatedCount);
        }
    }
}