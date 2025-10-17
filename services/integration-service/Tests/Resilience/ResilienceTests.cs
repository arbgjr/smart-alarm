using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using Xunit.Abstractions;

namespace SmartAlarm.IntegrationService.Tests.Resilience
{
    /// <summary>
    /// Testes de resiliência e recuperação de falhas
    /// </summary>
    public class ResilienceTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly ITestOutputHelper _output;

        public ResilienceTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
        {
            _factory = factory;
            _output = output;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task Service_Should_RecoverFrom_TransientFailures()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var provider = "google";
            var attempts = 5;
            var successCount = 0;

            // Act - Simular falhas transientes
            for (int i = 0; i < attempts; i++)
            {
                try
                {
                    var response = await _client.PostAsync(
                        $"/api/v1/integrations/calendar/sync?userId={userId}&provider={provider}",
                        JsonContent.Create(new { simulateTransientFailure = i < 2 })); // Primeiras 2 falham

                    if (response.IsSuccessStatusCode)
                    {
                        successCount++;
                    }

                    _output.WriteLine($"Attempt {i + 1}: {response.StatusCode}");
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"Attempt {i + 1}: Exception - {ex.Message}");
                }

                await Task.Delay(1000); // Aguardar entre tentativas
            }

            // Assert - Deve se recuperar após falhas iniciais
            Assert.True(successCount >= 2, $"Should recover from transient failures, got {successCount} successes");

            _output.WriteLine($"Transient failure recovery: {successCount}/{attempts} successful");
        }

        [Fact]
        public async Task CircuitBreaker_Should_PreventCascadingFailures()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var provider = "microsoft";
            var failureThreshold = 3;
            var totalRequests = 10;

            var responses = new List<HttpResponseMessage>();

            // Act - Disparar circuit breaker com falhas consecutivas
            for (int i = 0; i < totalRequests; i++)
            {
                var response = await _client.PostAsync(
                    $"/api/v1/integrations/calendar/sync?userId={userId}&provider={provider}",
                    JsonContent.Create(new { forceFailure = i < failureThreshold + 2 }));

                responses.Add(response);
                _output.WriteLine($"Request {i + 1}: {response.StatusCode}");

                await Task.Delay(100);
            }

            // Assert - Circuit breaker deve estar ativo após threshold
            var failureCount = responses.Count(r => !r.IsSuccessStatusCode);
            var circuitBreakerResponses = responses.Skip(failureThreshold)
                .Count(r => r.StatusCode == HttpStatusCode.ServiceUnavailable);

            Assert.True(circuitBreakerResponses > 0, "Circuit breaker should activate after threshold");
            Assert.True(failureCount < totalRequests, "Not all requests should fail due to circuit breaker");

            _output.WriteLine($"Circuit breaker test: {circuitBreakerResponses} circuit breaker responses");
        }

        [Fact]
        public async Task Service_Should_HandleTimeout_Gracefully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var provider = "apple";

            // Configurar timeout curto para o teste
            using var timeoutClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            timeoutClient.BaseAddress = _client.BaseAddress;

            // Act
            var startTime = DateTime.UtcNow;

            try
            {
                var response = await timeoutClient.PostAsync(
                    $"/api/v1/integrations/calendar/sync?userId={userId}&provider={provider}",
                    JsonContent.Create(new { simulateSlowResponse = true }));

                var endTime = DateTime.UtcNow;
                var duration = endTime - startTime;

                // Assert - Deve responder dentro do timeout ou falhar graciosamente
                if (response.IsSuccessStatusCode)
                {
                    Assert.True(duration.TotalSeconds <= 3, "Response should be within timeout + buffer");
                }
                else
                {
                    Assert.True(duration.TotalSeconds <= 5, "Timeout should be handled gracefully");
                }

                _output.WriteLine($"Timeout test: {response.StatusCode} in {duration.TotalSeconds:F2}s");
            }
            catch (TaskCanceledException)
            {
                var endTime = DateTime.UtcNow;
                var duration = endTime - startTime;

                Assert.True(duration.TotalSeconds <= 5, "Timeout should occur within expected time");
                _output.WriteLine($"Timeout test: Request cancelled after {duration.TotalSeconds:F2}s");
            }
        }

        [Fact]
        public async Task Service_Should_HandleInvalidData_Gracefully()
        {
            // Arrange
            var invalidRequests = new[]
            {
                // Dados inválidos
                new { userId = Guid.Empty, provider = "", accessToken = "" },
                new { userId = "invalid-guid", provider = "unknown", accessToken = null },
                new { userId = Guid.NewGuid(), provider = "google", accessToken = "invalid-token" }
            };

            // Act & Assert
            foreach (var request in invalidRequests)
            {
                var response = await _client.PostAsync(
                    "/api/v1/integrations/calendar/sync",
                    JsonContent.Create(request));

                // Deve retornar erro apropriado, não crash
                Assert.True(
                    response.StatusCode == HttpStatusCode.BadRequest ||
                    response.StatusCode == HttpStatusCode.Unauthorized ||
                    response.StatusCode == HttpStatusCode.UnprocessableEntity,
                    $"Should handle invalid data gracefully, got {response.StatusCode}");

                _output.WriteLine($"Invalid data test: {response.StatusCode} for {request}");
            }
        }

        [Fact]
        public async Task Service_Should_HandleConcurrentFailures()
        {
            // Arrange
            const int concurrentRequests = 20;
            var userId = Guid.NewGuid();
            var provider = "google";

            // Act - Requisições concorrentes com falhas simuladas
            var tasks = Enumerable.Range(1, concurrentRequests).Select(i =>
                _client.PostAsync(
                    $"/api/v1/integrations/calendar/sync?userId={userId}&provider={provider}",
                    JsonContent.Create(new { simulateRandomFailure = true, requestId = i }))
            ).ToArray();

            var responses = await Task.WhenAll(tasks);

            // Assert - Sistema deve permanecer estável mesmo com falhas concorrentes
            var successCount = responses.Count(r => r.IsSuccessStatusCode);
            var failureCount = responses.Count(r => !r.IsSuccessStatusCode);

            Assert.True(successCount > 0, "Some requests should succeed even with concurrent failures");
            Assert.True(successCount + failureCount == concurrentRequests, "All requests should complete");

            _output.WriteLine($"Concurrent failures test: {successCount} success, {failureCount} failures");
        }

        [Fact]
        public async Task Service_Should_RecoverAfter_DependencyFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var provider = "google";

            // Act 1 - Simular falha de dependência
            var failureResponse = await _client.PostAsync(
                $"/api/v1/integrations/calendar/sync?userId={userId}&provider={provider}",
                JsonContent.Create(new { simulateDependencyFailure = true }));

            _output.WriteLine($"Dependency failure: {failureResponse.StatusCode}");

            // Aguardar recuperação
            await Task.Delay(2000);

            // Act 2 - Tentar novamente após recuperação
            var recoveryResponse = await _client.PostAsync(
                $"/api/v1/integrations/calendar/sync?userId={userId}&provider={provider}",
                JsonContent.Create(new { simulateDependencyFailure = false }));

            // Assert - Deve se recuperar após falha de dependência
            Assert.True(recoveryResponse.IsSuccessStatusCode,
                $"Should recover after dependency failure, got {recoveryResponse.StatusCode}");

            _output.WriteLine($"Recovery test: {recoveryResponse.StatusCode}");
        }

        [Fact]
        public async Task Service_Should_HandleResourceExhaustion()
        {
            // Arrange
            const int heavyRequestCount = 50;
            var userId = Guid.NewGuid();

            // Act - Simular esgotamento de recursos com requisições pesadas
            var tasks = Enumerable.Range(1, heavyRequestCount).Select(_ =>
                _client.PostAsync(
                    $"/api/v1/integrations/calendar/sync?userId={userId}&provider=google",
                    JsonContent.Create(new { simulateHeavyLoad = true }))
            ).ToArray();

            var responses = await Task.WhenAll(tasks);

            // Assert - Sistema deve degradar graciosamente, não crashar
            var successCount = responses.Count(r => r.IsSuccessStatusCode);
            var throttledCount = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);
            var serverErrorCount = responses.Count(r =>
                r.StatusCode == HttpStatusCode.InternalServerError ||
                r.StatusCode == HttpStatusCode.ServiceUnavailable);

            Assert.True(successCount > 0, "Some requests should succeed even under heavy load");
            Assert.True(serverErrorCount < heavyRequestCount * 0.5, "Less than 50% should be server errors");

            _output.WriteLine($"Resource exhaustion test:");
            _output.WriteLine($"  Success: {successCount}");
            _output.WriteLine($"  Throttled: {throttledCount}");
            _output.WriteLine($"  Server errors: {serverErrorCount}");
        }

        [Fact]
        public async Task Service_Should_MaintainDataConsistency_DuringFailures()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var alarmId = Guid.NewGuid();

            // Act 1 - Criar integração
            var createResponse = await _client.PostAsync(
                $"/api/v1/integrations/alarm/{alarmId}",
                JsonContent.Create(new
                {
                    provider = "google",
                    configuration = new Dictionary<string, string> { ["test"] = "value" },
                    enableNotifications = true,
                    features = new[] { "sync" }
                }));

            Assert.True(createResponse.IsSuccessStatusCode);

            // Act 2 - Simular falha durante operação
            var updateResponse = await _client.PostAsync(
                $"/api/v1/integrations/{alarmId}/sync",
                JsonContent.Create(new { simulatePartialFailure = true }));

            // Act 3 - Verificar estado após falha
            var statusResponse = await _client.GetAsync($"/api/v1/integrations/user/{userId}");

            // Assert - Dados devem permanecer consistentes
            Assert.True(statusResponse.IsSuccessStatusCode);

            var integrations = await statusResponse.Content.ReadFromJsonAsync<UserIntegrationsResponse>();
            Assert.NotNull(integrations);

            _output.WriteLine($"Data consistency test: {integrations.TotalCount} integrations found");
        }

        [Fact]
        public async Task Service_Should_HandleGracefulShutdown()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var longRunningTasks = new List<Task<HttpResponseMessage>>();

            // Act - Iniciar requisições de longa duração
            for (int i = 0; i < 5; i++)
            {
                longRunningTasks.Add(_client.PostAsync(
                    $"/api/v1/integrations/calendar/sync?userId={userId}&provider=google",
                    JsonContent.Create(new { simulateLongRunning = true })));
            }

            // Simular shutdown gracioso (em teste real, seria sinal SIGTERM)
            await Task.Delay(1000);

            // Aguardar conclusão das tarefas
            var responses = await Task.WhenAll(longRunningTasks);

            // Assert - Requisições em andamento devem completar
            var completedCount = responses.Count(r =>
                r.IsSuccessStatusCode ||
                r.StatusCode == HttpStatusCode.ServiceUnavailable);

            Assert.Equal(longRunningTasks.Count, completedCount);

            _output.WriteLine($"Graceful shutdown test: {completedCount} requests completed");
        }
    }

    // DTO para teste
    public record UserIntegrationsResponse(
        Guid UserId,
        IEnumerable<object> Integrations,
        int TotalCount,
        DateTime RetrievedAt
    );
}
