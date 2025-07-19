using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using FluentAssertions;
using SmartAlarm.AiService.Infrastructure.MachineLearning;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.ValueObjects;

namespace SmartAlarm.AiService.Tests.Infrastructure
{
    /// <summary>
    /// Testes para o serviço de Machine Learning
    /// </summary>
    public class MachineLearningServiceTests
    {
        private readonly Mock<ILogger<MachineLearningService>> _loggerMock;
        private readonly IConfiguration _configuration;
        private readonly MachineLearningService _service;

        public MachineLearningServiceTests()
        {
            _loggerMock = new Mock<ILogger<MachineLearningService>>();
            
            // Criar configuração real usando InMemoryCollection
            var inMemorySettings = new Dictionary<string, string>
            {
                ["MachineLearning:ModelsPath"] = "./test-models/"
            };
            
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _service = new MachineLearningService(_loggerMock.Object, _configuration);
        }

        [Fact]
        public async Task AnalyzeAlarmPatternsAsync_ComAlarmes_DeveRetornarAnaliseValida()
        {
            // Arrange
            var alarmes = CriarAlarmesTeste();
            var startDate = DateTime.UtcNow.AddDays(-30);
            var endDate = DateTime.UtcNow;

            // Act
            var resultado = await _service.AnalyzeAlarmPatternsAsync(alarmes, startDate, endDate);

            // Assert
            resultado.Should().NotBeNull();
            resultado.MostCommonAlarmTime.Should().BePositive();
            resultado.MostActiveDays.Should().NotBeEmpty();
            resultado.AverageSnoozeCount.Should().BeGreaterOrEqualTo(0);
            resultado.AverageWakeupDelay.Should().BeGreaterOrEqualTo(TimeSpan.Zero);
            resultado.SleepPattern.Should().BeOneOf("Early Bird", "Night Owl", "Regular");
            resultado.PatternConfidence.Should().BeInRange(0.0, 1.0);
            resultado.ModelAccuracy.Should().BeInRange(0.0, 1.0);
        }

        [Fact]
        public async Task AnalyzeAlarmPatternsAsync_SemAlarmes_DeveRetornarAnaliseBasica()
        {
            // Arrange
            var alarmes = new List<Alarm>();
            var startDate = DateTime.UtcNow.AddDays(-30);
            var endDate = DateTime.UtcNow;

            // Act
            var resultado = await _service.AnalyzeAlarmPatternsAsync(alarmes, startDate, endDate);

            // Assert
            resultado.Should().NotBeNull();
            resultado.MostCommonAlarmTime.Should().Be(TimeSpan.FromHours(7)); // Fallback
            resultado.PatternConfidence.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task PredictOptimalTimeAsync_ComHistorico_DeveRetornarPredicaoValida()
        {
            // Arrange
            var alarmes = CriarAlarmesTeste();
            var targetDay = DayOfWeek.Monday;
            var context = "work";

            // Act
            var resultado = await _service.PredictOptimalTimeAsync(alarmes, targetDay, context);

            // Assert
            resultado.Should().NotBeNull();
            resultado.SuggestedTime.Should().BePositive();
            resultado.ConfidenceScore.Should().BeInRange(0.0, 1.0);
            resultado.Reasoning.Should().NotBeEmpty();
            resultado.ModelAccuracy.Should().BeInRange(0.0, 1.0);
            resultado.FactorsConsidered.Should().NotBeEmpty();
        }

        [Fact]
        public async Task PredictOptimalTimeAsync_SemHistorico_DeveRetornarPredicaoFallback()
        {
            // Arrange
            var alarmes = new List<Alarm>();
            var targetDay = DayOfWeek.Friday;
            var context = "exercise";

            // Act
            var resultado = await _service.PredictOptimalTimeAsync(alarmes, targetDay, context);

            // Assert
            resultado.Should().NotBeNull();
            resultado.ConfidenceScore.Should().BeGreaterThan(0.5); // Fallback deve ter confiança razoável
            resultado.Reasoning.Should().Contain("estimativa"); // Deve indicar que é estimativa
        }

        [Theory]
        [InlineData("work", 6.5, 8.5)] // Contexto work deve estar na faixa de horário de trabalho
        [InlineData("exercise", 5.0, 7.0)] // Exercício normalmente mais cedo
        [InlineData("personal", 8.0, 10.0)] // Pessoal pode ser mais tarde
        public async Task PredictOptimalTimeAsync_ComContextoEspecifico_DeveAjustarHorario(
            string context, double minHours, double maxHours)
        {
            // Arrange
            var alarmes = CriarAlarmesTeste();
            var targetDay = DayOfWeek.Wednesday;

            // Act
            var resultado = await _service.PredictOptimalTimeAsync(alarmes, targetDay, context);

            // Assert
            resultado.SuggestedTime.TotalHours.Should().BeInRange(minHours, maxHours);
            resultado.Reasoning.Should().Contain(context);
        }

        [Fact]
        public async Task TrainModelAsync_ComDadosSuficientes_DeveExecutarSemErros()
        {
            // Arrange
            var alarmes = CriarAlarmesTeste(20); // Dados suficientes para treinamento

            // Act
            Func<Task> act = async () => await _service.TrainModelAsync(alarmes);

            // Assert
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task TrainModelAsync_ComDadosInsuficientes_DeveLogarAviso()
        {
            // Arrange
            var alarmes = CriarAlarmesTeste(2); // Poucos dados

            // Act
            await _service.TrainModelAsync(alarmes);

            // Assert
            // Verificar que foi logado um warning sobre dados insuficientes
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("insuficientes")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #region Métodos Auxiliares

        private List<Alarm> CriarAlarmesTeste(int quantidade = 5)
        {
            var alarmes = new List<Alarm>();
            var userId = Guid.NewGuid();

            for (int i = 0; i < quantidade; i++)
            {
                var alarmId = Guid.NewGuid();
                var time = DateTime.Today.AddHours(7 + (i % 3)); // Varia entre 7h, 8h, 9h
                var alarm = new Alarm(alarmId, new Name($"Teste Alarm {i}"), time, true, userId);
                
                // Adicionar schedule ativo
                var schedule = new Schedule(
                    Guid.NewGuid(),
                    TimeOnly.FromDateTime(time),
                    ScheduleRecurrence.Weekly,
                    SmartAlarm.Domain.Entities.DaysOfWeek.Monday | 
                    SmartAlarm.Domain.Entities.DaysOfWeek.Tuesday |
                    SmartAlarm.Domain.Entities.DaysOfWeek.Wednesday,
                    alarmId
                );
                
                // Usar reflexão para adicionar schedule (já que Add não é público)
                var schedulesField = typeof(Alarm).GetField("_schedules", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var schedulesList = (List<Schedule>)schedulesField!.GetValue(alarm)!;
                schedulesList.Add(schedule);

                alarmes.Add(alarm);
            }

            return alarmes;
        }

        #endregion
    }
}
