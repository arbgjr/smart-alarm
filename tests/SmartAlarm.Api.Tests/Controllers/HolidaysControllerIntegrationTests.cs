using SmartAlarm.Domain.Abstractions;
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
using SmartAlarm.Application.DTOs.Holiday;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Domain.Entities;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartAlarm.Infrastructure.Data;
using SmartAlarm.Infrastructure.Repositories.EntityFramework;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

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
                // Configurar ambiente de teste
                builder.UseEnvironment("Testing");
                
                builder.ConfigureServices(services =>
                {
                    // Como o Program.cs já evita registrar infraestrutura em "Testing",
                    // vamos apenas adicionar os serviços necessários para os testes
                    
                    // Verificar se deve usar PostgreSQL real (quando rodando em container)
                    var useRealDatabase = Environment.GetEnvironmentVariable("POSTGRES_HOST") == "postgres" &&
                                         !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("POSTGRES_USER"));
                    
                    // Log para debug - mostrar qual banco está sendo usado
                    var logMessage = useRealDatabase ? "🐘 Usando PostgreSQL REAL do container" : "💾 Usando InMemory database (fallback)";
                    Console.WriteLine($"[TEST CONFIG] {logMessage}");
                    
                    if (useRealDatabase)
                    {
                        // Usar PostgreSQL real do container para testes de integração
                        var postgresHost = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
                        var postgresPort = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
                        var postgresUser = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "smartalarm";
                        var postgresPassword = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "smartalarm123";
                        var postgresDb = $"{Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "smartalarm"}_test_{Guid.NewGuid():N}";
                        
                        var connectionString = $"Host={postgresHost};Port={postgresPort};Database={postgresDb};Username={postgresUser};Password={postgresPassword}";
                        Console.WriteLine($"[TEST CONFIG] 🔗 Connection: Host={postgresHost}, Database={postgresDb}");
                        
                        services.AddDbContext<SmartAlarmDbContext>(options =>
                        {
                            options.UseNpgsql(connectionString);
                            options.EnableSensitiveDataLogging();
                            options.EnableDetailedErrors();
                        });
                    }
                    else
                    {
                        // Fallback para InMemory quando PostgreSQL não está disponível
                        var dbName = $"HolidayTestDb_{Guid.NewGuid()}";
                        Console.WriteLine($"[TEST CONFIG] 💾 InMemory Database: {dbName}");
                        
                        services.AddDbContext<SmartAlarmDbContext>(options =>
                        {
                            options.UseInMemoryDatabase(dbName);
                            options.EnableSensitiveDataLogging();
                            options.EnableDetailedErrors();
                        });
                    }
                    
                    // Registrar os serviços necessários
                    services.AddScoped<IHolidayRepository, EfHolidayRepository>();
                    services.AddScoped<IUnitOfWork, EfUnitOfWork>();
                    
                    // Desabilitar autenticação para testes
                    services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthenticationSchemeHandler>(
                            "Test", options => { });
                    services.AddAuthorization(options =>
                    {
                        options.DefaultPolicy = new AuthorizationPolicyBuilder("Test")
                            .RequireAssertion(_ => true)
                            .Build();
                    });
                });
            });

            _client = _factory.CreateClient();
            _scope = _factory.Services.CreateScope();
            _context = _scope.ServiceProvider.GetRequiredService<SmartAlarmDbContext>();

            // Garantir que o banco está criado
            _context.Database.EnsureCreated();
            
            // Limpar dados existentes para garantir isolamento entre testes
            // Nota: CleanDatabase() será chamado de forma síncrona, sem await no construtor
            CleanDatabase().GetAwaiter().GetResult();
        }

        private async Task CleanDatabase()
        {
            // Limpar todas as tabelas para garantir isolamento entre testes
            if (_context.Holidays.Any())
            {
                _context.Holidays.RemoveRange(_context.Holidays);
                await _context.SaveChangesAsync();
            }
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

    /// <summary>
    /// Handler de autenticação para testes que sempre permite acesso.
    /// </summary>
    public class TestAuthenticationSchemeHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthenticationSchemeHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "TestUser"),
                new Claim(ClaimTypes.NameIdentifier, "123"),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");
            
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
