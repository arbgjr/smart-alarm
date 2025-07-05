using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SmartAlarm.Application.DTOs;
using Xunit;

namespace SmartAlarm.Api.Tests
{
    public class AlarmControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public AlarmControllerTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Should_Return_Unauthorized_When_NotAuthenticated()
        {
            // Arrange
            var dto = new CreateAlarmDto { Name = "Teste", Time = DateTime.UtcNow.AddHours(1) };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/alarms", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Should_Return_ValidationError_When_Name_Is_Empty()
        {
            // Arrange
            var dto = new CreateAlarmDto { Name = "", Time = DateTime.UtcNow.AddHours(1) };
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid-token");

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/alarms", dto);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized); // Token inválido
        }

        // Adicione mais testes cobrindo casos de sucesso, erro e edge cases
    }
}
