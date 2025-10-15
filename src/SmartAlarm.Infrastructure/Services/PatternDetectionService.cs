using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartAlarm.Application.Services;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;

namespace SmartAlarm.Infrastructure.Services
{
    /// <summary>
    /// Implementação do serviço de detecção de padrões usando algoritmos simples
    /// </summary>
    public class PatternDetectionService : IPatternDetectionService
    {
        private readonly IAlarmEventRepository _eventRepository;
        private readonly ILogger<PatternDetectionService> _logger;

        // Thresholds para detecção de padrões
        private const double MinPatternConfidence = 0.6;
        private const int MinPatternFrequency = 3;
        private const double MinDisableRate = 0.7; // 70% das vezes
        private const int MaxTimeVariationMinutes = 60; // 1 hora

        public PatternDetectionService(
            IAlarmEventRepository eventRepository,
            ILogger<PatternDetectionService> logger)
        {
            _eventRepository = eventRepository;
            _logger = logger;
        }

        public async Task<List<RoutinePattern>> DetectUserRoutinePatternsAsync(
            Guid userId, 
            int analysisWindowDays = 30, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Detectando padrões de rotina para usuário {UserId} - {Days} dias", userId, analysisWindowDays);

            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-analysisWindowDays);

            var events = await _eventRepository.GetUserEventsAsync(userId, startDate, endDate, cancellationToken);
            
            if (events.Count < MinPatternFrequency)
            {
                _logger.LogDebug("Poucos eventos para análise: {Count}", events.Count);
                return new List<RoutinePattern>();
            }

            var patterns = new List<RoutinePattern>();

            // Detectar padrões por dia da semana
            foreach (DayOfWeek dayOfWeek in Enum.GetValues<DayOfWeek>())
            {
                var dayEvents = events
                    .Where(e => e.DayOfWeek == dayOfWeek)
                    .Where(e => e.EventType == AlarmEventType.Triggered || e.EventType == AlarmEventType.Created)
                    .OrderBy(e => e.Time)
                    .ToList();

                if (dayEvents.Count < MinPatternFrequency)
                    continue;

                // Agrupar eventos por horário aproximado (janela de 1 hora)
                var timeGroups = GroupEventsByTime(dayEvents, TimeSpan.FromMinutes(60));

                foreach (var group in timeGroups.Where(g => g.Value.Count >= MinPatternFrequency))
                {
                    var pattern = CreateRoutinePattern(userId, dayOfWeek, group.Value, analysisWindowDays);
                    if (pattern.Confidence >= MinPatternConfidence)
                    {
                        patterns.Add(pattern);
                    }
                }
            }

            _logger.LogInformation("Detectados {Count} padrões de rotina para usuário {UserId}", patterns.Count, userId);
            return patterns;
        }

        public async Task<List<DisablePattern>> DetectDisablePatternsAsync(
            Guid userId, 
            int analysisWindowDays = 30, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Detectando padrões de desativação para usuário {UserId}", userId);

            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-analysisWindowDays);

            var disableEvents = await _eventRepository.GetUserEventsByTypeAsync(
                userId, AlarmEventType.Disabled, startDate, endDate, cancellationToken);

            var triggeredEvents = await _eventRepository.GetUserEventsByTypeAsync(
                userId, AlarmEventType.Triggered, startDate, endDate, cancellationToken);

            var patterns = new List<DisablePattern>();

            // Analisar padrões por dia da semana
            foreach (DayOfWeek dayOfWeek in Enum.GetValues<DayOfWeek>())
            {
                var dayDisables = disableEvents.Where(e => e.DayOfWeek == dayOfWeek).ToList();
                var dayTriggers = triggeredEvents.Where(e => e.DayOfWeek == dayOfWeek).ToList();

                if (dayDisables.Count + dayTriggers.Count < MinPatternFrequency)
                    continue;

                var disableRate = (double)dayDisables.Count / (dayDisables.Count + dayTriggers.Count);
                
                if (disableRate >= MinDisableRate)
                {
                    // Detectar consecutividade
                    var consecutiveDisables = CalculateConsecutiveDisables(dayDisables, dayOfWeek);

                    patterns.Add(new DisablePattern
                    {
                        UserId = userId,
                        DayOfWeek = dayOfWeek,
                        ConsecutiveDisables = consecutiveDisables,
                        DisableRate = disableRate,
                        Reason = ExtractDisableReason(dayDisables),
                        Confidence = Math.Min(disableRate * 1.2, 1.0), // Boost confidence but cap at 1.0
                        DetectedAt = DateTime.UtcNow
                    });
                }
            }

            _logger.LogInformation("Detectados {Count} padrões de desativação para usuário {UserId}", patterns.Count, userId);
            return patterns;
        }

        public async Task<List<SnoozePattern>> DetectSnoozePatternsAsync(
            Guid userId, 
            int analysisWindowDays = 30, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Detectando padrões de soneca para usuário {UserId}", userId);

            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-analysisWindowDays);

            var snoozeEvents = await _eventRepository.GetUserEventsByTypeAsync(
                userId, AlarmEventType.Snoozed, startDate, endDate, cancellationToken);

            var triggeredEvents = await _eventRepository.GetUserEventsByTypeAsync(
                userId, AlarmEventType.Triggered, startDate, endDate, cancellationToken);

            var patterns = new List<SnoozePattern>();

            // Analisar padrões por dia da semana
            foreach (DayOfWeek dayOfWeek in Enum.GetValues<DayOfWeek>())
            {
                var daySnoozes = snoozeEvents
                    .Where(e => e.DayOfWeek == dayOfWeek && e.SnoozeMinutes.HasValue)
                    .ToList();

                var dayTriggers = triggeredEvents.Where(e => e.DayOfWeek == dayOfWeek).ToList();

                if (daySnoozes.Count < 2) // Precisa de pelo menos 2 sonecas para estabelecer padrão
                    continue;

                var snoozeRate = (double)daySnoozes.Count / (daySnoozes.Count + dayTriggers.Count);
                var avgSnoozeMinutes = (int)daySnoozes.Average(e => e.SnoozeMinutes!.Value);
                var avgSnoozeCount = CalculateAverageSnoozeCount(daySnoozes);
                var commonDurations = daySnoozes
                    .GroupBy(e => e.SnoozeMinutes!.Value)
                    .OrderByDescending(g => g.Count())
                    .Take(3)
                    .Select(g => g.Key)
                    .ToList();

                patterns.Add(new SnoozePattern
                {
                    UserId = userId,
                    DayOfWeek = dayOfWeek,
                    AverageSnoozeMinutes = avgSnoozeMinutes,
                    AverageSnoozeCount = avgSnoozeCount,
                    SnoozeRate = snoozeRate,
                    CommonSnoozeDurations = commonDurations,
                    Confidence = Math.Min(snoozeRate * 1.5 * (daySnoozes.Count / 10.0), 1.0),
                    DetectedAt = DateTime.UtcNow
                });
            }

            _logger.LogInformation("Detectados {Count} padrões de soneca para usuário {UserId}", patterns.Count, userId);
            return patterns;
        }

        public async Task<List<BehaviorAnomaly>> DetectBehaviorAnomaliesAsync(
            Guid userId, 
            int analysisWindowDays = 30, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Detectando anomalias comportamentais para usuário {UserId}", userId);

            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-analysisWindowDays);

            var events = await _eventRepository.GetUserEventsAsync(userId, startDate, endDate, cancellationToken);
            var anomalies = new List<BehaviorAnomaly>();

            // Detectar horários de despertar incomuns
            var wakeEvents = events.Where(e => e.EventType == AlarmEventType.Triggered).ToList();
            anomalies.AddRange(DetectUnusualWakeTimes(userId, wakeEvents));

            // Detectar desativações consecutivas excessivas
            var disableEvents = events.Where(e => e.EventType == AlarmEventType.Disabled).ToList();
            anomalies.AddRange(DetectExcessiveConsecutiveDisables(userId, disableEvents));

            // Detectar sonecas excessivas
            var snoozeEvents = events.Where(e => e.EventType == AlarmEventType.Snoozed).ToList();
            anomalies.AddRange(DetectExcessiveSnoozing(userId, snoozeEvents));

            _logger.LogInformation("Detectadas {Count} anomalias comportamentais para usuário {UserId}", anomalies.Count, userId);
            return anomalies;
        }

        public async Task<double> CalculateConsistencyScoreAsync(
            Guid userId, 
            int analysisWindowDays = 30, 
            CancellationToken cancellationToken = default)
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-analysisWindowDays);

            var events = await _eventRepository.GetUserEventsAsync(userId, startDate, endDate, cancellationToken);
            var triggeredEvents = events.Where(e => e.EventType == AlarmEventType.Triggered).ToList();

            if (triggeredEvents.Count < 7) // Precisa de pelo menos uma semana de dados
                return 0.0;

            var dayGroups = triggeredEvents.GroupBy(e => e.DayOfWeek).ToList();
            var consistencyScores = new List<double>();

            foreach (var dayGroup in dayGroups)
            {
                var times = dayGroup.Select(e => e.Time.ToTimeSpan().TotalMinutes).ToList();
                if (times.Count < 2) continue;

                var avg = times.Average();
                var variance = times.Sum(t => Math.Pow(t - avg, 2)) / times.Count;
                var stdDev = Math.Sqrt(variance);

                // Normalizar: quanto menor o desvio padrão, maior a consistência
                var consistency = Math.Max(0, 1 - (stdDev / 120.0)); // 120 min = 2h como referência
                consistencyScores.Add(consistency);
            }

            return consistencyScores.Any() ? consistencyScores.Average() : 0.0;
        }

        public async Task<List<OptimalTimeSlot>> SuggestOptimalTimeSlotsAsync(
            Guid userId, 
            DayOfWeek dayOfWeek, 
            CancellationToken cancellationToken = default)
        {
            var patterns = await DetectUserRoutinePatternsAsync(userId, 30, cancellationToken);
            var dayPatterns = patterns.Where(p => p.DayOfWeek == dayOfWeek).ToList();

            if (!dayPatterns.Any())
                return new List<OptimalTimeSlot>();

            return dayPatterns.Select(pattern => new OptimalTimeSlot
            {
                StartTime = pattern.AverageTime.AddMinutes(-15),
                EndTime = pattern.AverageTime.AddMinutes(15),
                OptimalityScore = pattern.Confidence,
                Reasoning = $"Baseado em {pattern.Frequency} observações com {pattern.Confidence:P0} de confiança",
                SupportingEvidence = new List<string>
                {
                    $"Padrão detectado: {pattern.Type}",
                    $"Horário médio: {pattern.AverageTime}",
                    $"Variação: ±{pattern.TimeVariation.TotalMinutes:F0} minutos"
                }
            }).ToList();
        }

        // Métodos auxiliares privados

        private Dictionary<TimeOnly, List<AlarmEvent>> GroupEventsByTime(List<AlarmEvent> events, TimeSpan window)
        {
            var groups = new Dictionary<TimeOnly, List<AlarmEvent>>();

            foreach (var evt in events)
            {
                var groupKey = groups.Keys.FirstOrDefault(k => 
                    Math.Abs((k.ToTimeSpan() - evt.Time.ToTimeSpan()).TotalMinutes) <= window.TotalMinutes);

                if (groupKey == default)
                {
                    groups[evt.Time] = new List<AlarmEvent> { evt };
                }
                else
                {
                    groups[groupKey].Add(evt);
                }
            }

            return groups;
        }

        private RoutinePattern CreateRoutinePattern(Guid userId, DayOfWeek dayOfWeek, List<AlarmEvent> events, int windowDays)
        {
            var times = events.Select(e => e.Time.ToTimeSpan().TotalMinutes).ToList();
            var avgTime = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(times.Average()));
            var variance = times.Sum(t => Math.Pow(t - times.Average(), 2)) / times.Count;
            var stdDev = TimeSpan.FromMinutes(Math.Sqrt(variance));

            return new RoutinePattern
            {
                UserId = userId,
                Type = DeterminePatternType(avgTime),
                DayOfWeek = dayOfWeek,
                AverageTime = avgTime,
                TimeVariation = stdDev,
                Frequency = events.Count,
                Confidence = CalculatePatternConfidence(events.Count, stdDev, windowDays),
                FirstObserved = events.Min(e => e.Timestamp),
                LastObserved = events.Max(e => e.Timestamp),
                Description = $"Rotina regular às {avgTime} com variação de ±{stdDev.TotalMinutes:F0} minutos"
            };
        }

        private PatternType DeterminePatternType(TimeOnly time)
        {
            var hour = time.Hour;
            return hour switch
            {
                >= 5 and <= 9 => PatternType.WakeUp,
                >= 22 or <= 1 => PatternType.Sleep,
                >= 7 and <= 18 => PatternType.Work,
                _ => PatternType.Other
            };
        }

        private double CalculatePatternConfidence(int frequency, TimeSpan variation, int windowDays)
        {
            var frequencyScore = Math.Min((double)frequency / (windowDays / 7.0), 1.0); // Quanto mais frequente, melhor
            var consistencyScore = Math.Max(0, 1 - (variation.TotalMinutes / MaxTimeVariationMinutes)); // Quanto menor variação, melhor

            return (frequencyScore + consistencyScore) / 2.0;
        }

        private int CalculateConsecutiveDisables(List<AlarmEvent> disableEvents, DayOfWeek dayOfWeek)
        {
            if (!disableEvents.Any()) return 0;

            var sortedEvents = disableEvents.OrderBy(e => e.Timestamp).ToList();
            var maxConsecutive = 1;
            var currentConsecutive = 1;

            for (int i = 1; i < sortedEvents.Count; i++)
            {
                var daysDiff = (sortedEvents[i].Timestamp.Date - sortedEvents[i-1].Timestamp.Date).Days;
                if (daysDiff == 7) // Mesmo dia da semana na próxima semana
                {
                    currentConsecutive++;
                }
                else
                {
                    maxConsecutive = Math.Max(maxConsecutive, currentConsecutive);
                    currentConsecutive = 1;
                }
            }

            return Math.Max(maxConsecutive, currentConsecutive);
        }

        private string ExtractDisableReason(List<AlarmEvent> disableEvents)
        {
            var reasons = disableEvents
                .Where(e => !string.IsNullOrEmpty(e.Metadata))
                .Select(e => e.Metadata)
                .GroupBy(r => r)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            return reasons?.Key ?? "Motivo não especificado";
        }

        private int CalculateAverageSnoozeCount(List<AlarmEvent> snoozeEvents)
        {
            // Agrupar por data e calcular quantas sonecas por dia
            var dailyCounts = snoozeEvents
                .GroupBy(e => e.Timestamp.Date)
                .Select(g => g.Count())
                .ToList();

            return dailyCounts.Any() ? (int)dailyCounts.Average() : 0;
        }

        private List<BehaviorAnomaly> DetectUnusualWakeTimes(Guid userId, List<AlarmEvent> wakeEvents)
        {
            var anomalies = new List<BehaviorAnomaly>();
            
            if (wakeEvents.Count < 7) return anomalies; // Precisa de pelo menos uma semana

            var avgWakeTime = wakeEvents.Average(e => e.Time.ToTimeSpan().TotalMinutes);
            var threshold = 90; // 1.5 horas de diferença

            foreach (var evt in wakeEvents)
            {
                var diff = Math.Abs(evt.Time.ToTimeSpan().TotalMinutes - avgWakeTime);
                if (diff > threshold)
                {
                    anomalies.Add(new BehaviorAnomaly
                    {
                        UserId = userId,
                        Type = AnomalyType.UnusualWakeTime,
                        Timestamp = evt.Timestamp,
                        Description = $"Horário de despertar incomum: {evt.Time} (média: {TimeSpan.FromMinutes(avgWakeTime)})",
                        Severity = Math.Min(diff / 180.0, 1.0), // 3h = severidade máxima
                        DetectedAt = DateTime.UtcNow
                    });
                }
            }

            return anomalies;
        }

        private List<BehaviorAnomaly> DetectExcessiveConsecutiveDisables(Guid userId, List<AlarmEvent> disableEvents)
        {
            // Implementação simplificada - detecta mais de 3 desativações consecutivas no mesmo dia da semana
            var anomalies = new List<BehaviorAnomaly>();
            
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                var consecutive = CalculateConsecutiveDisables(
                    disableEvents.Where(e => e.DayOfWeek == day).ToList(), day);

                if (consecutive > 3)
                {
                    anomalies.Add(new BehaviorAnomaly
                    {
                        UserId = userId,
                        Type = AnomalyType.ConsecutiveDisables,
                        Timestamp = DateTime.UtcNow,
                        Description = $"Alarmes desativados consecutivamente {consecutive} vezes às {day}s",
                        Severity = Math.Min(consecutive / 10.0, 1.0),
                        DetectedAt = DateTime.UtcNow
                    });
                }
            }

            return anomalies;
        }

        private List<BehaviorAnomaly> DetectExcessiveSnoozing(Guid userId, List<AlarmEvent> snoozeEvents)
        {
            var anomalies = new List<BehaviorAnomaly>();
            
            // Detectar dias com muitas sonecas
            var dailySnoozes = snoozeEvents
                .GroupBy(e => e.Timestamp.Date)
                .Where(g => g.Count() > 5) // Mais de 5 sonecas em um dia
                .ToList();

            foreach (var dayGroup in dailySnoozes)
            {
                anomalies.Add(new BehaviorAnomaly
                {
                    UserId = userId,
                    Type = AnomalyType.ExcessiveSnoozing,
                    Timestamp = dayGroup.Key,
                    Description = $"Sonecas excessivas: {dayGroup.Count()} sonecas em um dia",
                    Severity = Math.Min(dayGroup.Count() / 10.0, 1.0),
                    DetectedAt = DateTime.UtcNow
                });
            }

            return anomalies;
        }
    }
}