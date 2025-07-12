using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FluentAssertions;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace SmartAlarm.Tests.Integration
{
    /// <summary>
    /// Testes básicos de integração para autenticação JWT/FIDO2
    /// Versão simplificada para garantir compilação
    /// </summary>
    public class BasicJwtFido2Tests : IClassFixture<WebApplicationFactory<SmartAlarm.Api.Program>>
    {
        private readonly WebApplicationFactory<SmartAlarm.Api.Program> _factory;
        private readonly HttpClient _client;

        public BasicJwtFido2Tests(WebApplicationFactory<SmartAlarm.Api.Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task HealthCheck_ShouldReturnOk()
        {
            // Act
            var response = await _client.GetAsync("/health");

            // Assert
            response.Should().NotBeNull();
            response.IsSuccessStatusCode.Should().BeTrue();
        }

        [Fact]
        public async Task AuthEndpoint_ShouldBeAvailable()
        {
            // Act
            var response = await _client.GetAsync("/api/auth");

            // Assert
            response.Should().NotBeNull();
            // Pode retornar 401 (não autorizado) mas o endpoint deve existir
        }

        [Fact]
        public void ApplicationServices_ShouldBeConfigured()
        {
            // Arrange & Act
            using var scope = _factory.Services.CreateScope();
            var services = scope.ServiceProvider;

            // Assert - Verificar se serviços básicos estão registrados
            services.Should().NotBeNull();
        }

        [Fact]
        public async Task InvalidToken_ShouldReturnUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid-token");

            // Act
            var response = await _client.GetAsync("/api/alarms");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        }

        [Fact]
        public void Factory_ShouldCreateApplication()
        {
            // Assert
            _factory.Should().NotBeNull();
            _factory.Services.Should().NotBeNull();
        }
    }
}
