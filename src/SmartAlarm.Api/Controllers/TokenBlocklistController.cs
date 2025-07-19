using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartAlarm.Infrastructure.Security;
using SmartAlarm.Api.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace SmartAlarm.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    [SwaggerTag("Gerenciamento de JWT Blocklist")]
    public class TokenBlocklistController : ControllerBase
    {
        private readonly IJwtBlocklistService _blocklistService;
        private readonly ILogger<TokenBlocklistController> _logger;

        public TokenBlocklistController(
            IJwtBlocklistService blocklistService,
            ILogger<TokenBlocklistController> logger)
        {
            _blocklistService = blocklistService;
            _logger = logger;
        }

        [HttpGet("check/{tokenId}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<TokenStatusResponse>> CheckTokenStatus(string tokenId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tokenId))
                {
                    return BadRequest(new ErrorResponse
                    {
                        StatusCode = 400,
                        Title = "Dados inválidos",
                        Detail = "ID do token é obrigatório",
                        Type = "ValidationError",
                        TraceId = HttpContext.TraceIdentifier
                    });
                }

                var isBlocked = await _blocklistService.IsTokenBlockedAsync(tokenId);
                
                return Ok(new TokenStatusResponse
                {
                    TokenId = tokenId,
                    IsBlocked = isBlocked,
                    CheckedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking token status for tokenId {TokenId}", tokenId);
                return StatusCode(500, new ErrorResponse
                {
                    StatusCode = 500,
                    Title = "Erro interno",
                    Detail = "Erro interno do servidor",
                    Type = "SystemError",
                    TraceId = HttpContext.TraceIdentifier
                });
            }
        }
    }

    public class TokenStatusResponse
    {
        public string TokenId { get; set; } = string.Empty;
        public bool IsBlocked { get; set; }
        public DateTime CheckedAt { get; set; }
    }
}
