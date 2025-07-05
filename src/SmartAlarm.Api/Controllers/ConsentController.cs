using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartAlarm.Api.Services;
using System.Security.Claims;

namespace SmartAlarm.Api.Controllers
{
    [ApiController]
    [Route("api/v1/consent")]
    public class ConsentController : ControllerBase
    {
        private readonly IUserConsentService _userConsentService;
        public ConsentController(IUserConsentService userConsentService)
        {
            _userConsentService = userConsentService;
        }

        /// <summary>
        /// Registra consentimento LGPD do usuário autenticado.
        /// </summary>
        [HttpPost]
        [Authorize]
        public IActionResult RegisterConsent([FromQuery] bool consentGiven)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            _userConsentService.RegisterConsent(userId, consentGiven);
            return Ok(new { userId, consentGiven });
        }

        /// <summary>
        /// Consulta consentimento LGPD do usuário autenticado.
        /// </summary>
        [HttpGet]
        [Authorize]
        public IActionResult GetConsent()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var consent = _userConsentService.HasConsent(userId);
            return Ok(new { userId, consent });
        }
    }
}
