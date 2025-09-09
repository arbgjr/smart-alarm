using SmartAlarm.Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;
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
using Xunit;

namespace SmartAlarm.Api.Tests.Controllers
{
    /// <summary>
    /// Testes de integração básicos para WebhookController
    /// </summary>
    public class WebhookControllerBasicIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public WebhookControllerBasicIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Substituir repository por implementação em memória para testes
                    services.RemoveAll<IWebhookRepository>();
                    services.AddSingleton<IWebhookRepository, TestWebhookRepository>();

                    // Configurar autenticação de teste
                    services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, BasicTestAuthHandler>(
                            "Test", options => { });
                });
            });

            _client = _factory.CreateClient();
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Test");
        }

        [Fact]
        public async Task CreateWebhook_ValidRequest_ReturnsCreated()
        {
            // Arrange
            var request = new CreateWebhookRequest
            {
                Url = "https://api.example.com/webhook",
                Events = new[] { "alarm.created", "alarm.triggered" }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/webhooks", request);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task GetWebhooks_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/webhooks");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    /// <summary>
    /// Implementação simples em memória para testes
    /// </summary>
    public class TestWebhookRepository : IWebhookRepository
    {
        private readonly List<Webhook> _webhooks = new();

        public Task<Webhook> CreateAsync(Webhook webhook)
        {
            webhook.Id = Guid.NewGuid();
            webhook.CreatedAt = DateTime.UtcNow;
            _webhooks.Add(webhook);
            return Task.FromResult(webhook);
        }

        public Task<Webhook?> GetByIdAsync(Guid id)
        {
            var webhook = _webhooks.FirstOrDefault(w => w.Id == id);
            return Task.FromResult(webhook);
        }

        public Task<IEnumerable<Webhook>> GetByUserIdAsync(Guid userId)
        {
            var webhooks = _webhooks.Where(w => w.UserId == userId);
            return Task.FromResult(webhooks);
        }

        public Task<IEnumerable<Webhook>> GetActiveWebhooksAsync()
        {
            var webhooks = _webhooks.Where(w => w.IsActive);
            return Task.FromResult(webhooks);
        }

        public Task<Webhook> UpdateAsync(Webhook webhook)
        {
            var existing = _webhooks.FirstOrDefault(w => w.Id == webhook.Id);
            if (existing != null)
            {
                var index = _webhooks.IndexOf(existing);
                _webhooks[index] = webhook;
            }
            return Task.FromResult(webhook);
        }

        public Task<bool> DeleteAsync(Guid id)
        {
            var webhook = _webhooks.FirstOrDefault(w => w.Id == id);
            if (webhook != null)
            {
                _webhooks.Remove(webhook);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<bool> ExistsAsync(Guid id)
        {
            var exists = _webhooks.Any(w => w.Id == id);
            return Task.FromResult(exists);
        }

        public Task<IEnumerable<Webhook>> GetByEventTypeAsync(string eventType)
        {
            var webhooks = _webhooks.Where(w => w.Events.Contains(eventType));
            return Task.FromResult(webhooks);
        }
    }

    /// <summary>
    /// Handler de autenticação simples para testes
    /// </summary>
    public class BasicTestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public BasicTestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, "Test User"),
            };

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
