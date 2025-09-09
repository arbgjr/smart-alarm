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
using SmartAlarm.Application.DTOs.ExceptionPeriod;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Domain.Entities;
using SmartAlarm.Api.Services;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartAlarm.Infrastructure.Data;
using SmartAlarm.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Moq;

namespace SmartAlarm.Api.Tests.Controllers
{
    /// <summary>
    /// Testes de integração para o ExceptionPeriodsController.
    /// </summary>
    public class ExceptionPeriodsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly SmartAlarmDbContext _context;
        private readonly Guid _testUserId = Guid.NewGuid();

        public ExceptionPeriodsControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                // Configurar ambiente de teste
                builder.UseEnvironment("Testing");
                
                builder.ConfigureServices(services =>
                {
                    // Verificar se deve usar PostgreSQL real (quando rodando em container)
                    var useRealDatabase = Environment.GetEnvironmentVariable("POSTGRES_HOST") == "postgres" &&
                                         !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("POSTGRES_USER"));
                    
                    // Log para debug
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
                        var dbName = $"ExceptionPeriodTestDb_{Guid.NewGuid()}";
                        Console.WriteLine($"[TEST CONFIG] 💾 InMemory Database: {dbName}");
                        
                        services.AddDbContext<SmartAlarmDbContext>(options =>
                        {
                            options.UseInMemoryDatabase(dbName);
                            options.EnableSensitiveDataLogging();
                            options.EnableDetailedErrors();
                        });
                    }
                    
                    // Registrar os serviços necessários
                    services.AddScoped<IExceptionPeriodRepository, EfExceptionPeriodRepository>();
                    services.AddScoped<IUnitOfWork, EfUnitOfWork>();
                    
                    // Mock do serviço de usuário atual
                    services.AddScoped<ICurrentUserService>(provider => 
                    {
                        var mock = new Mock<ICurrentUserService>();
                        var userId = Guid.NewGuid().ToString();
                        mock.Setup(x => x.UserId).Returns(userId);
                        mock.Setup(x => x.Email).Returns("test@test.com");
                        mock.Setup(x => x.Roles).Returns(new[] { "User" });
                        mock.Setup(x => x.IsAuthenticated).Returns(true);
                        
                        var claims = new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, userId),
                            new Claim(ClaimTypes.Email, "test@test.com"),
                            new Claim(ClaimTypes.Role, "User")
                        };
                        var identity = new ClaimsIdentity(claims, "Test");
                        var principal = new ClaimsPrincipal(identity);
                        mock.Setup(x => x.Principal).Returns(principal);
                        
                        return mock.Object;
                    });
                    
                    // Configurar autenticação de teste
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
            CleanDatabase().Wait();
        }

        private async Task CleanDatabase()
        {
            try
            {
                _context.ExceptionPeriods.RemoveRange(_context.ExceptionPeriods);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TEST] Erro ao limpar banco: {ex.Message}");
            }
        }

        [Fact]
        public async Task Post_Create_Should_Return_Unauthorized_When_NotAuthenticated()
        {
            // Arrange
            var dto = new CreateExceptionPeriodDto
            {
                Name = "Período de Teste",
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(5),
                Type = ExceptionPeriodType.Vacation,
                Description = "Descrição teste"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/exception-periods", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Post_Create_Should_Return_BadRequest_When_EndDate_Before_StartDate()
        {
            // Arrange
            var dto = new CreateExceptionPeriodDto
            {
                Name = "Período Inválido",
                StartDate = DateTime.Today.AddDays(5),
                EndDate = DateTime.Today.AddDays(1), // Data de fim antes da data de início
                Type = ExceptionPeriodType.Vacation
            };

            // Adicionar header de autorização de teste
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-token");

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/exception-periods", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Post_Create_Should_Return_BadRequest_When_Name_Is_Empty()
        {
            // Arrange
            var dto = new CreateExceptionPeriodDto
            {
                Name = "", // Nome vazio
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(5),
                Type = ExceptionPeriodType.Vacation
            };

            // Adicionar header de autorização de teste
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-token");

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/exception-periods", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Get_List_Should_Return_Unauthorized_When_NotAuthenticated()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/exception-periods");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Get_List_Should_Return_Ok_With_Empty_List_When_No_Periods()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-token");

            // Act
            var response = await _client.GetAsync("/api/v1/exception-periods");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var content = await response.Content.ReadFromJsonAsync<List<ExceptionPeriodDto>>();
            content.Should().NotBeNull();
            content.Should().BeEmpty();
        }

        [Fact]
        public async Task Get_List_Should_Filter_By_Type_When_Type_Parameter_Provided()
        {
            // Arrange
            await SeedTestData();
            
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-token");

            // Act
            var response = await _client.GetAsync("/api/v1/exception-periods?type=Vacation");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var content = await response.Content.ReadFromJsonAsync<List<ExceptionPeriodDto>>();
            content.Should().NotBeNull();
            content.Should().OnlyContain(p => p.Type == ExceptionPeriodType.Vacation);
        }

        [Fact]
        public async Task Get_ById_Should_Return_NotFound_When_Period_Not_Exists()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-token");

            // Act
            var response = await _client.GetAsync($"/api/v1/exception-periods/{nonExistentId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Get_ById_Should_Return_Ok_When_Period_Exists_And_User_Owns_It()
        {
            // Arrange
            var periodId = await SeedSingleTestPeriod();
            
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-token");

            // Act
            var response = await _client.GetAsync($"/api/v1/exception-periods/{periodId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var content = await response.Content.ReadFromJsonAsync<ExceptionPeriodDto>();
            content.Should().NotBeNull();
            content!.Id.Should().Be(periodId);
        }

        [Fact]
        public async Task Put_Update_Should_Return_Unauthorized_When_NotAuthenticated()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new UpdateExceptionPeriodDto
            {
                Name = "Período Atualizado",
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(5),
                Type = ExceptionPeriodType.Travel
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/v1/exception-periods/{id}", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Put_Update_Should_Return_NotFound_When_Period_Not_Exists()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            var dto = new UpdateExceptionPeriodDto
            {
                Name = "Período Atualizado",
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(5),
                Type = ExceptionPeriodType.Travel
            };

            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-token");

            // Act
            var response = await _client.PutAsJsonAsync($"/api/v1/exception-periods/{nonExistentId}", dto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Delete_Should_Return_Unauthorized_When_NotAuthenticated()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var response = await _client.DeleteAsync($"/api/v1/exception-periods/{id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Delete_Should_Return_NotFound_When_Period_Not_Exists()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-token");

            // Act
            var response = await _client.DeleteAsync($"/api/v1/exception-periods/{nonExistentId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Get_ActiveOnDate_Should_Return_Ok_With_Active_Periods()
        {
            // Arrange
            await SeedTestData();
            var targetDate = DateTime.Today.AddDays(3); // Data que está dentro de alguns períodos
            
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-token");

            // Act
            var response = await _client.GetAsync($"/api/v1/exception-periods/active-on/{targetDate:yyyy-MM-dd}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var content = await response.Content.ReadFromJsonAsync<List<ExceptionPeriodDto>>();
            content.Should().NotBeNull();
        }

        private async Task SeedTestData()
        {
            var periods = new[]
            {
                new Domain.Entities.ExceptionPeriod(
                    Guid.NewGuid(),
                    "Férias",
                    DateTime.Today.AddDays(1),
                    DateTime.Today.AddDays(7),
                    ExceptionPeriodType.Vacation,
                    _testUserId,
                    "Período de férias"),
                new Domain.Entities.ExceptionPeriod(
                    Guid.NewGuid(),
                    "Viagem",
                    DateTime.Today.AddDays(10),
                    DateTime.Today.AddDays(15),
                    ExceptionPeriodType.Travel,
                    _testUserId,
                    "Viagem de trabalho"),
                new Domain.Entities.ExceptionPeriod(
                    Guid.NewGuid(),
                    "Feriado",
                    DateTime.Today.AddDays(20),
                    DateTime.Today.AddDays(21),
                    ExceptionPeriodType.Holiday,
                    _testUserId,
                    "Feriado nacional")
            };

            _context.ExceptionPeriods.AddRange(periods);
            await _context.SaveChangesAsync();
        }

        private async Task<Guid> SeedSingleTestPeriod()
        {
            var period = new Domain.Entities.ExceptionPeriod(
                Guid.NewGuid(),
                "Período Único",
                DateTime.Today.AddDays(1),
                DateTime.Today.AddDays(5),
                ExceptionPeriodType.Maintenance,
                _testUserId,
                "Período para teste");

            _context.ExceptionPeriods.Add(period);
            await _context.SaveChangesAsync();
            
            return period.Id;
        }

        public void Dispose()
        {
            try
            {
                CleanDatabase().Wait();
                _context?.Dispose();
                _scope?.Dispose();
                _client?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TEST] Erro durante dispose: {ex.Message}");
            }
        }
    }
}
