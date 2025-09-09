using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Services.External;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Infrastructure.Services.External.Models;
using Polly;
using Polly.CircuitBreaker;

namespace SmartAlarm.Infrastructure.Services.External
{
    /// <summary>
    /// Implementação do serviço de integração com API Calendarific
    /// Documentação: https://calendarific.com/api-documentation
    /// </summary>
    public class CalendarificService : ICalendarificService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CalendarificService> _logger;
        private readonly string _apiKey;
        private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;
        private const string BaseUrl = "https://calendarific.com/api/v2";

        public CalendarificService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<CalendarificService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["Calendarific:ApiKey"] ?? string.Empty;

            // Configurar política de retry com circuit breaker
            _retryPolicy = Policy<HttpResponseMessage>
                .HandleResult(r => !r.IsSuccessStatusCode)
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        _logger.LogWarning("Retry {RetryCount} após {Timespan}s para Calendarific API", 
                            retryCount, timespan.TotalSeconds);
                    })
                .WrapAsync(Policy
                    .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                    .CircuitBreakerAsync(
                        3,
                        TimeSpan.FromMinutes(1),
                        onBreak: (result, duration) =>
                        {
                            _logger.LogError("Circuit breaker aberto para Calendarific API por {Duration}s", 
                                duration.TotalSeconds);
                        },
                        onReset: () =>
                        {
                            _logger.LogInformation("Circuit breaker resetado para Calendarific API");
                        }));
        }

        public async Task<List<Holiday>> GetHolidaysAsync(
            string countryCode, 
            int year, 
            CancellationToken cancellationToken = default)
        {
            return await GetHolidaysAsync(countryCode, null, year, cancellationToken);
        }

        public async Task<List<Holiday>> GetHolidaysAsync(
            string countryCode, 
            string state, 
            int year, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogWarning("Calendarific API key não configurada. Retornando lista vazia de feriados.");
                return new List<Holiday>();
            }

            try
            {
                var url = $"{BaseUrl}/holidays?api_key={_apiKey}&country={countryCode}&year={year}";
                
                if (!string.IsNullOrEmpty(state))
                {
                    url += $"&location={countryCode}-{state}";
                }

                _logger.LogInformation("Buscando feriados de {Country}{State} para {Year}", 
                    countryCode, 
                    string.IsNullOrEmpty(state) ? "" : $"-{state}", 
                    year);

                var response = await _retryPolicy.ExecuteAsync(async () =>
                    await _httpClient.GetAsync(url, cancellationToken));

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Erro ao buscar feriados. Status: {StatusCode}", response.StatusCode);
                    return new List<Holiday>();
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var apiResponse = JsonSerializer.Deserialize<CalendarificResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (apiResponse?.Response?.Holidays == null)
                {
                    _logger.LogWarning("Resposta inválida da API Calendarific");
                    return new List<Holiday>();
                }

                var holidays = apiResponse.Response.Holidays
                    .Where(h => h.Date?.Iso != null)
                    .Select(h => new Holiday(
                        Guid.NewGuid(),
                        DateTime.Parse(h.Date.Iso),
                        h.Name ?? "Feriado",
                        h.Description ?? h.Name ?? "Feriado",
                        DetermineHolidayType(h.Type),
                        countryCode,
                        state
                    ))
                    .ToList();

                _logger.LogInformation("Encontrados {Count} feriados para {Country}{State} em {Year}", 
                    holidays.Count, 
                    countryCode, 
                    string.IsNullOrEmpty(state) ? "" : $"-{state}", 
                    year);

                return holidays;
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogError(ex, "Circuit breaker aberto para Calendarific API");
                return new List<Holiday>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Erro de rede ao buscar feriados");
                return new List<Holiday>();
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout ao buscar feriados");
                return new List<Holiday>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao buscar feriados");
                return new List<Holiday>();
            }
        }

        public async Task<bool> IsHolidayAsync(
            DateTime date, 
            string countryCode, 
            CancellationToken cancellationToken = default)
        {
            var holidays = await GetHolidaysAsync(countryCode, date.Year, cancellationToken);
            return holidays.Any(h => h.Date.Date == date.Date);
        }

        private HolidayType DetermineHolidayType(List<string> types)
        {
            if (types == null || !types.Any())
                return HolidayType.Other;

            // Mapear tipos da API Calendarific para nossos tipos
            if (types.Contains("National holiday"))
                return HolidayType.National;
            if (types.Contains("Local holiday") || types.Contains("State holiday"))
                return HolidayType.State;
            if (types.Contains("Religious"))
                return HolidayType.Religious;
            if (types.Contains("Observance"))
                return HolidayType.Observance;

            return HolidayType.Other;
        }
    }
}

namespace SmartAlarm.Infrastructure.Services.External.Models
{
    /// <summary>
    /// Modelos para deserialização da resposta da API Calendarific
    /// </summary>
    internal class CalendarificResponse
    {
        public CalendarificResponseData Response { get; set; }
    }

    internal class CalendarificResponseData
    {
        public List<CalendarificHoliday> Holidays { get; set; }
    }

    internal class CalendarificHoliday
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public CalendarificDate Date { get; set; }
        public List<string> Type { get; set; }
    }

    internal class CalendarificDate
    {
        public string Iso { get; set; }
        public CalendarificDateTime Datetime { get; set; }
    }

    internal class CalendarificDateTime
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
    }
}