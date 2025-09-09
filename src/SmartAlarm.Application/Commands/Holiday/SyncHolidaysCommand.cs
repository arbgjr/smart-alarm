using System.Collections.Generic;
using MediatR;
using SmartAlarm.Application.DTOs.Holiday;

namespace SmartAlarm.Application.Commands.Holiday
{
    /// <summary>
    /// Comando para sincronizar feriados de uma fonte externa
    /// </summary>
    public class SyncHolidaysCommand : IRequest<SyncHolidaysResultDto>
    {
        /// <summary>
        /// Código do país (ex: BR para Brasil)
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// Código do estado (opcional, ex: SP para São Paulo)
        /// </summary>
        public string? State { get; set; }

        /// <summary>
        /// Ano para sincronizar feriados
        /// </summary>
        public int Year { get; set; }

        /// <summary>
        /// Se deve forçar atualização mesmo se já estiver em cache
        /// </summary>
        public bool ForceRefresh { get; set; }

        public SyncHolidaysCommand(string country, int year, string? state = null, bool forceRefresh = false)
        {
            Country = country;
            Year = year;
            State = state;
            ForceRefresh = forceRefresh;
        }
    }

    /// <summary>
    /// Resultado da sincronização de feriados
    /// </summary>
    public class SyncHolidaysResultDto
    {
        /// <summary>
        /// Total de feriados sincronizados
        /// </summary>
        public int TotalSynced { get; set; }

        /// <summary>
        /// Novos feriados adicionados
        /// </summary>
        public int NewHolidays { get; set; }

        /// <summary>
        /// Feriados atualizados
        /// </summary>
        public int UpdatedHolidays { get; set; }

        /// <summary>
        /// Se os dados vieram do cache
        /// </summary>
        public bool FromCache { get; set; }

        /// <summary>
        /// Lista de feriados sincronizados
        /// </summary>
        public List<HolidayResponseDto> Holidays { get; set; } = new();

        /// <summary>
        /// Mensagem de status
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}