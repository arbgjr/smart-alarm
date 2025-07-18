using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartAlarm.Application.Webhooks.Commands.RegisterWebhook;
using Swashbuckle.AspNetCore.Annotations;

namespace SmartAlarm.Api.Controllers;

/// <summary>
/// Controller para gerenciamento de webhooks
/// </summary>
[ApiController]
[Route("api/v1/webhooks")]
[SwaggerTag("Gerenciamento de Webhooks")]
public class WebhookController : ControllerBase
{
    private readonly ILogger<WebhookController> _logger;
    private readonly IMediator _mediator;

    public WebhookController(ILogger<WebhookController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    /// <summary>
    /// Registra um novo webhook
    /// </summary>
    /// <param name="request">Dados do webhook</param>
    /// <returns>Dados do webhook registrado</returns>
    [HttpPost("register")]
    [Authorize]
    [SwaggerOperation(Summary = "Registrar webhook", Description = "Registra um novo webhook para receber eventos")]
    [SwaggerResponse(201, "Webhook registrado com sucesso")]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult<RegisterWebhookResponse>> RegisterWebhook([FromBody] WebhookRegisterRequest request)
    {
        try
        {
            // Obter ID do usuário do token JWT
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("Invalid or missing user ID in token");
                return Unauthorized(new { Message = "Token inválido" });
            }

            _logger.LogInformation("Webhook registration requested for URL: {Url} by user: {UserId}", 
                request.Url, userId);

            var command = new RegisterWebhookCommand
            {
                Url = request.Url,
                Events = request.Events,
                UserId = userId
            };

            var response = await _mediator.Send(command);

            return Created($"/api/v1/webhooks/{response.Id}", response);
        }
        catch (FluentValidation.ValidationException ex)
        {
            _logger.LogWarning("Validation failed for webhook registration: {@Errors}", 
                ex.Errors.Select(e => e.ErrorMessage));
            return BadRequest(new { 
                Message = "Dados inválidos", 
                Errors = ex.Errors.Select(e => e.ErrorMessage) 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering webhook");
            return StatusCode(500, new { Message = "Erro interno do servidor" });
        }
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
