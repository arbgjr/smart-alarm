using System;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SmartAlarm.Api.Services;
using SmartAlarm.Application.DTOs.Import;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Infrastructure.Data;
using SmartAlarm.Infrastructure.Repositories;
using Xunit;

namespace SmartAlarm.Api.Tests.Controllers;

public class AlarmImportControllerTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly HttpClient _clientWithoutAuth;
    private readonly IServiceScope _scope;
    private readonly SmartAlarmDbContext _context;
    private readonly Guid _testUserId = Guid.NewGuid();

    public AlarmImportControllerTests(WebApplicationFactory<Program> factory)
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
                
                if (useRealDatabase)
                {
                    // Usar PostgreSQL real do container para testes de integração
                    var postgresHost = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
                    var postgresPort = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
                    var postgresUser = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "smartalarm";
                    var postgresPassword = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "smartalarm123";
                    var postgresDb = $"{Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "smartalarm"}_test_{Guid.NewGuid():N}";
                    
                    var connectionString = $"Host={postgresHost};Port={postgresPort};Database={postgresDb};Username={postgresUser};Password={postgresPassword}";
                    
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
                    var dbName = $"AlarmImportTestDb_{Guid.NewGuid()}";
                    
                    services.AddDbContext<SmartAlarmDbContext>(options =>
                    {
                        options.UseInMemoryDatabase(dbName);
                        options.EnableSensitiveDataLogging();
                        options.EnableDetailedErrors();
                    });
                }
                
                // Registrar os repositórios necessários (apenas os que existem)
                services.AddScoped<IExceptionPeriodRepository, EfExceptionPeriodRepository>();
                services.AddScoped<IUnitOfWork, EfUnitOfWork>();
                
                // Mock do serviço de usuário atual
                services.AddScoped<ICurrentUserService>(provider => 
                {
                    var mock = new Mock<ICurrentUserService>();
                    mock.Setup(x => x.UserId).Returns(_testUserId.ToString());
                    mock.Setup(x => x.IsAuthenticated).Returns(true);
                    return mock.Object;
                });

                // Configurar autenticação fake para testes
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
                services.AddAuthorization(options =>
                {
                    options.DefaultPolicy = new AuthorizationPolicyBuilder("Test").RequireAuthenticatedUser().Build();
                });
            });
        });

        // Client com autenticação para a maioria dos testes
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Test", "token");
        
        // Client sem autenticação para testar endpoints que devem retornar 401
        _clientWithoutAuth = _factory.CreateClient();
        
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<SmartAlarmDbContext>();
        
        // Configurar database
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context?.Database?.EnsureDeleted();
        _context?.Dispose();
        _scope?.Dispose();
        _client?.Dispose();
        _clientWithoutAuth?.Dispose();
    }

    [Fact]
    public async Task ImportAlarms_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var csvContent = "Name,Time,Enabled\nMorning Alarm,07:00,true";
        var formData = CreateMultipartFormData("alarms.csv", csvContent);

        // Act - usar client sem autenticação
        var response = await _clientWithoutAuth.PostAsync("/api/v1/alarms/import", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ImportAlarms_WithoutFile_ShouldReturnBadRequest()
    {
        // Arrange
        var formData = new MultipartFormDataContent();
        formData.Add(new StringContent("false"), "overwriteExisting");

        // Act
        var response = await _client.PostAsync("/api/v1/alarms/import", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ImportAlarms_WithEmptyFile_ShouldReturnBadRequest()
    {
        // Arrange
        var formData = CreateMultipartFormData("empty.csv", "");

        // Act
        var response = await _client.PostAsync("/api/v1/alarms/import", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ImportAlarms_WithLargeFile_ShouldReturnPayloadTooLarge()
    {
        // Arrange
        var largeContent = new string('a', 6 * 1024 * 1024); // 6MB
        var formData = CreateMultipartFormData("large.csv", largeContent);


        // Act
        var response = await _client.PostAsync("/api/v1/alarms/import", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task ImportAlarms_WithUnsupportedFileType_ShouldReturnUnsupportedMediaType()
    {
        // Arrange
        var formData = CreateMultipartFormData("alarms.txt", "Some text content");


        // Act
        var response = await _client.PostAsync("/api/v1/alarms/import", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }

    [Theory]
    [InlineData("alarms.CSV")]
    [InlineData("ALARMS.csv")]
    [InlineData("my-alarms.csv")]
    public async Task ImportAlarms_WithValidCsvExtensions_ShouldAcceptFile(string fileName)
    {
        // Arrange
        var csvContent = "Name,Time,Enabled\nTest Alarm,08:00,true";
        var formData = CreateMultipartFormData(fileName, csvContent);


        // Act
        var response = await _client.PostAsync("/api/v1/alarms/import", formData);

        // Assert
        // Espera-se Unauthorized por causa do token inválido, mas não UnsupportedMediaType
        response.StatusCode.Should().NotBe(HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task ImportAlarms_WithOverwriteFlag_ShouldIncludeFlag()
    {
        // Arrange
        var csvContent = "Name,Time,Enabled\nTest Alarm,08:00,true";
        var formData = CreateMultipartFormData("alarms.csv", csvContent);
        formData.Add(new StringContent("true"), "overwriteExisting");


        // Act
        var response = await _client.PostAsync("/api/v1/alarms/import", formData);

        // Assert
        // Verifica que o parâmetro foi aceito (não erro de binding)
        response.StatusCode.Should().NotBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ImportAlarms_WithValidCsvStructure_ShouldAcceptContent()
    {
        // Arrange
        var csvContent = @"Name,Time,Enabled
Morning Alarm,07:00,true
Evening Alarm,19:00,false
Weekend Alarm,09:00,true";
        var formData = CreateMultipartFormData("alarms.csv", csvContent);


        // Act
        var response = await _client.PostAsync("/api/v1/alarms/import", formData);

        // Assert
        // Deve passar das validações de arquivo (falha apenas na autenticação)
        response.StatusCode.Should().NotBe(HttpStatusCode.UnsupportedMediaType);
        response.StatusCode.Should().NotBe(HttpStatusCode.RequestEntityTooLarge);
        response.StatusCode.Should().NotBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ImportAlarms_FileSizeValidation_ShouldRejectExactly5MBPlus1Byte()
    {
        // Arrange
        var size5MBPlus1 = (5 * 1024 * 1024) + 1;
        var largeContent = new string('a', size5MBPlus1);
        var formData = CreateMultipartFormData("large.csv", largeContent);


        // Act
        var response = await _client.PostAsync("/api/v1/alarms/import", formData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task ImportAlarms_FileSizeValidation_ShouldAcceptExactly5MB()
    {
        // Arrange
        var size5MB = 5 * 1024 * 1024;
        var content = new string('a', size5MB);
        var formData = CreateMultipartFormData("alarms.csv", content);


        // Act
        var response = await _client.PostAsync("/api/v1/alarms/import", formData);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.RequestEntityTooLarge);
    }

    private static MultipartFormDataContent CreateMultipartFormData(string fileName, string content)
    {
        var formData = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        formData.Add(fileContent, "file", fileName);
        formData.Add(new StringContent("false"), "overwriteExisting");
        return formData;
    }
}

/// <summary>
/// Handler de autenticação fake para testes de integração.
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
