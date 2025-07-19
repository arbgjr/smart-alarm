using System.Diagnostics;
using System.Net;
using System.Security.Claims;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartAlarm.Application.Webhooks.Commands.CreateWebhook;
using SmartAlarm.Application.Webhooks.Commands.UpdateWebhook;
using SmartAlarm.Application.Webhooks.Commands.DeleteWebhook;
using SmartAlarm.Application.Webhooks.Queries.GetWebhookById;
using SmartAlarm.Application.Webhooks.Queries.GetWebhooksByUserId;
using SmartAlarm.Application.Webhooks.Models;
using SmartAlarm.Observability.Context;
using SmartAlarm.Observability.Metrics;
using SmartAlarm.Observability.Tracing;
using Swashbuckle.AspNetCore.Annotations;

namespace SmartAlarm.Api.Controllers;

/// <summary>
/// Controller para gerenciamento completo de webhooks com CRUD enterprise-grade
/// </summary>
[ApiController]
[Route("api/v1/webhooks")]
[Authorize]
[SwaggerTag("Gerenciamento Completo de Webhooks")]
public class WebhookController : ControllerBase
{
    private readonly ILogger<WebhookController> _logger;
    private readonly IMediator _mediator;
    private readonly SmartAlarmMeter _meter;
    private readonly ICorrelationContext _correlationContext;
    private readonly SmartAlarmActivitySource _activitySource;

    public WebhookController(
        ILogger<WebhookController> logger, 
        IMediator mediator,
        SmartAlarmMeter meter,
        ICorrelationContext correlationContext,
        SmartAlarmActivitySource activitySource)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _meter = meter ?? throw new ArgumentNullException(nameof(meter));
        _correlationContext = correlationContext ?? throw new ArgumentNullException(nameof(correlationContext));
        _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
    }

    /// <summary>
    /// Cria um novo webhook
    /// </summary>
    /// <param name="request">Dados do webhook</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Dados do webhook criado</returns>
    [HttpPost]
    [ProducesResponseType(typeof(WebhookResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [SwaggerOperation(Summary = "Criar webhook", Description = "Cria um novo webhook para receber eventos")]
    public async Task<IActionResult> CreateWebhook(
        [FromBody] CreateWebhookRequest request,
        CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity("WebhookController.CreateWebhook");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            activity?.SetTag("webhook.url", request.Url);
            activity?.SetTag("webhook.events", string.Join(",", request.Events));
            activity?.SetTag("correlation.id", _correlationContext.CorrelationId);

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new ErrorResponse(HttpStatusCode.Unauthorized, "Token inválido", 
                    null, null, _correlationContext.CorrelationId));
            }

            var command = new CreateWebhookCommand
            {
                Url = request.Url,
                Events = request.Events,
                UserId = userId.Value,
                Description = request.Description
            };

            var result = await _mediator.Send(command, cancellationToken);
            
            _meter.WebhookRegistered.Add(1, 
                new KeyValuePair<string, object?>("user_id", userId.ToString()));

            stopwatch.Stop();
            _meter.WebhookCommandDuration.Record(stopwatch.ElapsedMilliseconds,
                new KeyValuePair<string, object?>("operation", "create"),
                new KeyValuePair<string, object?>("controller", "WebhookController"));
            
            activity?.SetStatus(ActivityStatusCode.Ok);
            
            return CreatedAtAction(
                nameof(GetWebhookById), 
                new { id = result.Id }, 
                result
            );
        }
        catch (ValidationException ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            _meter.WebhookRegistrationErrors.Add(1, 
                new KeyValuePair<string, object?>("error_type", "validation"));
            
            _logger.LogWarning("Validation failed for webhook creation: {@Errors}", 
                ex.Errors.Select(e => e.ErrorMessage));
                
            return BadRequest(new ErrorResponse(HttpStatusCode.BadRequest, "Dados inválidos", 
                null, ex.Errors.Select(e => e.ErrorMessage), _correlationContext.CorrelationId));
        }
        catch (InvalidOperationException ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return Conflict(new ErrorResponse(HttpStatusCode.Conflict, ex.Message,
                null, null, _correlationContext.CorrelationId));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            _logger.LogError(ex, "Error creating webhook");
            
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ErrorResponse(HttpStatusCode.InternalServerError, "Erro interno do servidor",
                    correlationId: _correlationContext.CorrelationId));
        }
    }

    /// <summary>
    /// Busca webhook por ID
    /// </summary>
    /// <param name="id">ID do webhook</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Dados do webhook</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WebhookResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [SwaggerOperation(Summary = "Buscar webhook", Description = "Busca um webhook específico por ID")]
    public async Task<IActionResult> GetWebhookById(
        Guid id,
        CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity("WebhookController.GetWebhookById");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            activity?.SetTag("webhook.id", id.ToString());
            activity?.SetTag("correlation.id", _correlationContext.CorrelationId);

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new ErrorResponse(HttpStatusCode.Unauthorized, "Token inválido",
                    correlationId: _correlationContext.CorrelationId));
            }

            var query = new GetWebhookByIdQuery(id, userId.Value);
            var result = await _mediator.Send(query, cancellationToken);

            stopwatch.Stop();
            _meter.WebhookCommandDuration.Record(stopwatch.ElapsedMilliseconds,
                new KeyValuePair<string, object?>("operation", "get_by_id"),
                new KeyValuePair<string, object?>("controller", "WebhookController"));

            if (result == null)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Webhook not found");
                
                return NotFound(new ErrorResponse(HttpStatusCode.NotFound, "Webhook não encontrado",
                    correlationId: _correlationContext.CorrelationId));
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
            return Ok(result);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            _logger.LogError(ex, "Error getting webhook {WebhookId}", id);
            
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ErrorResponse(HttpStatusCode.InternalServerError, "Erro interno do servidor",
                    correlationId: _correlationContext.CorrelationId));
        }
    }

    /// <summary>
    /// Lista webhooks do usuário atual
    /// </summary>
    /// <param name="includeInactive">Incluir webhooks inativos</param>
    /// <param name="page">Página</param>
    /// <param name="pageSize">Tamanho da página</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Lista de webhooks</returns>
    [HttpGet]
    [ProducesResponseType(typeof(WebhookListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [SwaggerOperation(Summary = "Listar webhooks", Description = "Lista todos os webhooks do usuário atual")]
    public async Task<IActionResult> GetWebhooks(
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("WebhookController.GetWebhooks");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            activity?.SetTag("include.inactive", includeInactive.ToString());
            activity?.SetTag("pagination.page", page.ToString());
            activity?.SetTag("pagination.pageSize", pageSize.ToString());
            activity?.SetTag("correlation.id", _correlationContext.CorrelationId);

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new ErrorResponse(HttpStatusCode.Unauthorized, "Token inválido",
                    correlationId: _correlationContext.CorrelationId));
            }

            var query = new GetWebhooksByUserIdQuery(userId.Value, includeInactive, page, pageSize);
            var result = await _mediator.Send(query, cancellationToken);

            stopwatch.Stop();
            _meter.WebhookCommandDuration.Record(stopwatch.ElapsedMilliseconds,
                new KeyValuePair<string, object?>("operation", "get_by_user"),
                new KeyValuePair<string, object?>("controller", "WebhookController"));

            activity?.SetStatus(ActivityStatusCode.Ok);
            activity?.SetTag("result.count", result.TotalCount.ToString());

            return Ok(result);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            _logger.LogError(ex, "Error getting webhooks for user");
            
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ErrorResponse(HttpStatusCode.InternalServerError, "Erro interno do servidor",
                    correlationId: _correlationContext.CorrelationId));
        }
    }

    /// <summary>
    /// Atualiza um webhook
    /// </summary>
    /// <param name="id">ID do webhook</param>
    /// <param name="request">Dados para atualização</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Dados do webhook atualizado</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(WebhookResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [SwaggerOperation(Summary = "Atualizar webhook", Description = "Atualiza um webhook existente")]
    public async Task<IActionResult> UpdateWebhook(
        Guid id,
        [FromBody] UpdateWebhookRequest request,
        CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity("WebhookController.UpdateWebhook");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            activity?.SetTag("webhook.id", id.ToString());
            activity?.SetTag("correlation.id", _correlationContext.CorrelationId);

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new ErrorResponse(HttpStatusCode.Unauthorized, "Token inválido",
                    correlationId: _correlationContext.CorrelationId));
            }

            var command = new UpdateWebhookCommand
            {
                Id = id,
                UserId = userId.Value,
                Url = request.Url,
                Events = request.Events,
                IsActive = request.IsActive,
                Description = request.Description
            };

            var result = await _mediator.Send(command, cancellationToken);

            stopwatch.Stop();
            _meter.WebhookCommandDuration.Record(stopwatch.ElapsedMilliseconds,
                new KeyValuePair<string, object?>("operation", "update"),
                new KeyValuePair<string, object?>("controller", "WebhookController"));

            activity?.SetStatus(ActivityStatusCode.Ok);

            return Ok(result);
        }
        catch (ValidationException ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            _logger.LogWarning("Validation failed for webhook update: {@Errors}", 
                ex.Errors.Select(e => e.ErrorMessage));
                
            return BadRequest(new ErrorResponse(HttpStatusCode.BadRequest, "Dados inválidos", 
                null, ex.Errors.Select(e => e.ErrorMessage), _correlationContext.CorrelationId));
        }
        catch (InvalidOperationException ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return ex.Message.Contains("não encontrado") 
                ? NotFound(new ErrorResponse(HttpStatusCode.NotFound, ex.Message,
                    correlationId: _correlationContext.CorrelationId))
                : Conflict(new ErrorResponse(HttpStatusCode.Conflict, ex.Message,
                    correlationId: _correlationContext.CorrelationId));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            _logger.LogError(ex, "Error updating webhook {WebhookId}", id);
            
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ErrorResponse(HttpStatusCode.InternalServerError, "Erro interno do servidor",
                    correlationId: _correlationContext.CorrelationId));
        }
    }

    /// <summary>
    /// Deleta um webhook
    /// </summary>
    /// <param name="id">ID do webhook</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Status da operação</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [SwaggerOperation(Summary = "Deletar webhook", Description = "Remove um webhook existente")]
    public async Task<IActionResult> DeleteWebhook(
        Guid id,
        CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity("WebhookController.DeleteWebhook");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            activity?.SetTag("webhook.id", id.ToString());
            activity?.SetTag("correlation.id", _correlationContext.CorrelationId);

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new ErrorResponse(HttpStatusCode.Unauthorized, "Token inválido",
                    correlationId: _correlationContext.CorrelationId));
            }

            var command = new DeleteWebhookCommand(id, userId.Value);
            var deleted = await _mediator.Send(command, cancellationToken);

            stopwatch.Stop();
            _meter.WebhookCommandDuration.Record(stopwatch.ElapsedMilliseconds,
                new KeyValuePair<string, object?>("operation", "delete"),
                new KeyValuePair<string, object?>("controller", "WebhookController"));

            if (!deleted)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Webhook not found");
                
                return NotFound(new ErrorResponse(HttpStatusCode.NotFound, "Webhook não encontrado",
                    correlationId: _correlationContext.CorrelationId));
            }

            activity?.SetStatus(ActivityStatusCode.Ok);

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return Forbid();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            _logger.LogError(ex, "Error deleting webhook {WebhookId}", id);
            
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ErrorResponse(HttpStatusCode.InternalServerError, "Erro interno do servidor",
                    correlationId: _correlationContext.CorrelationId));
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Invalid or missing user ID in token");
            return null;
        }

        return userId;
    }
}

/// <summary>
/// Dados para registro de webhook
/// </summary>
public class WebhookRegisterRequest
{
    public string Url { get; set; } = string.Empty;
    public string[] Events { get; set; } = Array.Empty<string>();
}
