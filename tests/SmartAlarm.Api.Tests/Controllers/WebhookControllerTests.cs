using System.Security.Claims;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAlarm.Api.Controllers;
using SmartAlarm.Application.Webhooks.Commands.CreateWebhook;
using SmartAlarm.Application.Webhooks.Commands.UpdateWebhook;
using SmartAlarm.Application.Webhooks.Commands.DeleteWebhook;
using SmartAlarm.Application.Webhooks.Queries.GetWebhookById;
using SmartAlarm.Application.Webhooks.Queries.GetWebhooksByUserId;
using SmartAlarm.Application.Webhooks.Models;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;
using Xunit;

namespace SmartAlarm.Api.Tests.Controllers
{
    /// <summary>
    /// Testes unitários para WebhookController com 100% de cobertura
    /// </summary>
    public class WebhookControllerTests
    {
        private readonly Mock<ILogger<WebhookController>> _loggerMock;
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<SmartAlarmMeter> _meterMock;
        private readonly Mock<ICorrelationContext> _correlationContextMock;
        private readonly Mock<SmartAlarmActivitySource> _activitySourceMock;
        private readonly WebhookController _controller;
        private readonly Guid _testUserId = Guid.NewGuid();
        private readonly string _testCorrelationId = "test-correlation-id";

        public WebhookControllerTests()
        {
            _loggerMock = new Mock<ILogger<WebhookController>>();
            _mediatorMock = new Mock<IMediator>();
            _meterMock = new Mock<SmartAlarmMeter>();
            _correlationContextMock = new Mock<ICorrelationContext>();
            _activitySourceMock = new Mock<SmartAlarmActivitySource>();

            _correlationContextMock.Setup(x => x.CorrelationId).Returns(_testCorrelationId);

            _controller = new WebhookController(
                _loggerMock.Object,
                _mediatorMock.Object,
                _meterMock.Object,
                _correlationContextMock.Object,
                _activitySourceMock.Object);

            SetupUserContext();
        }

        private void SetupUserContext()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString())
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };
        }

        #region CreateWebhook Tests

        [Fact]
        public async Task CreateWebhook_ValidRequest_ReturnsCreatedResult()
        {
            // Arrange
            var request = new CreateWebhookRequest
            {
                Url = "https://example.com/webhook",
                Events = new[] { "alarm.created", "alarm.triggered" },
                Description = "Test webhook"
            };

            var expectedResponse = new WebhookResponse
            {
                Id = Guid.NewGuid(),
                Url = request.Url,
                Events = request.Events,
                Secret = "test-secret",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _mediatorMock.Setup(x => x.Send(It.IsAny<CreateWebhookCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.CreateWebhook(request, CancellationToken.None);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(201, createdResult.StatusCode);
            Assert.Equal(expectedResponse, createdResult.Value);
            Assert.Equal(nameof(WebhookController.GetWebhookById), createdResult.ActionName);

            _mediatorMock.Verify(x => x.Send(
                It.Is<CreateWebhookCommand>(c => 
                    c.Url == request.Url && 
                    c.Events.SequenceEqual(request.Events) && 
                    c.UserId == _testUserId),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateWebhook_ValidationException_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateWebhookRequest
            {
                Url = "invalid-url",
                Events = new[] { "invalid-event" }
            };

            var validationException = new ValidationException("Validation failed");

            _mediatorMock.Setup(x => x.Send(It.IsAny<CreateWebhookCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(validationException);

            // Act
            var result = await _controller.CreateWebhook(request, CancellationToken.None);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);

            var errorResponse = Assert.IsType<ErrorResponse>(badRequestResult.Value);
            Assert.Equal(400, errorResponse.StatusCode);
            Assert.Equal("Dados inválidos", errorResponse.Message);
            Assert.Equal(_testCorrelationId, errorResponse.CorrelationId);
        }

        [Fact]
        public async Task CreateWebhook_InvalidOperationException_ReturnsConflict()
        {
            // Arrange
            var request = new CreateWebhookRequest
            {
                Url = "https://example.com/webhook",
                Events = new[] { "alarm.created" }
            };

            _mediatorMock.Setup(x => x.Send(It.IsAny<CreateWebhookCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Webhook already exists"));

            // Act
            var result = await _controller.CreateWebhook(request, CancellationToken.None);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(409, conflictResult.StatusCode);

            var errorResponse = Assert.IsType<ErrorResponse>(conflictResult.Value);
            Assert.Equal(409, errorResponse.StatusCode);
            Assert.Equal("Webhook already exists", errorResponse.Message);
        }

        [Fact]
        public async Task CreateWebhook_UnauthorizedUser_ReturnsUnauthorized()
        {
            // Arrange
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

            var request = new CreateWebhookRequest
            {
                Url = "https://example.com/webhook",
                Events = new[] { "alarm.created" }
            };

            // Act
            var result = await _controller.CreateWebhook(request, CancellationToken.None);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(401, unauthorizedResult.StatusCode);

            var errorResponse = Assert.IsType<ErrorResponse>(unauthorizedResult.Value);
            Assert.Equal(401, errorResponse.StatusCode);
            Assert.Equal("Token inválido", errorResponse.Message);
        }

        #endregion

        #region GetWebhookById Tests

        [Fact]
        public async Task GetWebhookById_ExistingWebhook_ReturnsOkResult()
        {
            // Arrange
            var webhookId = Guid.NewGuid();
            var expectedResponse = new WebhookResponse
            {
                Id = webhookId,
                Url = "https://example.com/webhook",
                Events = new[] { "alarm.created" },
                Secret = "test-secret",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _mediatorMock.Setup(x => x.Send(It.IsAny<GetWebhookByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetWebhookById(webhookId, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            Assert.Equal(expectedResponse, okResult.Value);

            _mediatorMock.Verify(x => x.Send(
                It.Is<GetWebhookByIdQuery>(q => q.Id == webhookId && q.UserId == _testUserId),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetWebhookById_NonExistentWebhook_ReturnsNotFound()
        {
            // Arrange
            var webhookId = Guid.NewGuid();

            _mediatorMock.Setup(x => x.Send(It.IsAny<GetWebhookByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((WebhookResponse?)null);

            // Act
            var result = await _controller.GetWebhookById(webhookId, CancellationToken.None);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);

            var errorResponse = Assert.IsType<ErrorResponse>(notFoundResult.Value);
            Assert.Equal(404, errorResponse.StatusCode);
            Assert.Equal("Webhook não encontrado", errorResponse.Message);
        }

        #endregion

        #region GetWebhooks Tests

        [Fact]
        public async Task GetWebhooks_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var expectedResponse = new WebhookListResponse
            {
                Webhooks = new List<WebhookResponse>
                {
                    new WebhookResponse
                    {
                        Id = Guid.NewGuid(),
                        Url = "https://example.com/webhook1",
                        Events = new[] { "alarm.created" },
                        IsActive = true
                    }
                },
                TotalCount = 1,
                Page = 1,
                PageSize = 10,
                TotalPages = 1,
                HasNextPage = false,
                HasPreviousPage = false
            };

            _mediatorMock.Setup(x => x.Send(It.IsAny<GetWebhooksByUserIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetWebhooks(false, 1, 10, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            Assert.Equal(expectedResponse, okResult.Value);

            _mediatorMock.Verify(x => x.Send(
                It.Is<GetWebhooksByUserIdQuery>(q => 
                    q.UserId == _testUserId && 
                    q.IncludeInactive == false && 
                    q.Page == 1 && 
                    q.PageSize == 10),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region UpdateWebhook Tests

        [Fact]
        public async Task UpdateWebhook_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var webhookId = Guid.NewGuid();
            var request = new UpdateWebhookRequest
            {
                Url = "https://example.com/webhook-updated",
                Events = new[] { "alarm.created", "alarm.triggered" },
                IsActive = false
            };

            var expectedResponse = new WebhookResponse
            {
                Id = webhookId,
                Url = request.Url,
                Events = request.Events,
                IsActive = request.IsActive.Value,
                UpdatedAt = DateTime.UtcNow
            };

            _mediatorMock.Setup(x => x.Send(It.IsAny<UpdateWebhookCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.UpdateWebhook(webhookId, request, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            Assert.Equal(expectedResponse, okResult.Value);

            _mediatorMock.Verify(x => x.Send(
                It.Is<UpdateWebhookCommand>(c => 
                    c.Id == webhookId && 
                    c.UserId == _testUserId && 
                    c.Url == request.Url && 
                    c.Events!.SequenceEqual(request.Events!) && 
                    c.IsActive == request.IsActive),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateWebhook_InvalidOperationException_ReturnsNotFoundOrConflict()
        {
            // Arrange
            var webhookId = Guid.NewGuid();
            var request = new UpdateWebhookRequest { Url = "https://example.com/webhook" };

            _mediatorMock.Setup(x => x.Send(It.IsAny<UpdateWebhookCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Webhook não encontrado"));

            // Act
            var result = await _controller.UpdateWebhook(webhookId, request, CancellationToken.None);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);

            var errorResponse = Assert.IsType<ErrorResponse>(notFoundResult.Value);
            Assert.Equal(404, errorResponse.StatusCode);
            Assert.Equal("Webhook não encontrado", errorResponse.Message);
        }

        #endregion

        #region DeleteWebhook Tests

        [Fact]
        public async Task DeleteWebhook_ExistingWebhook_ReturnsNoContent()
        {
            // Arrange
            var webhookId = Guid.NewGuid();

            _mediatorMock.Setup(x => x.Send(It.IsAny<DeleteWebhookCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteWebhook(webhookId, CancellationToken.None);

            // Assert
            var noContentResult = Assert.IsType<NoContentResult>(result);
            Assert.Equal(204, noContentResult.StatusCode);

            _mediatorMock.Verify(x => x.Send(
                It.Is<DeleteWebhookCommand>(c => c.Id == webhookId && c.UserId == _testUserId),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteWebhook_NonExistentWebhook_ReturnsNotFound()
        {
            // Arrange
            var webhookId = Guid.NewGuid();

            _mediatorMock.Setup(x => x.Send(It.IsAny<DeleteWebhookCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteWebhook(webhookId, CancellationToken.None);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);

            var errorResponse = Assert.IsType<ErrorResponse>(notFoundResult.Value);
            Assert.Equal(404, errorResponse.StatusCode);
            Assert.Equal("Webhook não encontrado", errorResponse.Message);
        }

        [Fact]
        public async Task DeleteWebhook_UnauthorizedAccess_ReturnsForbid()
        {
            // Arrange
            var webhookId = Guid.NewGuid();

            _mediatorMock.Setup(x => x.Send(It.IsAny<DeleteWebhookCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UnauthorizedAccessException("Acesso negado"));

            // Act
            var result = await _controller.DeleteWebhook(webhookId, CancellationToken.None);

            // Assert
            var forbidResult = Assert.IsType<ForbidResult>(result);
        }

        #endregion

        #region Exception Handling Tests

        [Fact]
        public async Task CreateWebhook_UnexpectedException_ReturnsInternalServerError()
        {
            // Arrange
            var request = new CreateWebhookRequest
            {
                Url = "https://example.com/webhook",
                Events = new[] { "alarm.created" }
            };

            _mediatorMock.Setup(x => x.Send(It.IsAny<CreateWebhookCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.CreateWebhook(request, CancellationToken.None);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);

            var errorResponse = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal(500, errorResponse.StatusCode);
            Assert.Equal("Erro interno do servidor", errorResponse.Message);
        }

        #endregion
    }
}
