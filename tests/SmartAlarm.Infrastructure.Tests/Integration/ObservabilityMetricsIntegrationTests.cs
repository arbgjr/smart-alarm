using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SmartAlarm.Infrastructure.Tests.Integration
{
    /// <summary>
    /// Teste de integração real para métricas: valida emissão e leitura de métricas customizadas via Meter.
    /// </summary>
    public class ObservabilityMetricsIntegrationTests
    {
        [Fact(DisplayName = "Deve emitir e capturar métrica customizada via Meter (OpenTelemetry)")]
        [Trait("Category", "Integration")]
        public async Task Deve_Emitir_e_Capturar_Metrica_Customizada()
        {
            // Arrange
            var meter = new Meter("SmartAlarm.TestMetrics");
            var counter = meter.CreateCounter<long>("test_counter");
            long capturedValue = 0;
            using var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Name == "test_counter")
                    listener.EnableMeasurementEvents(instrument);
            };
            listener.SetMeasurementEventCallback<long>((inst, value, tags, state) =>
            {
                capturedValue += value;
            });
            listener.Start();

            // Act
            counter.Add(5);
            counter.Add(3);
            await Task.Delay(10); // Simula operação

            // Assert
            Assert.Equal(8, capturedValue);
        }
    }
}
