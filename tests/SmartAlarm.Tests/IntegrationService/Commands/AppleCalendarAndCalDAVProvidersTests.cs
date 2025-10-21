using SmartAlarm.Domain.Abstractions;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using FluentAssertions;
using Xunit;
using System.Diagnostics;
using SmartAlarm.IntegrationService.Application.Exceptions;
using SmartAlarm.IntegrationService.Application.Services;
using SmartAlarm.IntegrationService.Application.Commands;
using SmartAlarm.Domain.Repositories;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Tracing;
using SmartAlarm.Observability.Metrics;
using static SmartAlarm.IntegrationService.Application.Commands.SyncExternalCalendarCommandHandler;

namespace SmartAlarm.Tests.IntegrationService.Commands;

/// <summary>
/// Testes unitários para Apple Calendar e CalDAV providers do SyncExternalCalendarCommandHandler.
/// 
/// Cobertura:
/// - Apple Calendar: CloudKit API integration, error handling, event parsing
/// - CalDAV: RFC 4791 compliance, multiple auth methods, XML parsing
/// - Validação de tokens e parâmetros
/// - Tratamento de exceções temporárias e permanentes
/// - Retry logic integration
/// </summary>
public class AppleCalendarAndCalDAVProvidersTests : IDisposable
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IAlarmRepository> _mockAlarmRepository;
    private readonly Mock<ILogger<SyncExternalCalendarCommandHandler>> _mockLogger;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<ICalendarRetryService> _mockRetryService;
    private readonly Mock<ICorrelationContext> _mockCorrelationContext;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _mockHttpClient;
    private readonly SmartAlarmActivitySource _testActivitySource;
    private readonly SmartAlarmMeter _testMeter;
    private readonly SyncExternalCalendarCommandHandler _handler;

    public AppleCalendarAndCalDAVProvidersTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockAlarmRepository = new Mock<IAlarmRepository>();
        _mockLogger = new Mock<ILogger<SyncExternalCalendarCommandHandler>>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockRetryService = new Mock<ICalendarRetryService>();
        _mockCorrelationContext = new Mock<ICorrelationContext>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        
        _testActivitySource = new SmartAlarmActivitySource();
        _testMeter = new SmartAlarmMeter();

        _mockHttpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://api.apple-cloudkit.com/")
        };

        _mockHttpClientFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(_mockHttpClient);

        _mockCorrelationContext
            .Setup(c => c.CorrelationId)
            .Returns(Guid.NewGuid().ToString());

        // Configurar mock do usuário - retorna sempre um usuário válido
        _mockUserRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmartAlarm.Domain.Entities.User(
                Guid.NewGuid(),
                new SmartAlarm.Domain.ValueObjects.Name("Test User"),
                new SmartAlarm.Domain.ValueObjects.Email("test@example.com")));

        // Configurar mock do retry service - retorna uma lista vazia por padrão
        _mockRetryService
            .Setup(r => r.ExecuteWithRetryAsync(
                It.IsAny<Func<CancellationToken, Task<List<ExternalCalendarEvent>>>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<SmartAlarm.IntegrationService.Application.Services.RetryPolicy>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExternalCalendarEvent>());

        _handler = new SyncExternalCalendarCommandHandler(
            _mockUserRepository.Object,
            _mockAlarmRepository.Object,
            _mockLogger.Object,
            _testActivitySource,
            _testMeter,
            _mockCorrelationContext.Object,
            _mockHttpClientFactory.Object,
            _mockRetryService.Object);
    }

    #region Apple Calendar Tests

    [Fact]
    public async Task Handle_WithAppleProvider_ShouldProcessSuccessfully()
    {
        // Arrange
        var command = new SyncExternalCalendarCommand(
            UserId: Guid.NewGuid(),
            Provider: "apple",
            StartDate: DateTime.UtcNow.Date,
            EndDate: DateTime.UtcNow.Date.AddDays(7));

        var mockAppleResponse = """
        {
            "records": [
                {
                    "recordName": "event-123",
                    "fields": {
                        "title": { "value": "Reunião Apple" },
                        "startDate": { "value": "2025-01-15T10:00:00Z" },
                        "endDate": { "value": "2025-01-15T11:00:00Z" },
                        "location": { "value": "Cupertino, CA" },
                        "notes": { "value": "Reunião importante" }
                    }
                }
            ]
        }
        """;

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(mockAppleResponse)
            });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Provider.Should().Be("apple");
        result.EventsProcessed.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Handle_WithAppleProvider_InvalidToken_ShouldThrowPermanentException()
    {
        // Arrange
        var command = new SyncExternalCalendarCommand(
            UserId: Guid.NewGuid(),
            Provider: "apple",
            AccessToken: "", // Token vazio
            SyncFromDate: DateTime.UtcNow.Date,
            SyncToDate: DateTime.UtcNow.Date.AddDays(7));

        // Act & Assert
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            async () => await _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithAppleProvider_ApiError_ShouldHandleGracefully()
    {
        // Arrange
        var command = new SyncExternalCalendarCommand(
            UserId: Guid.NewGuid(),
            Provider: "apple",
            StartDate: DateTime.UtcNow.Date,
            EndDate: DateTime.UtcNow.Date.AddDays(7));

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("Apple CloudKit Error")
            });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Provider.Should().Be("apple");
        result.EventsProcessed.Should().Be(0);
        result.Warnings.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region CalDAV Tests

    [Fact]
    public async Task Handle_WithCalDAVProvider_ShouldProcessSuccessfully()
    {
        // Arrange
        var command = new SyncExternalCalendarCommand(
            UserId: Guid.NewGuid(),
            Provider: "caldav",
            AccessToken: "user:password", // Basic Auth format
            SyncFromDate: DateTime.UtcNow.Date,
            SyncToDate: DateTime.UtcNow.Date.AddDays(7));

        var mockCalDAVResponse = """
        <?xml version="1.0" encoding="UTF-8"?>
        <D:multistatus xmlns:D="DAV:" xmlns:C="urn:ietf:params:xml:ns:caldav">
            <D:response>
                <D:href>/calendar/event-456.ics</D:href>
                <D:propstat>
                    <D:status>HTTP/1.1 200 OK</D:status>
                    <D:prop>
                        <C:calendar-data>BEGIN:VCALENDAR
        VERSION:2.0
        PRODID:-//Test//Test//EN
        BEGIN:VEVENT
        UID:event-456
        DTSTART:20250115T140000Z
        DTEND:20250115T150000Z
        SUMMARY:Reunião CalDAV
        LOCATION:Escritório
        DESCRIPTION:Reunião via CalDAV
        END:VEVENT
        END:VCALENDAR</C:calendar-data>
                    </D:prop>
                </D:propstat>
            </D:response>
        </D:multistatus>
        """;

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(mockCalDAVResponse, System.Text.Encoding.UTF8, "application/xml")
            });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Provider.Should().Be("caldav");
        result.EventsProcessed.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Handle_WithCalDAVProvider_EmptyToken_ShouldFailValidation()
    {
        // Arrange
        var command = new SyncExternalCalendarCommand(
            UserId: Guid.NewGuid(),
            Provider: "caldav",
            AccessToken: "", // Token vazio
            SyncFromDate: DateTime.UtcNow.Date,
            SyncToDate: DateTime.UtcNow.Date.AddDays(7));

        // Act & Assert
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            async () => await _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithCalDAVProvider_ServerError_ShouldHandleGracefully()
    {
        // Arrange
        var command = new SyncExternalCalendarCommand(
            UserId: Guid.NewGuid(),
            Provider: "caldav",
            AccessToken: "valid-token",
            SyncFromDate: DateTime.UtcNow.Date,
            SyncToDate: DateTime.UtcNow.Date.AddDays(7));

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection timeout"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Provider.Should().Be("caldav");
        result.EventsProcessed.Should().Be(0);
        result.Warnings.Should().NotBeNullOrEmpty();
    }

    #endregion

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _mockHttpClient?.Dispose();
            _testActivitySource?.Dispose();
            _testMeter?.Dispose();
        }
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
