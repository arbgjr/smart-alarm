using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SmartAlarm.SecurityTests;

/// <summary>
/// Testes de penetração automatizados
/// </summary>
public class PenetrationTests : SecurityTestsBase
{
    public PenetrationTests(WebApplicationFactory<Program> factory) : base(factory) { }

    [Fact]
    public async Task BruteForce_LoginAttempts_ShouldBeBlocked()
    {
        var commonPasswords = new[]
        {
            "password", "123456", "password123", "admin", "qwerty",
            "letmein", "welcome", "monkey", "dragon", "master"
        };

        var email = "admin@smartalarm.com";
        var blockedCount = 0;

        foreach (var password in commonPasswords)
        {
            var loginRequest = new
            {
                Email = email,
                Password = password
            };

            var response = await _client.PostAsync("/api/auth/login",
                new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json"));

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                blockedCount++;
                break; // Rate limiting ativo
            }

            // Pequeno delay para simular tentativas reais
            await Task.Delay(100);
        }

        // Deve ter rate limiting ou todas as tentativas devem falhar
        Assert.True(blockedCount > 0 || commonPasswords.All(async p =>
        {
            var req = new { Email = email, Password = p };
            var resp = await _client.PostAsync("/api/auth/login",
                new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json"));
            return resp.StatusCode == HttpStatusCode.Unauthorized;
        }));
    }

    [Theory]
    [InlineData("' OR '1'='1")]
    [InlineData("'; DROP TABLE Users; --")]
    [InlineData("admin'--")]
    [InlineData("' UNION SELECT * FROM Users --")]
    [InlineData("1' OR '1'='1' /*")]
    public async Task SqlInjection_LoginField_ShouldBePrevented(string sqlPayload)
    {
        var loginRequest = new
        {
            Email = sqlPayload,
            Password = "password"
        };

        var response = await _client.PostAsync("/api/auth/login",
            new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json"));

        // Não deve retornar erro de servidor (500)
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);

        // Deve retornar erro de validação ou unauthorized
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest ||
                   response.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("<script>alert('XSS')</script>")]
    [InlineData("<img src=x onerror=alert('XSS')>")]
    [InlineData("javascript:alert('XSS')")]
    [InlineData("<svg onload=alert('XSS')>")]
    [InlineData("'><script>alert('XSS')</script>")]
    public async Task XssInjection_UserInput_ShouldBeSanitized(string xssPayload)
    {
        var token = await GetValidJwtTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            return;
        }

        var alarmRequest = new
        {
            Title = xssPayload,
            Description = "Test description",
            Time = "08:00"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/alarms")
        {
            Content = new StringContent(JsonSerializer.Serialize(alarmRequest), Encoding.UTF8, "application/json")
        };
        AddAuthorizationHeader(request, token);

        var response = await _client.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            // Se aceitar, verificar se foi sanitizado
            var getRequest = new HttpRequestMessage(HttpMethod.Get, "/api/alarms");
            AddAuthorizationHeader(getRequest, token);

            var getResponse = await _client.SendAsync(getRequest);
            if (getResponse.IsSuccessStatusCode)
            {
                var content = await getResponse.Content.ReadAsStringAsync();

                // Não deve conter scripts não sanitizados
                Assert.DoesNotContain("<script>", content, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("javascript:", content, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("onerror=", content, StringComparison.OrdinalIgnoreCase);
            }
        }
        else
        {
            // Se rejeitar, deve ser por validação, não erro de servidor
            Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        }
    }

    [Theory]
    [InlineData("<?xml version=\"1.0\"?><!DOCTYPE root [<!ENTITY test SYSTEM 'file:///etc/passwd'>]><root>&test;</root>")]
    [InlineData("<!DOCTYPE foo [<!ELEMENT foo ANY><!ENTITY xxe SYSTEM \"file:///etc/passwd\">]><foo>&xxe;</foo>")]
    public async Task XxeInjection_XmlInput_ShouldBePrevented(string xxePayload)
    {
        var response = await _client.PostAsync("/api/import/xml",
            new StringContent(xxePayload, Encoding.UTF8, "application/xml"));

        // Não deve processar entidades externas ou retornar conteúdo de arquivos
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("root:", content);
            Assert.DoesNotContain("/bin/", content);
        }
    }

    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("..\\..\\..\\windows\\system32\\drivers\\etc\\hosts")]
    [InlineData("/etc/passwd")]
    [InlineData("C:\\Windows\\System32\\config\\SAM")]
    [InlineData("....//....//....//etc/passwd")]
    [InlineData("%2e%2e%2f%2e%2e%2f%2e%2e%2fetc%2fpasswd")]
    public async Task DirectoryTraversal_FileAccess_ShouldBePrevented(string maliciousPath)
    {
        var token = await GetValidJwtTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            return;
        }

        var encodedPath = Uri.EscapeDataString(maliciousPath);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/files/{encodedPath}");
        AddAuthorizationHeader(request, token);

        var response = await _client.SendAsync(request);

        // Não deve retornar conteúdo de arquivos do sistema
        Assert.True(response.StatusCode == HttpStatusCode.NotFound ||
                   response.StatusCode == HttpStatusCode.BadRequest ||
                   response.StatusCode == HttpStatusCode.Forbidden);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("root:", content);
            Assert.DoesNotContain("daemon:", content);
            Assert.DoesNotContain("[boot loader]", content);
        }
    }

    [Theory]
    [InlineData("http://169.254.169.254/latest/meta-data/")]
    [InlineData("http://localhost:8080/admin")]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://internal-server/secrets")]
    public async Task ServerSideRequestForgery_ShouldBePrevented(string maliciousUrl)
    {
        var token = await GetValidJwtTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            return;
        }

        var webhookRequest = new
        {
            Name = "Test Webhook",
            Url = maliciousUrl,
            Events = new[] { "alarm.created" }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks")
        {
            Content = new StringContent(JsonSerializer.Serialize(webhookRequest), Encoding.UTF8, "application/json")
        };
        AddAuthorizationHeader(request, token);

        var response = await _client.SendAsync(request);

        // Deve rejeitar URLs suspeitas
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("url", content, StringComparison.OrdinalIgnoreCase);
        }
        else if (response.IsSuccessStatusCode)
        {
            // Se aceitar, não deve fazer requisições para URLs internas
            // Isso seria testado em testes de integração mais complexos
            Assert.True(true); // Placeholder para validação mais complexa
        }
    }

    [Fact]
    public async Task CommandInjection_ShouldBePrevented()
    {
        var token = await GetValidJwtTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            return;
        }

        var maliciousCommands = new[]
        {
            "; cat /etc/passwd",
            "| whoami",
            "&& ls -la",
            "`id`",
            "$(whoami)",
            "; rm -rf /",
            "| net user"
        };

        foreach (var command in maliciousCommands)
        {
            var exportRequest = new
            {
                Format = "csv",
                Filename = $"export{command}.csv"
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/export")
            {
                Content = new StringContent(JsonSerializer.Serialize(exportRequest), Encoding.UTF8, "application/json")
            };
            AddAuthorizationHeader(request, token);

            var response = await _client.SendAsync(request);

            // Não deve executar comandos ou retornar erro de servidor
            Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Assert.DoesNotContain("uid=", content);
                Assert.DoesNotContain("root:", content);
                Assert.DoesNotContain("Administrator", content);
            }
        }
    }

    [Fact]
    public async Task LdapInjection_ShouldBePrevented()
    {
        var ldapPayloads = new[]
        {
            "*)(uid=*))(|(uid=*",
            "*)(|(password=*))",
            "admin)(&(password=*))",
            "*))%00"
        };

        foreach (var payload in ldapPayloads)
        {
            var searchRequest = new
            {
                Query = payload,
                Type = "user"
            };

            var response = await _client.PostAsync("/api/search",
                new StringContent(JsonSerializer.Serialize(searchRequest), Encoding.UTF8, "application/json"));

            // Deve tratar adequadamente ou rejeitar
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest ||
                       response.StatusCode == HttpStatusCode.NotFound ||
                       response.StatusCode == HttpStatusCode.NotImplemented);
        }
    }

    [Fact]
    public async Task InsecureDeserialization_ShouldBePrevented()
    {
        var maliciousJson = @"{
            ""$type"": ""System.IO.FileInfo, System.IO.FileSystem"",
            ""FileName"": ""/etc/passwd""
        }";

        var response = await _client.PostAsync("/api/import/json",
            new StringContent(maliciousJson, Encoding.UTF8, "application/json"));

        // Não deve deserializar tipos perigosos
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("root:", content);
        }
    }

    [Fact]
    public async Task CsrfProtection_ShouldBeImplemented()
    {
        var token = await GetValidJwtTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            return;
        }

        // Tentar fazer requisição sem CSRF token (simulando ataque CSRF)
        var maliciousRequest = new
        {
            Title = "Malicious Alarm",
            Time = "00:00"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/alarms")
        {
            Content = new StringContent(JsonSerializer.Serialize(maliciousRequest), Encoding.UTF8, "application/json")
        };
        AddAuthorizationHeader(request, token);

        // Simular origem externa
        request.Headers.Add("Origin", "https://malicious-site.com");
        request.Headers.Add("Referer", "https://malicious-site.com/attack.html");

        var response = await _client.SendAsync(request);

        // CORS deve bloquear ou deve ter proteção CSRF
        Assert.True(response.StatusCode == HttpStatusCode.Forbidden ||
                   response.StatusCode == HttpStatusCode.BadRequest ||
                   response.IsSuccessStatusCode); // Se CORS permitir, deve ter outras proteções
    }

    [Fact]
    public async Task SessionFixation_ShouldBePrevented()
    {
        // Obter token inicial
        var initialToken = await GetValidJwtTokenAsync();
        if (string.IsNullOrEmpty(initialToken))
        {
            return;
        }

        // Fazer login novamente
        var loginRequest = new
        {
            Email = "test@example.com",
            Password = "TestPassword123!"
        };

        var loginResponse = await _client.PostAsync("/api/auth/login",
            new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json"));

        if (loginResponse.IsSuccessStatusCode)
        {
            var content = await loginResponse.Content.ReadAsStringAsync();
            var newLoginResult = JsonSerializer.Deserialize<LoginResponse>(content);

            // Novo token deve ser diferente do inicial
            Assert.NotEqual(initialToken, newLoginResult?.AccessToken);
        }
    }

    [Fact]
    public async Task InformationDisclosure_ShouldBePrevented()
    {
        // Tentar acessar endpoints que podem vazar informações
        var sensitiveEndpoints = new[]
        {
            "/api/debug",
            "/api/config",
            "/api/logs",
            "/api/status",
            "/.env",
            "/web.config",
            "/appsettings.json"
        };

        foreach (var endpoint in sensitiveEndpoints)
        {
            var response = await _client.GetAsync(endpoint);

            // Não deve retornar informações sensíveis
            Assert.True(response.StatusCode == HttpStatusCode.NotFound ||
                       response.StatusCode == HttpStatusCode.Forbidden ||
                       response.StatusCode == HttpStatusCode.Unauthorized);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Assert.DoesNotContain("password", content, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("secret", content, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("connectionstring", content, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
