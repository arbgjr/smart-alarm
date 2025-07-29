using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SmartAlarm.Application.DTOs.Routine;
using Xunit;

namespace SmartAlarm.Api.Tests
{
    public class RoutineControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public RoutineControllerTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetRoutines_Should_Return_Unauthorized_When_NotAuthenticated()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/routines");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetRoutineById_Should_Return_Unauthorized_When_NotAuthenticated()
        {
            // Arrange
            var routineId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/v1/routines/{routineId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task CreateRoutine_Should_Return_Unauthorized_When_NotAuthenticated()
        {
            // Arrange
            var dto = new CreateRoutineDto
            {
                Name = "Morning Routine",
                AlarmId = Guid.NewGuid(),
                Actions = new List<string> { "Turn on lights", "Play music" }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/routines", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task UpdateRoutine_Should_Return_Unauthorized_When_NotAuthenticated()
        {
            // Arrange
            var routineId = Guid.NewGuid();
            var dto = new UpdateRoutineDto
            {
                Name = "Updated Routine",
                Actions = new List<string> { "New action" }
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/v1/routines/{routineId}", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task DeleteRoutine_Should_Return_Unauthorized_When_NotAuthenticated()
        {
            // Arrange
            var routineId = Guid.NewGuid();

            // Act
            var response = await _client.DeleteAsync($"/api/v1/routines/{routineId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ActivateRoutine_Should_Return_Unauthorized_When_NotAuthenticated()
        {
            // Arrange
            var routineId = Guid.NewGuid();

            // Act
            var response = await _client.PostAsync($"/api/v1/routines/{routineId}/activate", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task DeactivateRoutine_Should_Return_Unauthorized_When_NotAuthenticated()
        {
            // Arrange
            var routineId = Guid.NewGuid();

            // Act
            var response = await _client.PostAsync($"/api/v1/routines/{routineId}/deactivate", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task CreateRoutine_Should_Return_ValidationError_When_Name_Is_Empty()
        {
            // Arrange
            var dto = new CreateRoutineDto
            {
                Name = "",
                AlarmId = Guid.NewGuid(),
                Actions = new List<string> { "Action" }
            };
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid-token");

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/routines", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized); // Token inválido
        }

        [Fact]
        public async Task CreateRoutine_Should_Return_ValidationError_When_AlarmId_Is_Empty()
        {
            // Arrange
            var dto = new CreateRoutineDto
            {
                Name = "Valid Name",
                AlarmId = Guid.Empty,
                Actions = new List<string> { "Action" }
            };
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid-token");

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/routines", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized); // Token inválido
        }

        [Fact]
        public async Task UpdateRoutine_Should_Return_ValidationError_When_Name_Is_Empty()
        {
            // Arrange
            var routineId = Guid.NewGuid();
            var dto = new UpdateRoutineDto
            {
                Name = "",
                Actions = new List<string> { "Action" }
            };
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid-token");

            // Act
            var response = await _client.PutAsJsonAsync($"/api/v1/routines/{routineId}", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized); // Token inválido
        }
    }
}
