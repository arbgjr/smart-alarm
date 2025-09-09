using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using SmartAlarm.Application.Commands.Auth;
using SmartAlarm.Application.DTOs.Auth;
using SmartAlarm.Application.Queries.Auth;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartAlarm.Api.Controllers;

/// <summary>
/// Controller para autenticação JWT/FIDO2
/// Seguindo padrões Clean Architecture e SOLID
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[SwaggerTag("Autenticação JWT/FIDO2")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, ILogger<AuthController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Login tradicional com email e senha
    /// </summary>
    /// <param name="request">Dados de login</param>
    /// <returns>Token JWT e dados do usuário</returns>
    [HttpPost("login")]
    [SwaggerOperation(Summary = "Login tradicional", Description = "Autentica usuário com email e senha")]
    [SwaggerResponse(200, "Login realizado com sucesso", typeof(AuthResponseDto))]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(401, "Credenciais inválidas")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            var command = new LoginCommand(request.Email, request.Password, request.RememberMe);
            var result = await _mediator.Send(command);

            if (!result.Success)
            {
                return Unauthorized(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing login request");
            return StatusCode(500, new { Message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Registro de novo usuário
    /// </summary>
    /// <param name="request">Dados de registro</param>
    /// <returns>Token JWT e dados do usuário</returns>
    [HttpPost("register")]
    [SwaggerOperation(Summary = "Registro de usuário", Description = "Cria nova conta de usuário")]
    [SwaggerResponse(200, "Usuário registrado com sucesso", typeof(AuthResponseDto))]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(409, "Email já existe")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto request)
    {
        try
        {
            var command = new RegisterCommand(request.Name, request.Email, request.Password, request.ConfirmPassword);
            var result = await _mediator.Send(command);

            if (!result.Success)
            {
                if (result.Message?.Contains("já está em uso") == true)
                {
                    return Conflict(result);
                }
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing registration request");
            return StatusCode(500, new { Message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Refresh do token JWT
    /// </summary>
    /// <param name="request">Refresh token</param>
    /// <returns>Novo token JWT</returns>
    [HttpPost("refresh")]
    [SwaggerOperation(Summary = "Refresh token", Description = "Obtém novo token JWT usando refresh token")]
    [SwaggerResponse(200, "Token renovado com sucesso", typeof(AuthResponseDto))]
    [SwaggerResponse(400, "Refresh token inválido")]
    [SwaggerResponse(401, "Refresh token expirado")]
    public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        try
        {
            var command = new RefreshTokenCommand(request.RefreshToken);
            var result = await _mediator.Send(command);

            if (!result.Success)
            {
                return Unauthorized(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refresh token request");
            return StatusCode(500, new { Message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Logout do usuário
    /// </summary>
    /// <param name="request">Token a ser revogado</param>
    /// <returns>Confirmação de logout</returns>
    [HttpPost("logout")]
    [Authorize]
    [SwaggerOperation(Summary = "Logout", Description = "Revoga token JWT atual")]
    [SwaggerResponse(200, "Logout realizado com sucesso")]
    [SwaggerResponse(400, "Token inválido")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult> Logout([FromBody] LogoutRequestDto request)
    {
        try
        {
            var command = new LogoutCommand(request.Token);
            var result = await _mediator.Send(command);

            if (!result)
            {
                return BadRequest(new { Message = "Falha ao realizar logout" });
            }

            return Ok(new { Message = "Logout realizado com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing logout request");
            return StatusCode(500, new { Message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Obtém usuário atual pelo token
    /// </summary>
    /// <returns>Dados do usuário atual</returns>
    [HttpGet("me")]
    [Authorize]
    [SwaggerOperation(Summary = "Usuário atual", Description = "Obtém dados do usuário autenticado")]
    [SwaggerResponse(200, "Dados do usuário", typeof(UserDto))]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        try
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized(new { Message = "Token não fornecido" });
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var query = new GetCurrentUserQuery(token);
            var result = await _mediator.Send(query);

            if (result == null)
            {
                return Unauthorized(new { Message = "Token inválido" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, new { Message = "Erro interno do servidor" });
        }
    }

    #region FIDO2 Endpoints

    /// <summary>
    /// Inicia registro de credencial FIDO2
    /// </summary>
    /// <param name="request">Dados para iniciar registro</param>
    /// <returns>Options para o cliente</returns>
    [HttpPost("fido2/register/start")]
    [Authorize]
    [SwaggerOperation(Summary = "Iniciar registro FIDO2", Description = "Cria challenge para registro de credencial")]
    [SwaggerResponse(200, "Challenge criado", typeof(Fido2RegisterStartResponseDto))]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult<Fido2RegisterStartResponseDto>> StartFido2Registration([FromBody] Fido2RegisterStartDto request)
    {
        try
        {
            var command = new Fido2RegisterStartCommand(request.UserId, request.DisplayName ?? string.Empty, request.DeviceName);
            var result = await _mediator.Send(command);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting FIDO2 registration");
            return StatusCode(500, new { Message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Completa registro de credencial FIDO2
    /// </summary>
    /// <param name="request">Response do cliente</param>
    /// <returns>Resultado do registro</returns>
    [HttpPost("fido2/register/complete")]
    [Authorize]
    [SwaggerOperation(Summary = "Completar registro FIDO2", Description = "Processa response de registro")]
    [SwaggerResponse(200, "Credencial registrada", typeof(AuthResponseDto))]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult<AuthResponseDto>> CompleteFido2Registration([FromBody] Fido2RegisterCompleteDto request)
    {
        try
        {
            var command = new Fido2RegisterCompleteCommand(request.UserId, request.Response, request.DeviceName);
            var result = await _mediator.Send(command);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing FIDO2 registration");
            return StatusCode(500, new { Message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Inicia autenticação FIDO2 (formato novo)
    /// </summary>
    /// <param name="request">Dados para autenticação</param>
    /// <returns>Options para o cliente</returns>
    [HttpPost("fido2/auth/start")]
    [SwaggerOperation(Summary = "Iniciar autenticação FIDO2", Description = "Cria challenge para autenticação")]
    [SwaggerResponse(200, "Challenge criado", typeof(Fido2AuthStartResponseDto))]
    [SwaggerResponse(400, "Dados inválidos")]
    public async Task<ActionResult<Fido2AuthStartResponseDto>> StartFido2Authentication([FromBody] Fido2AuthStartDto request)
    {
        try
        {
            var command = new Fido2AuthStartCommand(request.UserId, request.Email);
            var result = await _mediator.Send(command);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting FIDO2 authentication");
            return StatusCode(500, new { Message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Inicia autenticação FIDO2 (formato compatibilidade com testes)
    /// </summary>
    /// <param name="request">Dados para autenticação</param>
    /// <returns>Options para o cliente</returns>
    [HttpPost("fido2/authenticate/start")] // Alias para compatibilidade com testes
    [SwaggerOperation(Summary = "Iniciar autenticação FIDO2 (compatibilidade)", Description = "Cria challenge para autenticação usando email")]
    [SwaggerResponse(200, "Challenge criado", typeof(Fido2AuthStartResponseDto))]
    [SwaggerResponse(400, "Dados inválidos")]
    public async Task<ActionResult<Fido2AuthStartResponseDto>> StartFido2AuthenticationCompat([FromBody] StartFido2AuthenticationRequest request)
    {
        try
        {
            var command = new Fido2AuthStartCommand(null, request.UserEmail);
            var result = await _mediator.Send(command);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found for FIDO2 authentication start");
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting FIDO2 authentication");
            return StatusCode(500, new { Message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Completa autenticação FIDO2
    /// </summary>
    /// <param name="request">Response do cliente</param>
    /// <returns>Token JWT se autenticado</returns>
    [HttpPost("fido2/auth/complete")]
    [SwaggerOperation(Summary = "Completar autenticação FIDO2", Description = "Processa response de autenticação")]
    [SwaggerResponse(200, "Autenticação realizada", typeof(AuthResponseDto))]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(401, "Autenticação falhou")]
    public async Task<ActionResult<AuthResponseDto>> CompleteFido2Authentication([FromBody] Fido2AuthCompleteDto request)
    {
        try
        {
            var command = new Fido2AuthCompleteCommand(request.Response);
            var result = await _mediator.Send(command);

            if (!result.Success)
            {
                return Unauthorized(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing FIDO2 authentication");
            return StatusCode(500, new { Message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Lista credenciais do usuário
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <returns>Lista de credenciais</returns>
    [HttpGet("fido2/credentials/{userId:guid}")]
    [Authorize]
    [SwaggerOperation(Summary = "Listar credenciais", Description = "Obtém credenciais FIDO2 do usuário")]
    [SwaggerResponse(200, "Lista de credenciais", typeof(IEnumerable<UserCredentialDto>))]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult<IEnumerable<UserCredentialDto>>> GetUserCredentials(Guid userId)
    {
        try
        {
            var query = new GetUserCredentialsQuery(userId);
            var result = await _mediator.Send(query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user credentials for user: {UserId}", userId);
            return StatusCode(500, new { Message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Remove credencial FIDO2
    /// </summary>
    /// <param name="request">Dados da credencial a remover</param>
    /// <returns>Confirmação de remoção</returns>
    [HttpDelete("fido2/credentials")]
    [Authorize]
    [SwaggerOperation(Summary = "Remover credencial", Description = "Remove credencial FIDO2 do usuário")]
    [SwaggerResponse(200, "Credencial removida")]
    [SwaggerResponse(400, "Dados inválidos")]
    [SwaggerResponse(401, "Não autorizado")]
    [SwaggerResponse(404, "Credencial não encontrada")]
    public async Task<ActionResult> RemoveCredential([FromBody] RemoveCredentialDto request)
    {
        try
        {
            var command = new RemoveCredentialCommand(request.CredentialId.ToString(), request.UserId);
            var result = await _mediator.Send(command);

            if (!result)
            {
                return NotFound(new { Message = "Credencial não encontrada" });
            }

            return Ok(new { Message = "Credencial removida com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing credential");
            return StatusCode(500, new { Message = "Erro interno do servidor" });
        }
    }

    #endregion

    #region OAuth2 Endpoints

    /// <summary>
    /// Obtém URL de autorização para provedor OAuth2
    /// </summary>
    /// <param name="provider">Nome do provedor (Google, GitHub, Facebook, Microsoft)</param>
    /// <param name="redirectUri">URI de redirecionamento</param>
    /// <param name="state">State parameter para proteção CSRF (opcional)</param>
    /// <returns>URL de autorização</returns>
    [HttpGet("oauth/{provider}/authorize")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Obter URL OAuth2", Description = "Gera URL de autorização para provedor OAuth2")]
    [SwaggerResponse(200, "URL de autorização gerada", typeof(OAuthAuthorizationResponseDto))]
    [SwaggerResponse(400, "Provedor não suportado ou parâmetros inválidos")]
    public async Task<ActionResult<OAuthAuthorizationResponseDto>> GetOAuthAuthorizationUrl(
        string provider,
        [FromQuery] string redirectUri,
        [FromQuery] string? state = null)
    {
        try
        {
            var command = new GetOAuthAuthorizationUrlCommand(provider, redirectUri, state);
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid OAuth2 authorization request for provider: {Provider}", provider);
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating OAuth2 authorization URL for provider: {Provider}", provider);
            return StatusCode(500, new { Message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Processa callback OAuth2 e realiza login/registro
    /// </summary>
    /// <param name="provider">Nome do provedor</param>
    /// <param name="request">Dados do callback OAuth2</param>
    /// <returns>Token JWT se autenticação for bem-sucedida</returns>
    [HttpPost("oauth/{provider}/callback")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Callback OAuth2", Description = "Processa callback de provedor OAuth2 e realiza autenticação")]
    [SwaggerResponse(200, "Autenticação realizada com sucesso", typeof(OAuthLoginResponseDto))]
    [SwaggerResponse(400, "Dados inválidos ou erro de autenticação")]
    [SwaggerResponse(401, "Falha na autenticação")]
    public async Task<ActionResult<OAuthLoginResponseDto>> HandleOAuthCallback(
        string provider,
        [FromBody] OAuthCallbackRequestDto request)
    {
        try
        {
            // Verificar se há erro na resposta OAuth
            if (!string.IsNullOrEmpty(request.Error))
            {
                _logger.LogWarning("OAuth2 error from provider {Provider}: {Error} - {Description}", 
                    provider, request.Error, request.ErrorDescription);
                
                return BadRequest(new OAuthLoginResponseDto
                {
                    Success = false,
                    Message = $"OAuth2 error: {request.Error} - {request.ErrorDescription}"
                });
            }

            var command = new ProcessOAuthCallbackCommand(provider, request.Code, GetCallbackUri(provider), request.State);
            var result = await _mediator.Send(command);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OAuth2 callback for provider: {Provider}", provider);
            return StatusCode(500, new { Message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Vincula conta externa a usuário autenticado
    /// </summary>
    /// <param name="provider">Nome do provedor</param>
    /// <param name="request">Dados de autorização</param>
    /// <returns>Resultado da vinculação</returns>
    [HttpPost("oauth/{provider}/link")]
    [Authorize]
    [SwaggerOperation(Summary = "Vincular conta externa", Description = "Vincula provedor OAuth2 a usuário autenticado")]
    [SwaggerResponse(200, "Conta vinculada com sucesso")]
    [SwaggerResponse(400, "Dados inválidos ou falha na vinculação")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult> LinkExternalAccount(
        string provider,
        [FromBody] OAuthCallbackRequestDto request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { Message = "Token inválido" });
            }

            var command = new LinkExternalAccountCommand(userId.Value, provider, request.Code, GetCallbackUri(provider), request.State);
            var result = await _mediator.Send(command);

            if (result)
            {
                return Ok(new { Message = "Conta externa vinculada com sucesso" });
            }

            return BadRequest(new { Message = "Falha ao vincular conta externa" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking external account for provider: {Provider}", provider);
            return StatusCode(500, new { Message = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Remove vinculação de conta externa
    /// </summary>
    /// <param name="provider">Nome do provedor</param>
    /// <returns>Resultado da remoção</returns>
    [HttpDelete("oauth/{provider}/unlink")]
    [Authorize]
    [SwaggerOperation(Summary = "Desvincular conta externa", Description = "Remove vinculação de provedor OAuth2")]
    [SwaggerResponse(200, "Conta desvinculada com sucesso")]
    [SwaggerResponse(400, "Falha na desvinculação")]
    [SwaggerResponse(401, "Não autorizado")]
    public async Task<ActionResult> UnlinkExternalAccount(string provider)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { Message = "Token inválido" });
            }

            var command = new UnlinkExternalAccountCommand(userId.Value, provider);
            var result = await _mediator.Send(command);

            if (result)
            {
                return Ok(new { Message = "Conta externa desvinculada com sucesso" });
            }

            return BadRequest(new { Message = "Falha ao desvincular conta externa" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlinking external account for provider: {Provider}", provider);
            return StatusCode(500, new { Message = "Erro interno do servidor" });
        }
    }

    #endregion

    /// <summary>
    /// Endpoint público para verificação de status da API
    /// </summary>
    /// <returns>Status da API</returns>
    [HttpGet("ping")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Ping da API", Description = "Verifica se a API está funcionando")]
    [SwaggerResponse(200, "API funcionando")]
    public IActionResult Ping()
    {
        return Ok(new { Status = "OK", Timestamp = DateTime.UtcNow });
    }

    #region Private Helper Methods

    /// <summary>
    /// Obtém ID do usuário atual do token JWT
    /// </summary>
    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }

    /// <summary>
    /// Constrói URI de callback baseada no provedor
    /// </summary>
    private string GetCallbackUri(string provider)
    {
        var baseUri = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
        return $"{baseUri}/api/v1/auth/oauth/{provider.ToLower()}/callback";
    }

    #endregion
}
