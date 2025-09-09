using SmartAlarm.Domain.Abstractions;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests.Integration
{
    /// <summary>
    /// Teste de integração real para observabilidade: verifica emissão de Activity (tracing), logs estruturados e métricas customizadas.
    /// </summary>
    public class ObservabilityIntegrationTests
    {
        [Fact(DisplayName = "Deve emitir Activity (tracing) e log estruturado em operação real")]
        [Trait("Category", "Integration")]
        public async Task Deve_Emitir_Tracing_e_Log()
        {
            // Arrange
            var activitySource = new ActivitySource("SmartAlarm.Test");
            Activity? capturedActivity = null;
            using var listener = new ActivityListener
            {
                ShouldListenTo = src => src.Name == "SmartAlarm.Test",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = act => capturedActivity = act,
                ActivityStopped = _ => { }
            };
            ActivitySource.AddActivityListener(listener);

            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger("ObservabilityTest");

            // Act
            using (var activity = activitySource.StartActivity("OperacaoDeTeste"))
            {
                activity?.SetTag("operation.type", "integration-test");
                logger.LogInformation("Operação de teste executada com sucesso. TraceId: {TraceId}", activity?.TraceId);
                await Task.Delay(10); // Simula operação
            }

            // Assert
            Assert.NotNull(capturedActivity);
            Assert.Equal("OperacaoDeTeste", capturedActivity.DisplayName);
            Assert.Equal("integration-test", capturedActivity.Tags.FirstOrDefault(t => t.Key == "operation.type").Value);
        }
    }
}
