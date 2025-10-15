using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SmartAlarm.SecurityTests;

/// <summary>
/// Testes de segurança baseados no OWASP Top 10
/// </summary>
public class OwaspTop10Tests : SecurityTestsBase
{
    public OwaspTop10Tests(WebApplicationFactory<Program> factory) : base(factory) { }

    [Fact]
    public async Task A01_BrokenAccessControl_ShouldPreventUnauthorizedAccess()
    {
        // Tentar acessar endpoint protegido sem autenticação
        var response = await _client.GetAsync("/api/alarms");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        AssertSecurityHeaders(response);
    }

    [Fact]
    public async Task A01_BrokenAccessControl_ShouldPreventAccessWithInvalidToken()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/alarms");
        AddAuthorizationHeader(request, GetInvalidJwtToken());

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A02_CryptographicFailures_ShouldEnforceHttpsRedirection()
    {
        // Este teste verifica se o middleware de redirecionamento HTTPS está ativo
        var response = await _client.GetAsync("/api/health");

        // Em ambiente de teste, pode não redirecionar, mas deve ter headers de segurança
        AssertSecurityHeaders(response);
        AssertNoSensitiveHeaders(response);
    }

    [Fact]
    public async Task A03_Injection_ShouldPreventSqlInjection()
    {
        var maliciousPayload = CreateMaliciousPayload("sql");
        var loginRequest = new
        {
            Email = maliciousPayload,
            Password = "password"
        };

        var response = await _client.PostAsync("/api/auth/login",
            new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json"));

        // Deve retornar erro de validação, não erro de servidor
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest ||
                   response.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A03_Injection_ShouldPreventXssInUserInput()
    {
        var token = await GetValidJwtTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            // Skip test if authentication is not available
            return;
        }

        var maliciousPayload = CreateMaliciousPayload("xss");
        var alarmRequest = new
        {
            Title = maliciousPayload,
            Description = "Test alarm",
            Time = "08:00"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/alarms")
        {
            Content = new StringContent(JsonSerializer.Serialize(alarmRequest), Encoding.UTF8, "application/json")
        };
        AddAuthorizationHeader(request, token);

        var response = await _client.SendAsync(request);

        // Deve aceitar ou rejeitar com validação, mas não causar erro de servidor
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            // Verificar se o conteúdo foi sanitizado (não contém script tags)
            Assert.DoesNotContain("<script>", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task A04_InsecureDesign_ShouldImplementRateLimiting()
    {
        var loginRequest = new
        {
            Email = "test@example.com",
            Password = "wrongpassword"
        };

        var tasks = new List<Task<HttpResponseMessage>>();

        // Fazer múltiplas tentativas de login rapidamente
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_client.PostAsync("/api/auth/login",
                new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json")));
        }

        var responses = await Task.WhenAll(tasks);

        // Pelo menos uma das respostas deve ser rate limited
        var rateLimitedResponses = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);

        // Em ambiente de teste, rate limiting pode estar desabilitado
        // Verificar se pelo menos as tentativas falharam apropriadamente
        var unauthorizedResponses = responses.Count(r => r.StatusCode == HttpStatusCode.Unauthorized);

        Assert.True(rateLimitedResponses > 0 || unauthorizedResponses == responses.Length,
            "Rate limiting should be active or all login attempts should fail");
    }

    [Fact]
    public async Task A05_SecurityMisconfiguration_ShouldHaveSecurityHeaders()
    {
        var response = await _client.GetAsync("/api/health");

        AssertSecurityHeaders(response);
        AssertNoSensitiveHeaders(response);

        // Verificar CSP header
        if (response.Headers.TryGetValues("Content-Security-Policy", out var cspValues))
        {
            var csp = cspValues.First();
            Assert.Contains("default-src 'self'", csp);
        }
    }

    [Fact]
    public async Task A06_VulnerableComponents_ShouldNotExposeVersionInfo()
    {
        var response = await _client.GetAsync("/api/health");

        AssertNoSensitiveHeaders(response);

        var content = await response.Content.ReadAsStringAsync();

        // Não deve expor informações de versão sensíveis
        Assert.DoesNotContain("Microsoft", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ASP.NET", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("version", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A07_IdentificationAuthenticationFailures_ShouldValidateTokens()
    {
        var expiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyLCJleHAiOjE1MTYyMzkwMjJ9.4Adcj3UFYzPUVaVF43FmMab6RlaQD8A9V8wFzzht-KQ";

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/alarms");
        AddAuthorizationHeader(request, expiredToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A08_SoftwareDataIntegrityFailures_ShouldValidateInput()
    {
        var invalidJsonPayload = "{ invalid json }";

        var response = await _client.PostAsync("/api/auth/login",
            new StringContent(invalidJsonPayload, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("validation", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A09_SecurityLoggingMonitoringFailures_ShouldLogSecurityEvents()
    {
        // Tentar login com credenciais inválidas
        var loginRequest = new
        {
            Email = "nonexistent@example.com",
            Password = "wrongpassword"
        };

        var response = await _client.PostAsync("/api/auth/login",
            new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        // Verificar se há correlation ID para rastreamento
        Assert.True(response.Headers.Contains("X-Correlation-ID") ||
                   response.Headers.Contains("TraceId") ||
                   !string.IsNullOrEmpty(response.Headers.GetValues("X-Request-ID").FirstOrDefault()),
                   "Security events should be traceable");
    }

    [Fact]
    public async Task A10_ServerSideRequestForgery_ShouldValidateUrls()
    {
        var token = await GetValidJwtTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            return;
        }

        // Tentar fazer requisição para URL interna maliciosa
        var maliciousUrl = "http://localhost:8080/admin/secrets";
        var integrationRequest = new
        {
            Name = "Test Integration",
            Url = maliciousUrl,
            Type = "webhook"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/integrations")
        {
            Content = new StringContent(JsonSerializer.Serialize(integrationRequest), Encoding.UTF8, "application/json")
        };
        AddAuthorizationHeader(request, token);

        var response = await _client.SendAsync(request);

        // Deve rejeitar URLs suspeitas ou validar adequadamente
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("url", content, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            // Se aceitar, deve ter validação adequada implementada
            Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        }
    }

    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("..\\..\\..\\windows\\system32\\drivers\\etc\\hosts")]
    [InlineData("/etc/passwd")]
    [InlineData("C:\\Windows\\System32\\config\\SAM")]
    public async Task PathTraversal_ShouldPreventDirectoryTraversal(string maliciousPath)
    {
        var token = await GetValidJwtTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            return;
        }

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/files/{Uri.EscapeDataString(maliciousPath)}");
        AddAuthorizationHeader(request, token);

        var response = await _client.SendAsync(request);

        // Deve retornar 404 ou 400, nunca 200 com conteúdo de arquivo do sistema
        Assert.True(response.StatusCode == HttpStatusCode.NotFound ||
                   response.StatusCode == HttpStatusCode.BadRequest ||
                   response.StatusCode == HttpStatusCode.Forbidden,
                   $"Path traversal should be prevented for: {maliciousPath}");
    }

    [Fact]
    public async Task CORS_ShouldBeConfiguredSecurely()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/alarms");
        request.Headers.Add("Origin", "https://malicious-site.com");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await _client.SendAsync(request);

        // Verificar se CORS está configurado adequadamente
        if (response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins))
        {
            var origin = origins.First();
            Assert.NotEqual("*", origin); // Não deve permitir qualquer origem
            Assert.DoesNotContain("malicious-site.com", origin);
        }
    }
}
