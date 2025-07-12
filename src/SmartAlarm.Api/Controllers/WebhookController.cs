using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    public WebhookController(ILogger<WebhookController> logger)
    {
        _logger = logger;
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
    public async Task<ActionResult> RegisterWebhook([FromBody] WebhookRegisterRequest request)
    {
        try
        {
            _logger.LogInformation("Webhook registration requested for URL: {Url}", request.Url);
            
            // TODO: Implementar lógica de registro de webhook
            var response = new
            {
                Id = Guid.NewGuid(),
                Url = request.Url,
                Events = request.Events,
                Secret = Guid.NewGuid().ToString("N")[..32],
                CreatedAt = DateTime.UtcNow
            };

            return Created($"/api/v1/webhooks/{response.Id}", response);
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
