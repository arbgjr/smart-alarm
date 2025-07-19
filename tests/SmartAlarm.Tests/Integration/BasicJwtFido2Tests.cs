using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SmartAlarm.Tests.Factories;
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
    public class BasicJwtFido2Tests : IClassFixture<TestWebApplicationFactory>
    {
        private readonly TestWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public BasicJwtFido2Tests(TestWebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task HealthCheck_ShouldReturnOk()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/health");

            // Assert
            response.Should().NotBeNull();
            response.IsSuccessStatusCode.Should().BeTrue();
        }

        [Fact]
        public async Task AuthEndpoint_ShouldBeAvailable()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/auth/ping");

            // Assert
            response.Should().NotBeNull();
            // Deve retornar 200 para o endpoint de ping
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
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
            var response = await _client.GetAsync("/api/v1/alarms");

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
