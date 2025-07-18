using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartAlarm.Api;
using SmartAlarm.Application.Webhooks.Models;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Infrastructure.Repositories;
using Xunit;

namespace SmartAlarm.Api.Tests.Controllers
{
    /// <summary>
    /// Testes de integração enterprise-grade para WebhookController
    /// </summary>
    public class WebhookControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly Guid _testUserId = Guid.NewGuid();

        public WebhookControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Substituir repository por implementação em memória para testes
                    services.RemoveAll<IWebhookRepository>();
                    services.AddSingleton<IWebhookRepository, InMemoryWebhookRepository>();

                    // Configurar autenticação de teste
                    services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthenticationSchemeHandler>(
                            "Test", options => { });
                });
            });

            _client = _factory.CreateClient();
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Test");
        }

        [Fact]
        public async Task CreateWebhook_ValidRequest_ReturnsCreatedWithLocation()
        {
            // Arrange
            var request = new CreateWebhookRequest
            {
                Url = "https://api.example.com/webhook",
                Events = new[] { "alarm.created", "alarm.triggered" },
                Description = "Integration test webhook"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/webhooks", request);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(response.Headers.Location);

            var webhook = await response.Content.ReadFromJsonAsync<WebhookResponse>();
            Assert.NotNull(webhook);
            Assert.Equal(request.Url, webhook.Url);
            Assert.Equal(request.Events, webhook.Events);
            Assert.True(webhook.IsActive);
            Assert.NotEmpty(webhook.Secret);
        }

        [Fact]
        public async Task CreateWebhook_InvalidUrl_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateWebhookRequest
            {
                Url = "invalid-url",
                Events = new[] { "alarm.created" }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/webhooks", request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            Assert.NotNull(errorResponse);
            Assert.Equal(400, errorResponse.StatusCode);
            Assert.Equal("Dados inválidos", errorResponse.Message);
            Assert.NotNull(errorResponse.Errors);
        }

        [Fact]
        public async Task CreateWebhook_DuplicateUrl_ReturnsConflict()
        {
            // Arrange
            var request = new CreateWebhookRequest
            {
                Url = "https://api.example.com/duplicate-webhook",
                Events = new[] { "alarm.created" }
            };

            // Criar primeiro webhook
            await _client.PostAsJsonAsync("/api/v1/webhooks", request);

            // Act - Tentar criar webhook com mesma URL
            var response = await _client.PostAsJsonAsync("/api/v1/webhooks", request);

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

            var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            Assert.NotNull(errorResponse);
            Assert.Equal(409, errorResponse.StatusCode);
        }

        [Fact]
        public async Task GetWebhookById_ExistingWebhook_ReturnsOk()
        {
            // Arrange
            var createRequest = new CreateWebhookRequest
            {
                Url = "https://api.example.com/get-test-webhook",
                Events = new[] { "alarm.created" }
            };

            var createResponse = await _client.PostAsJsonAsync("/api/v1/webhooks", createRequest);
            var createdWebhook = await createResponse.Content.ReadFromJsonAsync<WebhookResponse>();

            // Act
            var response = await _client.GetAsync($"/api/v1/webhooks/{createdWebhook!.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var webhook = await response.Content.ReadFromJsonAsync<WebhookResponse>();
            Assert.NotNull(webhook);
            Assert.Equal(createdWebhook.Id, webhook.Id);
            Assert.Equal(createRequest.Url, webhook.Url);
        }

        [Fact]
        public async Task GetWebhookById_NonExistentWebhook_ReturnsNotFound()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/v1/webhooks/{nonExistentId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            Assert.NotNull(errorResponse);
            Assert.Equal(404, errorResponse.StatusCode);
        }

        [Fact]
        public async Task GetWebhooks_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            // Criar múltiplos webhooks
            for (int i = 1; i <= 15; i++)
            {
                var request = new CreateWebhookRequest
                {
                    Url = $"https://api.example.com/webhook-{i}",
                    Events = new[] { "alarm.created" }
                };
                await _client.PostAsJsonAsync("/api/v1/webhooks", request);
            }

            // Act
            var response = await _client.GetAsync("/api/v1/webhooks?page=2&pageSize=10");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var webhookList = await response.Content.ReadFromJsonAsync<WebhookListResponse>();
            Assert.NotNull(webhookList);
            Assert.Equal(2, webhookList.Page);
            Assert.Equal(10, webhookList.PageSize);
            Assert.True(webhookList.TotalCount >= 15);
            Assert.True(webhookList.HasPreviousPage);
            Assert.Equal(5, webhookList.Webhooks.Count()); // 15 total - 10 primeira página = 5 segunda página
        }

        [Fact]
        public async Task UpdateWebhook_ValidRequest_ReturnsOk()
        {
            // Arrange
            var createRequest = new CreateWebhookRequest
            {
                Url = "https://api.example.com/update-test-webhook",
                Events = new[] { "alarm.created" }
            };

            var createResponse = await _client.PostAsJsonAsync("/api/v1/webhooks", createRequest);
            var createdWebhook = await createResponse.Content.ReadFromJsonAsync<WebhookResponse>();

            var updateRequest = new UpdateWebhookRequest
            {
                Url = "https://api.example.com/updated-webhook",
                Events = new[] { "alarm.created", "alarm.triggered" },
                IsActive = false
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/v1/webhooks/{createdWebhook!.Id}", updateRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var updatedWebhook = await response.Content.ReadFromJsonAsync<WebhookResponse>();
            Assert.NotNull(updatedWebhook);
            Assert.Equal(updateRequest.Url, updatedWebhook.Url);
            Assert.Equal(updateRequest.Events, updatedWebhook.Events);
            Assert.Equal(updateRequest.IsActive, updatedWebhook.IsActive);
            Assert.NotNull(updatedWebhook.UpdatedAt);
        }

        [Fact]
        public async Task DeleteWebhook_ExistingWebhook_ReturnsNoContent()
        {
            // Arrange
            var createRequest = new CreateWebhookRequest
            {
                Url = "https://api.example.com/delete-test-webhook",
                Events = new[] { "alarm.created" }
            };

            var createResponse = await _client.PostAsJsonAsync("/api/v1/webhooks", createRequest);
            var createdWebhook = await createResponse.Content.ReadFromJsonAsync<WebhookResponse>();

            // Act
            var response = await _client.DeleteAsync($"/api/v1/webhooks/{createdWebhook!.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Verificar que o webhook foi realmente deletado
            var getResponse = await _client.GetAsync($"/api/v1/webhooks/{createdWebhook.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        [Fact]
        public async Task WebhookCrud_FullWorkflow_WorksCorrectly()
        {
            // 1. Create
            var createRequest = new CreateWebhookRequest
            {
                Url = "https://api.example.com/full-workflow-webhook",
                Events = new[] { "alarm.created" },
                Description = "Full workflow test"
            };

            var createResponse = await _client.PostAsJsonAsync("/api/v1/webhooks", createRequest);
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

            var createdWebhook = await createResponse.Content.ReadFromJsonAsync<WebhookResponse>();
            Assert.NotNull(createdWebhook);

            // 2. Read
            var getResponse = await _client.GetAsync($"/api/v1/webhooks/{createdWebhook.Id}");
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var retrievedWebhook = await getResponse.Content.ReadFromJsonAsync<WebhookResponse>();
            Assert.Equal(createdWebhook.Id, retrievedWebhook!.Id);

            // 3. Update
            var updateRequest = new UpdateWebhookRequest
            {
                Events = new[] { "alarm.created", "alarm.triggered" },
                IsActive = false
            };

            var updateResponse = await _client.PutAsJsonAsync($"/api/v1/webhooks/{createdWebhook.Id}", updateRequest);
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

            // 4. List (verificar se aparece na listagem)
            var listResponse = await _client.GetAsync("/api/v1/webhooks?includeInactive=true");
            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

            var webhookList = await listResponse.Content.ReadFromJsonAsync<WebhookListResponse>();
            Assert.Contains(webhookList!.Webhooks, w => w.Id == createdWebhook.Id);

            // 5. Delete
            var deleteResponse = await _client.DeleteAsync($"/api/v1/webhooks/{createdWebhook.Id}");
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            // 6. Verificar que foi deletado
            var finalGetResponse = await _client.GetAsync($"/api/v1/webhooks/{createdWebhook.Id}");
            Assert.Equal(HttpStatusCode.NotFound, finalGetResponse.StatusCode);
        }

        [Fact]
        public async Task WebhookController_Performance_ResponseTimeUnder200ms()
        {
            // Arrange
            var request = new CreateWebhookRequest
            {
                Url = "https://api.example.com/performance-webhook",
                Events = new[] { "alarm.created" }
            };

            // Act & Assert - Múltiplas operações devem ser rápidas
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var createResponse = await _client.PostAsJsonAsync("/api/v1/webhooks", request);
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

            var webhook = await createResponse.Content.ReadFromJsonAsync<WebhookResponse>();

            var getResponse = await _client.GetAsync($"/api/v1/webhooks/{webhook!.Id}");
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var listResponse = await _client.GetAsync("/api/v1/webhooks");
            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

            stopwatch.Stop();

            // Critério de performance: operações básicas devem completar em menos de 200ms
            Assert.True(stopwatch.ElapsedMilliseconds < 200, 
                $"Performance test failed: {stopwatch.ElapsedMilliseconds}ms > 200ms");
        }
    }
}
