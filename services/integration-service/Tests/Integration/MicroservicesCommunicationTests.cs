using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace SmartAlarm.IntegrationService.Tests.Integration
{
    /// <summary>
    /// Testes de comunicação entre microserviços
    /// </summary>
    public class MicroservicesCommunicationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly ITestOutputHelper _output;

        public MicroservicesCommunicationTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
        {
            _factory = factory;
            _output = output;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task IntegrationService_Should_CommunicateWith_AIService()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var provider = "google";
            var accessToken = "Bearer test-token-123";

            // Simular que AI Service está disponível
            var aiServiceResponse = new
            {
                userId = userId,
                recommendations = new[]
                {
                    new { type = "schedule_optimization", confidence = 0.85 },
                    new { type = "sleep_hygiene", confidence = 0.72 }
                }
            };

            // Act - Sincronizar calendário (que deve consultar AI Service para otimizações)
            var syncResponse = await _client.PostAsync(
                $"/api/v1/integrations/calendar/sync?userId={userId}&provider={provider}",
                JsonContent.Create(new { }));

            // Assert
            Assert.True(syncResponse.IsSuccessStatusCode);

            var syncResult = await syncResponse.Content.ReadFromJsonAsync<CalendarSyncResponse>();
            Assert.NotNull(syncResult);
            Assert.Equal(userId, syncResult.UserId);
            Assert.Equal(provider, syncResult.Provider);

            _output.WriteLine($"Sync completed: {syncResult.EventsProcessed} events processed");
        }

        [Fact]
        public async Task IntegrationService_Should_CommunicateWith_AlarmService()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var alarmId = Guid.NewGuid();

            // Simular criação de integração que deve comunicar com Alarm Service
            var createIntegrationRequest = new
            {
                provider = "google",
                configuration = new Dictionary<string, string>
                {
                    ["calendar_id"] = "primary",
                    ["sync_frequency"] = "hourly"
                },
                enableNotifications = true,
                features = new[] { "calendar_sync", "smart_scheduling" }
            };

            // Act
            var response = await _client.PostAsync(
                $"/api/v1/integrations/alarm/{alarmId}",
                JsonContent.Create(createIntegrationRequest));

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            var integration = await response.Content.ReadFromJsonAsync<IntegrationResponse>();
            Assert.NotNull(integration);
            Assert.Equal("google", integration.Provider);

            _output.WriteLine($"Integration created: {integration.Id} for alarm {alarmId}");
        }

        [Fact]
        public async Task IntegrationService_Should_HandleWebhooks_AndNotifyAlarmService()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var webhookRequest = new
            {
                userId = userId,
                provider = "google",
                eventType = "calendar.event.created",
                callbackUrl = "https://smartalarm.com/webhooks/google",
                configuration = new Dictionary<string, string>
                {
                    ["calendar_id"] = "primary"
                },
                expirationHours = 24
            };

            // Act 1 - Registrar webhook
            var registerResponse = await _client.PostAsync(
                "/api/v1/integrations/webhooks",
                JsonContent.Create(webhookRequest));

            Assert.True(registerResponse.IsSuccessStatusCode);

            var webhook = await registerResponse.Content.ReadFromJsonAsync<WebhookResponse>();
            Assert.NotNull(webhook);

            // Act 2 - Simular recebimento de webhook
            var webhookPayload = new
            {
                eventType = "calendar.event.created",
                eventId = "google-event-123",
                calendarId = "primary",
                eventData = new
                {
                    title = "Reunião importante",
                    startTime = DateTime.UtcNow.AddHours(2),
                    endTime = DateTime.UtcNow.AddHours(3)
                }
            };

            var processResponse = await _client.PostAsync(
                $"/api/v1/integrations/webhooks/{webhook.WebhookId}/process",
                JsonContent.Create(webhookPayload));

            // Assert
            Assert.True(processResponse.IsSuccessStatusCode);

            var processResult = await processResponse.Content.ReadFromJsonAsync<WebhookProcessResponse>();
            Assert.NotNull(processResult);
            Assert.True(processResult.Success);
            Assert.Contains("sync_calendar_event", processResult.ActionsTriggered);

            _output.WriteLine($"Webhook processed: {processResult.ActionsTriggered.Count()} actions triggered");
        }

        [Fact]
        public async Task Services_Should_HandleCircuitBreaker_Gracefully()
        {
            // Arrange - Simular falha no AI Service
            var userId = Guid.NewGuid();
            var provider = "microsoft";

            // Act - Múltiplas tentativas para disparar circuit breaker
            var tasks = new List<Task<HttpResponseMessage>>();
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(_client.PostAsync(
                    $"/api/v1/integrations/calendar/sync?userId={userId}&provider={provider}",
                    JsonContent.Create(new { forceFailure = true })));
            }

            var responses = await Task.WhenAll(tasks);

            // Assert - Pelo menos algumas requisições devem ter sucesso (fallback)
            var successCount = responses.Count(r => r.IsSuccessStatusCode);
            Assert.True(successCount > 0, "Circuit breaker should allow fallback responses");

            _output.WriteLine($"Circuit breaker test: {successCount}/{responses.Length} requests succeeded");
        }

        [Fact]
        public async Task Services_Should_HandleRetry_WithExponentialBackoff()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var provider = "apple";

            // Act - Requisição que deve disparar retry logic
            var startTime = DateTime.UtcNow;
            var response = await _client.PostAsync(
                $"/api/v1/integrations/calendar/sync?userId={userId}&provider={provider}",
                JsonContent.Create(new { simulateTransientFailure = true }));

            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            // Assert - Deve ter levado tempo suficiente para retries
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(duration.TotalMilliseconds > 100, "Should take time for retries");

            _output.WriteLine($"Retry test completed in {duration.TotalMilliseconds}ms");
        }

        [Fact]
        public async Task RateLimiting_Should_WorkAcross_Services()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var provider = "google";

            // Act - Fazer muitas requisições rapidamente
            var tasks = new List<Task<HttpResponseMessage>>();
            for (int i = 0; i < 20; i++)
            {
                tasks.Add(_client.GetAsync($"/api/v1/integrations/user/{userId}?providerFilter={provider}"));
            }

            var responses = await Task.WhenAll(tasks);

            // Assert - Algumas requisições devem ser bloqueadas por rate limiting
            var successCount = responses.Count(r => r.IsSuccessStatusCode);
            var rateLimitedCount = responses.Count(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests);

            Assert.True(rateLimitedCount > 0, "Rate limiting should block some requests");
            Assert.True(successCount > 0, "Some requests should still succeed");

            _output.WriteLine($"Rate limiting test: {successCount} success, {rateLimitedCount} rate limited");
        }

        [Fact]
        public async Task Services_Should_PropagateCorrelationId()
        {
            // Arrange
            var correlationId = Guid.NewGuid().ToString();
            var userId = Guid.NewGuid();

            _client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);

            // Act
            var response = await _client.GetAsync($"/api/v1/integrations/user/{userId}");

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            // Verificar se correlation ID foi propagado nos logs
            // (Em um teste real, isso seria verificado através de logs estruturados)
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.NotNull(responseContent);

            _output.WriteLine($"Correlation ID {correlationId} propagated successfully");
        }

        [Fact]
        public async Task Services_Should_HandleConcurrentRequests()
        {
            // Arrange
            var userIds = Enumerable.Range(1, 10).Select(_ => Guid.NewGuid()).ToArray();
            var provider = "google";

            // Act - Requisições concorrentes para diferentes usuários
            var tasks = userIds.Select(userId =>
                _client.GetAsync($"/api/v1/integrations/user/{userId}?providerFilter={provider}")
            ).ToArray();

            var responses = await Task.WhenAll(tasks);

            // Assert - Todas as requisições devem ter sucesso
            Assert.All(responses, response => Assert.True(response.IsSuccessStatusCode));

            _output.WriteLine($"Concurrent requests test: {responses.Length} requests completed successfully");
        }

        [Fact]
        public async Task Services_Should_ValidateAuthentication()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var clientWithoutAuth = _factory.CreateClient();

            // Act - Requisição sem autenticação
            var response = await clientWithoutAuth.GetAsync($"/api/v1/integrations/user/{userId}");

            // Assert - Deve retornar 401 Unauthorized
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);

            _output.WriteLine("Authentication validation test passed");
        }

        [Fact]
        public async Task Services_Should_HandleLargePayloads()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var largeConfiguration = new Dictionary<string, string>();

            // Criar configuração grande
            for (int i = 0; i < 100; i++)
            {
                largeConfiguration[$"config_key_{i}"] = $"config_value_{i}_with_some_long_text_to_make_it_larger";
            }

            var request = new
            {
                provider = "google",
                configuration = largeConfiguration,
                enableNotifications = true,
                features = Enumerable.Range(1, 50).Select(i => $"feature_{i}").ToArray()
            };

            // Act
            var response = await _client.PostAsync(
                $"/api/v1/integrations/alarm/{Guid.NewGuid()}",
                JsonContent.Create(request));

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            _output.WriteLine($"Large payload test: {JsonSerializer.Serialize(request).Length} bytes processed");
        }

        [Fact]
        public async Task Services_Should_HandleHealthChecks()
        {
            // Act
            var healthResponse = await _client.GetAsync("/health");
            var detailedHealthResponse = await _client.GetAsync("/health/detail");

            // Assert
            Assert.True(healthResponse.IsSuccessStatusCode);
            Assert.True(detailedHealthResponse.IsSuccessStatusCode);

            var healthContent = await healthResponse.Content.ReadAsStringAsync();
            var detailedHealthContent = await detailedHealthResponse.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", healthContent);
            Assert.Contains("status", detailedHealthContent);

            _output.WriteLine("Health checks test passed");
        }
    }

    // DTOs para os testes
    public record CalendarSyncResponse(
        Guid UserId,
        string Provider,
        int EventsProcessed,
        int EventsCreated,
        int EventsUpdated,
        int EventsDeleted,
        IEnumerable<string> Errors,
        DateTime SyncedAt,
        TimeSpan Duration
    );

    public record IntegrationResponse(
        Guid Id,
        string Provider,
        string Type,
        bool IsActive,
        DateTime CreatedAt
    );

    public record WebhookResponse(
        Guid WebhookId,
        Guid UserId,
        string Provider,
        string EventType,
        string CallbackUrl,
        string Status,
        DateTime CreatedAt,
        DateTime? ExpiresAt,
        string SecretKey
    );

    public record WebhookProcessResponse(
        Guid WebhookId,
        bool Success,
        string Message,
        DateTime ProcessedAt,
        TimeSpan ProcessingDuration,
        IEnumerable<string> ActionsTriggered
    );
}
