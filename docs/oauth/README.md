# OAuth2 Authentication - Smart Alarm

## Overview

Smart Alarm implements a comprehensive OAuth2 authentication system following the Authorization Code Flow with PKCE (Proof Key for Code Exchange) for enhanced security. The implementation supports multiple OAuth2 providers and seamlessly integrates with the existing JWT-based authentication system.

## Supported Providers

- **Google** - Full OAuth2 integration with Google accounts
- **GitHub** - Developer-friendly authentication via GitHub
- **Facebook** - Social media login integration
- **Microsoft** - Enterprise and personal Microsoft accounts

## Architecture

The OAuth2 implementation follows Clean Architecture principles across all layers:

### Domain Layer
- `ExternalAuthInfo` - Value object containing OAuth user information
- `User` entity extended with OAuth properties (`ExternalProvider`, `ExternalProviderId`)
- `IOAuthProvider` - OAuth provider abstraction
- `IOAuthProviderFactory` - Factory pattern for provider creation

### Application Layer
- **Commands**:
  - `GetOAuthAuthorizationUrlCommand` - Generate provider authorization URLs
  - `ProcessOAuthCallbackCommand` - Handle OAuth callback processing
  - `LinkExternalAccountCommand` - Link OAuth accounts to existing users
  - `UnlinkExternalAccountCommand` - Remove OAuth account links
- **Handlers**: Command handlers with comprehensive error handling and logging
- **Validators**: FluentValidation for all OAuth requests
- **DTOs**: Data transfer objects for OAuth API communication

### Infrastructure Layer
- **Base Classes**: `BaseOAuthProvider` with common OAuth2 functionality
- **Provider Implementations**:
  - `GoogleOAuthProvider` - Google-specific OAuth2 implementation
  - `GitHubOAuthProvider` - GitHub OAuth2 with email API integration
  - `FacebookOAuthProvider` - Facebook Graph API integration
  - `MicrosoftOAuthProvider` - Microsoft Graph OAuth2
- **Factory**: `OAuthProviderFactory` for provider instantiation

### API Layer
- **Endpoints**:
  - `GET /api/v1/auth/oauth/{provider}/authorize` - Get authorization URL
  - `POST /api/v1/auth/oauth/{provider}/callback` - Handle OAuth callback
  - `POST /api/v1/auth/oauth/{provider}/link` - Link account (authenticated)
  - `DELETE /api/v1/auth/oauth/{provider}/unlink` - Unlink account (authenticated)

## Security Features

### PKCE (Proof Key for Code Exchange)
- Implements RFC 7636 for enhanced security
- Generates cryptographically secure state parameters
- Protects against authorization code interception

### State Parameter Validation
- Anti-CSRF protection using state parameter
- Secure random state generation
- State validation on callback

### Secure Secrets Management
- All OAuth client secrets stored in KeyVault providers
- No hardcoded credentials in source code
- Support for multiple secret management backends

### Token Security
- Secure token exchange and validation
- Integration with existing JWT token blacklist system
- Automatic token refresh handling

## Database Schema

### Users Table Extensions
```sql
-- OAuth2 columns added to Users table
ALTER TABLE Users ADD COLUMN ExternalProvider VARCHAR(50);
ALTER TABLE Users ADD COLUMN ExternalProviderId VARCHAR(255);

-- Indexes for efficient OAuth lookups
CREATE INDEX IX_Users_ExternalProvider_ExternalProviderId 
ON Users (ExternalProvider, ExternalProviderId);
```

## Configuration

### Environment Variables
```bash
# Google OAuth2
GOOGLE_CLIENT_ID=your-google-client-id
GOOGLE_CLIENT_SECRET=your-google-client-secret

# GitHub OAuth2
GITHUB_CLIENT_ID=your-github-client-id
GITHUB_CLIENT_SECRET=your-github-client-secret

# Facebook OAuth2
FACEBOOK_CLIENT_ID=your-facebook-client-id
FACEBOOK_CLIENT_SECRET=your-facebook-client-secret

# Microsoft OAuth2
MICROSOFT_CLIENT_ID=your-microsoft-client-id
MICROSOFT_CLIENT_SECRET=your-microsoft-client-secret
```

### KeyVault Configuration
OAuth secrets are automatically retrieved from your configured KeyVault provider:
- HashiCorp Vault
- Azure Key Vault
- OCI Vault
- AWS Secrets Manager
- Mock provider (development)

## Frontend Integration

### OAuth Login Flow
```typescript
// Initiate OAuth flow
const handleOAuthLogin = async (provider: string) => {
  try {
    const response = await api.get(`/auth/oauth/${provider}/authorize`, {
      params: {
        redirectUri: window.location.origin + '/auth/callback',
        state: generateSecureState()
      }
    });
    
    // Redirect to provider's authorization page
    window.location.href = response.data.authorizationUrl;
  } catch (error) {
    console.error('OAuth login failed:', error);
  }
};
```

### Callback Handling
```typescript
// Handle OAuth callback
const handleOAuthCallback = async (code: string, state: string) => {
  try {
    const response = await api.post(`/auth/oauth/${provider}/callback`, {
      code,
      state
    });
    
    if (response.data.success) {
      // Store tokens and redirect to app
      setAuthTokens(response.data.accessToken, response.data.refreshToken);
      router.push('/dashboard');
    }
  } catch (error) {
    console.error('OAuth callback failed:', error);
  }
};
```

## Testing

### Unit Tests
- **Domain Tests**: User OAuth functionality and ExternalAuthInfo validation
- **Application Tests**: Command handlers with mock OAuth providers
- **Infrastructure Tests**: OAuth provider implementations with HTTP mocking
- **Validator Tests**: FluentValidation test helpers for comprehensive coverage

### Integration Tests
- **API Tests**: Full OAuth flow testing with TestWebApplicationFactory
- **Authentication Tests**: OAuth integration with existing auth system
- **Error Handling**: Comprehensive error scenario testing

### End-to-End Tests
- **Playwright Tests**: Full browser-based OAuth flow simulation
- **API E2E Tests**: Backend OAuth endpoint testing with curl/bash scripts
- **Cross-Platform**: Tests for desktop and mobile OAuth flows

## Error Handling

### Common OAuth Errors
```typescript
// OAuth error response structure
{
  success: false,
  message: "access_denied: User denied access",
  provider: "Google",
  error: "access_denied",
  errorDescription: "User denied access"
}
```

### Provider-Specific Error Handling
- **Network Issues**: Automatic retry with exponential backoff
- **Invalid Tokens**: Clear error messages and re-authentication prompts
- **User Cancellation**: Graceful handling of user-initiated cancellations
- **Expired Sessions**: Automatic cleanup and fresh authentication flow

## Monitoring and Observability

### Logging
- Structured logging with Serilog
- OAuth flow tracking with correlation IDs
- Security event logging (failed attempts, suspicious activity)

### Metrics
- OAuth success/failure rates per provider
- Authentication flow performance metrics
- User registration patterns via OAuth

### Tracing
- Distributed tracing with OpenTelemetry
- Full request/response correlation
- Performance bottleneck identification

## Best Practices

### Security
1. **Never log sensitive data** (access tokens, client secrets)
2. **Validate all callback parameters** (state, code, error)
3. **Use HTTPS everywhere** for OAuth redirects
4. **Implement proper CORS policies** for frontend OAuth flows

### Performance
1. **Cache provider configurations** to reduce API calls
2. **Implement connection pooling** for HTTP clients
3. **Use async/await patterns** consistently
4. **Optimize token validation** with efficient algorithms

### User Experience
1. **Clear OAuth provider buttons** with recognizable logos
2. **Loading states** during OAuth flows
3. **Error messages** that guide users to resolution
4. **Responsive design** for mobile OAuth flows

## Troubleshooting

### Common Issues

#### OAuth Provider Not Supported
```
Error: OAuth provider 'Twitter' is not supported
Solution: Check OAuthProviderFactory.GetSupportedProviders() for valid providers
```

#### Invalid Redirect URI
```
Error: Redirect URI mismatch
Solution: Ensure redirect URI matches exactly what's configured in OAuth provider
```

#### State Parameter Mismatch
```
Error: State parameter validation failed
Solution: Verify state parameter is correctly passed between authorization and callback
```

#### Token Exchange Failed
```
Error: Failed to exchange authorization code for access token
Solution: Check client credentials and authorization code validity
```

## API Documentation

### Get OAuth Authorization URL
```http
GET /api/v1/auth/oauth/{provider}/authorize
Parameters:
  - redirectUri (required): OAuth callback URL
  - state (optional): CSRF protection state
  - scopes (optional): Requested OAuth scopes

Response:
{
  "authorizationUrl": "https://provider.com/oauth/authorize?...",
  "state": "secure-random-state",
  "provider": "Google"
}
```

### Process OAuth Callback
```http
POST /api/v1/auth/oauth/{provider}/callback
Body:
{
  "code": "oauth-authorization-code",
  "state": "secure-random-state",
  "error": "access_denied", // optional, if error occurred
  "errorDescription": "User denied access" // optional
}

Response:
{
  "success": true,
  "accessToken": "jwt-access-token",
  "refreshToken": "jwt-refresh-token",
  "user": {
    "id": "user-guid",
    "name": "User Name",
    "email": "user@example.com"
  },
  "provider": "Google",
  "isNewUser": false
}
```

## Contributing

When adding new OAuth providers:

1. **Create provider implementation** extending `BaseOAuthProvider`
2. **Add provider to factory** in `OAuthProviderFactory.CreateProvider()`
3. **Update supported providers list** in domain constants
4. **Add comprehensive tests** (unit, integration, E2E)
5. **Update documentation** with provider-specific details

## Migration Guide

For existing users with email/password authentication:
1. OAuth login creates new account if email doesn't exist
2. OAuth login links to existing account if email matches
3. Users can link/unlink OAuth providers in account settings
4. External-only users cannot unlink their sole authentication method

---

For more detailed technical documentation, see:
- [OAuth2 RFC 6749](https://tools.ietf.org/html/rfc6749)
- [PKCE RFC 7636](https://tools.ietf.org/html/rfc7636)
- [Provider-specific documentation](./providers/)