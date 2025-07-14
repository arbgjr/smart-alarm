using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SmartAlarm.Application.DTOs.Holiday;
using SmartAlarm.Domain.Abstractions;
using SmartAlarm.Domain.Entities;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartAlarm.Infrastructure.Data;

namespace SmartAlarm.Api.Tests.Controllers
{
    /// <summary>
    /// Testes de integração para o HolidaysController.
    /// </summary>
    public class HolidaysControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly SmartAlarmDbContext _context;

        public HolidaysControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Usar banco em memória para testes
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<SmartAlarmDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<SmartAlarmDbContext>(options =>
                        options.UseInMemoryDatabase("TestDb"));
                });
            });

            _client = _factory.CreateClient();
            _scope = _factory.Services.CreateScope();
            _context = _scope.ServiceProvider.GetRequiredService<SmartAlarmDbContext>();

            // Garantir que o banco está criado
            _context.Database.EnsureCreated();
        }

        [Fact]
        public async Task CreateHoliday_WithValidData_ShouldReturnCreated()
        {
            // Arrange
            var createDto = new CreateHolidayDto
            {
                Date = new DateTime(2024, 12, 25),
                Description = "Natal"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/holidays", createDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            
            var result = await response.Content.ReadFromJsonAsync<HolidayResponseDto>();
            result.Should().NotBeNull();
            result!.Description.Should().Be("Natal");
            result.Date.Should().Be(new DateTime(2024, 12, 25));
        }

        [Fact]
        public async Task CreateHoliday_WithInvalidData_ShouldReturnBadRequest()
        {
            // Arrange
            var createDto = new CreateHolidayDto
            {
                Date = DateTime.MinValue,
                Description = "" // Descrição vazia
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/holidays", createDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetHolidays_ShouldReturnAllHolidays()
        {
            // Arrange
            await SeedTestData();

            // Act
            var response = await _client.GetAsync("/api/v1/holidays");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var result = await response.Content.ReadFromJsonAsync<List<HolidayResponseDto>>();
            result.Should().NotBeNull();
            result!.Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public async Task GetHolidayById_WithExistingId_ShouldReturnHoliday()
        {
            // Arrange
            var holiday = await SeedSingleHoliday();

            // Act
            var response = await _client.GetAsync($"/api/v1/holidays/{holiday.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var result = await response.Content.ReadFromJsonAsync<HolidayResponseDto>();
            result.Should().NotBeNull();
            result!.Id.Should().Be(holiday.Id);
            result.Description.Should().Be(holiday.Description);
        }

        [Fact]
        public async Task GetHolidayById_WithNonExistingId_ShouldReturnNotFound()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/v1/holidays/{nonExistingId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateHoliday_WithValidData_ShouldReturnOk()
        {
            // Arrange
            var holiday = await SeedSingleHoliday();
            var updateDto = new UpdateHolidayDto
            {
                Date = new DateTime(2024, 12, 31),
                Description = "Reveillon"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/v1/holidays/{holiday.Id}", updateDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var result = await response.Content.ReadFromJsonAsync<HolidayResponseDto>();
            result.Should().NotBeNull();
            result!.Description.Should().Be("Reveillon");
            result.Date.Should().Be(new DateTime(2024, 12, 31));
        }

        [Fact]
        public async Task UpdateHoliday_WithNonExistingId_ShouldReturnNotFound()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();
            var updateDto = new UpdateHolidayDto
            {
                Date = new DateTime(2024, 12, 31),
                Description = "Reveillon"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/v1/holidays/{nonExistingId}", updateDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteHoliday_WithExistingId_ShouldReturnNoContent()
        {
            // Arrange
            var holiday = await SeedSingleHoliday();

            // Act
            var response = await _client.DeleteAsync($"/api/v1/holidays/{holiday.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verificar se foi realmente deletado
            var getResponse = await _client.GetAsync($"/api/v1/holidays/{holiday.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteHoliday_WithNonExistingId_ShouldReturnNotFound()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();

            // Act
            var response = await _client.DeleteAsync($"/api/v1/holidays/{nonExistingId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetHolidaysByDate_ShouldReturnHolidaysForSpecificDate()
        {
            // Arrange
            var testDate = new DateTime(2024, 12, 25);
            await SeedHolidayForDate(testDate);

            // Act
            var response = await _client.GetAsync($"/api/v1/holidays/by-date?date={testDate:yyyy-MM-dd}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var result = await response.Content.ReadFromJsonAsync<List<HolidayResponseDto>>();
            result.Should().NotBeNull();
            result!.Should().HaveCountGreaterThan(0);
            result.Should().AllSatisfy(h => h.Date.Date.Should().Be(testDate.Date));
        }

        private async Task SeedTestData()
        {
            if (!await _context.Holidays.AnyAsync())
            {
                var holidays = new[]
                {
                    new Holiday(new DateTime(2024, 1, 1), "Ano Novo"),
                    new Holiday(new DateTime(2024, 4, 21), "Tiradentes"),
                    new Holiday(new DateTime(2024, 12, 25), "Natal")
                };

                _context.Holidays.AddRange(holidays);
                await _context.SaveChangesAsync();
            }
        }

        private async Task<Holiday> SeedSingleHoliday()
        {
            var holiday = new Holiday(new DateTime(2024, 12, 25), "Natal de Teste");
            _context.Holidays.Add(holiday);
            await _context.SaveChangesAsync();
            return holiday;
        }

        private async Task SeedHolidayForDate(DateTime date)
        {
            var holiday = new Holiday(date, $"Feriado de {date:dd/MM}");
            _context.Holidays.Add(holiday);
            await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context?.Dispose();
            _scope?.Dispose();
            _client?.Dispose();
        }
    }
}
