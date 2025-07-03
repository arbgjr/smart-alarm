# User Context Extraction – SmartAlarm API

## Overview

The `CurrentUserService` provides a secure and consistent way to access the authenticated user's information from JWT claims. This service should be injected wherever user context is required (e.g., in controllers, handlers).

## Exposed Properties

- **UserId**: Unique identifier from `sub` or `nameidentifier` claim
- **Email**: User email from `email` claim
- **Roles**: List of roles from `role` claims
- **IsAuthenticated**: Indicates if the user is authenticated

## Usage Example

```csharp
[ApiController]
[Route("api/[controller]")]
public class AlarmController : ControllerBase
{
    private readonly ICurrentUserService _currentUserService;
    public AlarmController(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [Authorize]
    public IActionResult GetAlarms()
    {
        if (!_currentUserService.IsAuthenticated)
            return Unauthorized();
        var userId = _currentUserService.UserId;
        // ...
        return Ok();
    }
}
```

## Security Notes

- Never trust user input for identity; always use claims from the validated JWT.
- Do not log or expose sensitive claim data.
- Always check `IsAuthenticated` before using user data.
