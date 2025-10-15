using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace SmartAlarm.SecurityTests;

/// <summary>
/// Base class para testes de segurança
/// </summary>
public abstract class SecurityTestsBase : IClassFixture<WebApplicationFactory<Program>>
{
    protected readonly WebApplicationFactory<Program> _factory;
    protected readonly HttpClient _client;
    protected readonly IServiceScope _scope;

    protected SecurityTestsBase(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Configurações específicas para testes de segurança
                services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));
            });
        });

        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
    }

    /// <summary>
    /// Cria um token JWT válido para testes
    /// </summary>
    protected async Task<string> GetValidJwtTokenAsync()
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
            return loginResponse?.AccessToken ?? string.Empty;
        }

        return string.Empty;
    }

    /// <summary>
    /// Cria um token JWT inválido para testes
    /// </summary>
    protected string GetInvalidJwtToken()
    {
        return "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
    }

    /// <summary>
    /// Adiciona header de autorização à requisição
    /// </summary>
    protected void AddAuthorizationHeader(HttpRequestMessage request, string token)
    {
        request.Headers.Add("Authorization", $"Bearer {token}");
    }

    /// <summary>
    /// Cria payload malicioso para testes de injection
    /// </summary>
    protected string CreateMaliciousPayload(string type)
    {
        return type.ToLower() switch
        {
            "sql" => "'; DROP TABLE Users; --",
            "xss" => "<script>alert('XSS')</script>",
            "xxe" => "<?xml version=\"1.0\"?><!DOCTYPE root [<!ENTITY test SYSTEM 'file:///etc/passwd'>]><root>&test;</root>",
            "ldap" => "*)(uid=*))(|(uid=*",
            "command" => "; cat /etc/passwd",
            "path" => "../../../etc/passwd",
            _ => "malicious_input"
        };
    }

    /// <summary>
    /// Verifica se a resposta contém headers de segurança
    /// </summary>
    protected void AssertSecurityHeaders(HttpResponseMessage response)
    {
        Assert.True(response.Headers.Contains("X-Content-Type-Options"), "Missing X-Content-Type-Options header");
        Assert.True(response.Headers.Contains("X-Frame-Options"), "Missing X-Frame-Options header");
        Assert.True(response.Headers.Contains("X-XSS-Protection"), "Missing X-XSS-Protection header");
        Assert.True(response.Headers.Contains("Referrer-Policy"), "Missing Referrer-Policy header");

        if (response.RequestMessage?.RequestUri?.Scheme == "https")
        {
            Assert.True(response.Headers.Contains("Strict-Transport-Security"), "Missing HSTS header for HTTPS");
        }
    }

    /// <summary>
    /// Verifica se headers sensíveis foram removidos
    /// </summary>
    protected void AssertNoSensitiveHeaders(HttpResponseMessage response)
    {
        Assert.False(response.Headers.Contains("Server"), "Server header should be removed");
        Assert.False(response.Headers.Contains("X-Powered-By"), "X-Powered-By header should be removed");
        Assert.False(response.Headers.Contains("X-AspNet-Version"), "X-AspNet-Version header should be removed");
    }

    protected class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    public void Dispose()
    {
        _scope?.Dispose();
        _client?.Dispose();
    }
}
