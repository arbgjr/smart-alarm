using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SmartAlarm.Application.DTOs.UserHolidayPreference;
using SmartAlarm.Domain.Abstractions;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Domain.Repositories;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartAlarm.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using SmartAlarm.Tests.Factories;

namespace SmartAlarm.Api.Tests.Controllers
{
    /// <summary>
    /// Testes de integração para o UserHolidayPreferencesController.
    /// </summary>
    public class UserHolidayPreferencesControllerIntegrationTests : IClassFixture<TestWebApplicationFactory>, IDisposable
    {
        private readonly TestWebApplicationFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly SmartAlarmDbContext _context;

        public UserHolidayPreferencesControllerIntegrationTests(TestWebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.GetSeededClient();
            _scope = _factory.Services.CreateScope();
            _context = _scope.ServiceProvider.GetRequiredService<SmartAlarmDbContext>();
            
            // Configurar cabeçalho de autorização para todos os testes
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-token");
            
            // Garantir que o banco está criado
            _context.Database.EnsureCreated();
        }

        private async Task EnsureHolidayExistsAsync()
        {
            var existingHoliday = await _context.Holidays.FirstOrDefaultAsync(h => h.Id == TestData.HolidayId);
            if (existingHoliday == null)
            {
                var testHoliday = new Holiday(TestData.HolidayId, DateTime.Today.AddDays(30), "Test Holiday");
                _context.Holidays.Add(testHoliday);
                await _context.SaveChangesAsync();
            }
        }

        [Fact]
        public async Task CreateUserHolidayPreference_WithValidData_ShouldReturnCreated()
        {
            // Arrange - garantir que o Holiday existe
            await EnsureHolidayExistsAsync();
            
            var createDto = new CreateUserHolidayPreferenceDto
            {
                UserId = TestData.UserId,
                HolidayId = TestData.HolidayId,
                Action = HolidayPreferenceAction.Skip,
                IsEnabled = true
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/user-holiday-preferences", createDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            
            var result = await response.Content.ReadFromJsonAsync<UserHolidayPreferenceResponseDto>();
            result.Should().NotBeNull();
            result!.UserId.Should().Be(TestData.UserId);
            result.HolidayId.Should().Be(TestData.HolidayId);
            result.Action.Should().Be(HolidayPreferenceAction.Skip);
            result.IsEnabled.Should().BeTrue();
            result.User.Should().NotBeNull();
            result.Holiday.Should().NotBeNull();

            // Verificar se foi criado no banco
            var preference = await _context.UserHolidayPreferences.FirstOrDefaultAsync(p => p.Id == result.Id);
            preference.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateUserHolidayPreference_WithDelayAction_ShouldIncludeDelayInMinutes()
        {
            // Arrange
            var createDto = new CreateUserHolidayPreferenceDto
            {
                UserId = TestData.UserId,
                HolidayId = TestData.HolidayId,
                Action = HolidayPreferenceAction.Delay,
                DelayInMinutes = 30,
                IsEnabled = true
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/user-holiday-preferences", createDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            
            var result = await response.Content.ReadFromJsonAsync<UserHolidayPreferenceResponseDto>();
            result.Should().NotBeNull();
            result!.Action.Should().Be(HolidayPreferenceAction.Delay);
            result.DelayInMinutes.Should().Be(30);
        }

        [Fact]
        public async Task CreateUserHolidayPreference_WithInvalidUserId_ShouldReturnBadRequest()
        {
            // Arrange
            var createDto = new CreateUserHolidayPreferenceDto
            {
                UserId = Guid.NewGuid(), // User que não existe
                HolidayId = TestData.HolidayId,
                Action = HolidayPreferenceAction.Skip,
                IsEnabled = true
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/user-holiday-preferences", createDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetUserHolidayPreferenceById_WithExistingId_ShouldReturnPreference()
        {
            // Arrange
            var preference = await CreateTestPreference();

            // Act
            var response = await _client.GetAsync($"/api/v1/user-holiday-preferences/{preference.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var result = await response.Content.ReadFromJsonAsync<UserHolidayPreferenceResponseDto>();
            result.Should().NotBeNull();
            result!.Id.Should().Be(preference.Id);
            result.User.Should().NotBeNull();
            result.Holiday.Should().NotBeNull();
        }

        [Fact]
        public async Task GetUserHolidayPreferenceById_WithNonExistentId_ShouldReturnNotFound()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/v1/user-holiday-preferences/{nonExistentId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetUserHolidayPreferencesByUser_WithExistingUser_ShouldReturnPreferences()
        {
            // Arrange
            await CreateTestPreference();
            await CreateTestPreference(); // Criar outra preferência

            // Act
            var response = await _client.GetAsync($"/api/v1/user-holiday-preferences/user/{TestData.UserId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var result = await response.Content.ReadFromJsonAsync<List<UserHolidayPreferenceResponseDto>>();
            result.Should().NotBeNull();
            result!.Count.Should().BeGreaterOrEqualTo(1);
            result.All(p => p.UserId == TestData.UserId).Should().BeTrue();
        }

        [Fact]
        public async Task GetApplicableUserHolidayPreferences_WithValidDate_ShouldReturnApplicablePreferences()
        {
            // Arrange
            await CreateTestPreference();
            var testDate = DateTime.Today.AddDays(30); // Data do feriado de teste

            // Act
            var response = await _client.GetAsync($"/api/v1/user-holiday-preferences/user/{TestData.UserId}/applicable?date={testDate:yyyy-MM-dd}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var result = await response.Content.ReadFromJsonAsync<List<UserHolidayPreferenceResponseDto>>();
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateUserHolidayPreference_WithValidData_ShouldReturnUpdatedPreference()
        {
            // Arrange
            var preference = await CreateTestPreference();
            var updateDto = new UpdateUserHolidayPreferenceDto
            {
                Action = HolidayPreferenceAction.Delay,
                DelayInMinutes = 45,
                IsEnabled = false
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/v1/user-holiday-preferences/{preference.Id}", updateDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var result = await response.Content.ReadFromJsonAsync<UserHolidayPreferenceResponseDto>();
            result.Should().NotBeNull();
            result!.Action.Should().Be(HolidayPreferenceAction.Delay);
            result.DelayInMinutes.Should().Be(45);
            result.IsEnabled.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateUserHolidayPreference_WithNonExistentId_ShouldReturnBadRequest()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            var updateDto = new UpdateUserHolidayPreferenceDto
            {
                Action = HolidayPreferenceAction.Skip,
                IsEnabled = true
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/v1/user-holiday-preferences/{nonExistentId}", updateDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task DeleteUserHolidayPreference_WithExistingId_ShouldReturnNoContent()
        {
            // Arrange
            var preference = await CreateTestPreference();

            // Act
            var response = await _client.DeleteAsync($"/api/v1/user-holiday-preferences/{preference.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verificar se foi removido do banco
            var deletedPreference = await _context.UserHolidayPreferences.FirstOrDefaultAsync(p => p.Id == preference.Id);
            deletedPreference.Should().BeNull();
        }

        [Fact]
        public async Task DeleteUserHolidayPreference_WithNonExistentId_ShouldReturnNotFound()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var response = await _client.DeleteAsync($"/api/v1/user-holiday-preferences/{nonExistentId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest); // Handler lança ArgumentException para ID não encontrado
        }

        private async Task<UserHolidayPreference> CreateTestPreference()
        {
            var preference = new UserHolidayPreference(
                Guid.NewGuid(),
                TestData.UserId,
                TestData.HolidayId,
                true,
                HolidayPreferenceAction.Skip
            );

            _context.UserHolidayPreferences.Add(preference);
            await _context.SaveChangesAsync();
            return preference;
        }

        public void Dispose()
        {
            _context?.Database.EnsureDeleted();
            _context?.Dispose();
            _scope?.Dispose();
            _client?.Dispose();
        }

        private static class TestData
        {
            public static readonly Guid UserId = Guid.Parse("12345678-1234-1234-1234-123456789012");
            public static readonly Guid HolidayId = Guid.Parse("123e4567-e89b-12d3-a456-426614174001");
        }
    }
}
