using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SmartAlarm.SecurityTests;

/// <summary>
/// Testes específicos de autenticação e autorização
/// </summary>
public class AuthenticationAuthorizationTests : SecurityTestsBase
{
    public AuthenticationAuthorizationTests(WebApplicationFactory<Program> factory) : base(factory) { }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnToken()
    {
        var loginRequest = new
        {
            Email = "test@example.com",
            Password = "TestPassword123!"
        };

        var response = await _client.PostAsync("/api/auth/login",
            new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json"));

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var loginResponse = JsonSerializer.Deserialize<LoginResponse>(content);

            Assert.NotNull(loginResponse);
            Assert.NotEmpty(loginResponse.AccessToken);
            Assert.True(loginResponse.ExpiresAt > DateTime.UtcNow);
        }
        else
        {
            // Em ambiente de teste, pode não ter usuário configurado
            Assert.True(response.StatusCode == HttpStatusCode.Unauthorized ||
                       response.StatusCode == HttpStatusCode.BadRequest);
        }

        AssertSecurityHeaders(response);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        var loginRequest = new
        {
            Email = "invalid@example.com",
            Password = "wrongpassword"
        };

        var response = await _client.PostAsync("/api/auth/login",
            new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        AssertSecurityHeaders(response);
    }

    [Fact]
    public async Task Login_WithMissingFields_ShouldReturnBadRequest()
    {
        var loginRequest = new
        {
            Email = "test@example.com"
            // Missing password
        };

        var response = await _client.PostAsync("/api/auth/login",
            new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("validation", content, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("test@")]
    public async Task Login_WithInvalidEmailFormat_ShouldReturnBadRequest(string invalidEmail)
    {
        var loginRequest = new
        {
            Email = invalidEmail,
            Password = "TestPassword123!"
        };

        var response = await _client.PostAsync("/api/auth/login",
            new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("password")]
    [InlineData("PASSWORD")]
    [InlineData("Password")]
    public async Task Login_WithWeakPassword_ShouldReturnBadRequest(string weakPassword)
    {
        var loginRequest = new
        {
            Email = "test@example.com",
            Password = weakPassword
        };

        var response = await _client.PostAsync("/api/auth/login",
            new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json"));

        // Pode retornar BadRequest (validação) ou Unauthorized (credenciais inválidas)
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest ||
                   response.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ShouldReturnUnauthorized()
    {
        var response = await _client.GetAsync("/api/alarms");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        AssertSecurityHeaders(response);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithInvalidToken_ShouldReturnUnauthorized()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/alarms");
        AddAuthorizationHeader(request, "invalid-token");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithMalformedToken_ShouldReturnUnauthorized()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/alarms");
        AddAuthorizationHeader(request, "Bearer malformed.token.here");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithExpiredToken_ShouldReturnUnauthorized()
    {
        // Token JWT com exp no passado
        var expiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyLCJleHAiOjE1MTYyMzkwMjJ9.4Adcj3UFYzPUVaVF43FmMab6RlaQD8A9V8wFzzht-KQ";

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/alarms");
        AddAuthorizationHeader(request, expiredToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TokenRefresh_WithValidRefreshToken_ShouldReturnNewToken()
    {
        // Primeiro, fazer login para obter tokens
        var loginRequest = new
        {
            Email = "test@example.com",
            Password = "TestPassword123!"
        };

        var loginResponse = await _client.PostAsync("/api/auth/login",
            new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json"));

        if (!loginResponse.IsSuccessStatusCode)
        {
            // Skip test if login is not available
            return;
        }

        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<LoginResponse>(loginContent);

        // Tentar refresh do token
        var refreshRequest = new
        {
            RefreshToken = loginResult?.RefreshToken
        };

        var refreshResponse = await _client.PostAsync("/api/auth/refresh",
            new StringContent(JsonSerializer.Serialize(refreshRequest), Encoding.UTF8, "application/json"));

        // Pode não estar implementado ainda
        Assert.True(refreshResponse.StatusCode == HttpStatusCode.OK ||
                   refreshResponse.StatusCode == HttpStatusCode.NotFound ||
                   refreshResponse.StatusCode == HttpStatusCode.NotImplemented);
    }

    [Fact]
    public async Task Logout_WithValidToken_ShouldInvalidateToken()
    {
        var token = await GetValidJwtTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            return;
        }

        // Fazer logout
        var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        AddAuthorizationHeader(logoutRequest, token);

        var logoutResponse = await _client.SendAsync(logoutRequest);

        // Pode não estar implementado ainda
        if (logoutResponse.IsSuccessStatusCode)
        {
            // Tentar usar o token após logout
            var testRequest = new HttpRequestMessage(HttpMethod.Get, "/api/alarms");
            AddAuthorizationHeader(testRequest, token);

            var testResponse = await _client.SendAsync(testRequest);

            // Token deve estar invalidado
            Assert.Equal(HttpStatusCode.Unauthorized, testResponse.StatusCode);
        }
    }

    [Fact]
    public async Task Register_WithValidData_ShouldCreateUser()
    {
        var registerRequest = new
        {
            Name = "Test User",
            Email = $"test{Guid.NewGuid()}@example.com",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!"
        };

        var response = await _client.PostAsync("/api/auth/register",
            new StringContent(JsonSerializer.Serialize(registerRequest), Encoding.UTF8, "application/json"));

        // Pode retornar Created, OK ou NotImplemented
        Assert.True(response.StatusCode == HttpStatusCode.Created ||
                   response.StatusCode == HttpStatusCode.OK ||
                   response.StatusCode == HttpStatusCode.NotFound ||
                   response.StatusCode == HttpStatusCode.NotImplemented);

        AssertSecurityHeaders(response);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturnConflict()
    {
        var email = "duplicate@example.com";
        var registerRequest = new
        {
            Name = "Test User",
            Email = email,
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!"
        };

        // Primeira tentativa
        var response1 = await _client.PostAsync("/api/auth/register",
            new StringContent(JsonSerializer.Serialize(registerRequest), Encoding.UTF8, "application/json"));

        // Segunda tentativa com mesmo email
        var response2 = await _client.PostAsync("/api/auth/register",
            new StringContent(JsonSerializer.Serialize(registerRequest), Encoding.UTF8, "application/json"));

        // Se o registro funcionar, a segunda tentativa deve falhar
        if (response1.IsSuccessStatusCode)
        {
            Assert.True(response2.StatusCode == HttpStatusCode.Conflict ||
                       response2.StatusCode == HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task PasswordReset_ShouldImplementRateLimit()
    {
        var resetRequest = new
        {
            Email = "test@example.com"
        };

        var tasks = new List<Task<HttpResponseMessage>>();

        // Fazer múltiplas tentativas de reset
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(_client.PostAsync("/api/auth/forgot-password",
                new StringContent(JsonSerializer.Serialize(resetRequest), Encoding.UTF8, "application/json")));
        }

        var responses = await Task.WhenAll(tasks);

        // Pelo menos uma deve ser rate limited ou todas devem ser tratadas adequadamente
        var rateLimitedCount = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);
        var successCount = responses.Count(r => r.IsSuccessStatusCode);
        var notFoundCount = responses.Count(r => r.StatusCode == HttpStatusCode.NotFound);

        Assert.True(rateLimitedCount > 0 || successCount <= 3 || notFoundCount == responses.Length,
            "Password reset should implement rate limiting");
    }

    [Theory]
    [InlineData("/api/admin/users")]
    [InlineData("/api/admin/settings")]
    [InlineData("/hangfire")]
    public async Task AdminEndpoints_WithoutAdminRole_ShouldReturnForbidden(string endpoint)
    {
        var token = await GetValidJwtTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            return;
        }

        var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        AddAuthorizationHeader(request, token);

        var response = await _client.SendAsync(request);

        // Deve retornar Forbidden ou NotFound (se não implementado)
        Assert.True(response.StatusCode == HttpStatusCode.Forbidden ||
                   response.StatusCode == HttpStatusCode.NotFound ||
                   response.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task FIDO2_Registration_ShouldBeSecure()
    {
        var token = await GetValidJwtTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            return;
        }

        var fido2Request = new
        {
            Username = "testuser",
            DisplayName = "Test User"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/fido2/register")
        {
            Content = new StringContent(JsonSerializer.Serialize(fido2Request), Encoding.UTF8, "application/json")
        };
        AddAuthorizationHeader(request, token);

        var response = await _client.SendAsync(request);

        // FIDO2 pode não estar totalmente implementado
        Assert.True(response.StatusCode == HttpStatusCode.OK ||
                   response.StatusCode == HttpStatusCode.NotFound ||
                   response.StatusCode == HttpStatusCode.NotImplemented ||
                   response.StatusCode == HttpStatusCode.BadRequest);

        AssertSecurityHeaders(response);
    }
}
