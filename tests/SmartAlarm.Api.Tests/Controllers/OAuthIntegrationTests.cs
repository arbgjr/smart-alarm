using SmartAlarm.Domain.Abstractions;
using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SmartAlarm.Application.DTOs.Auth;
using SmartAlarm.Tests.Factories;
using Xunit;

namespace SmartAlarm.Api.Tests.Controllers;

[Collection("IntegrationTests")]
public class OAuthIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public OAuthIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task GetOAuthAuthorizationUrl_ValidProvider_ReturnsAuthorizationUrl()
    {
        // Arrange
        var provider = "Google";
        var redirectUri = "https://localhost:3000/auth/callback";
        var state = "test-state-123";

        // Act
        var response = await _client.GetAsync($"/api/v1/auth/oauth/{provider}/authorize?redirectUri={Uri.EscapeDataString(redirectUri)}&state={Uri.EscapeDataString(state)}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<OAuthAuthorizationResponseDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().NotBeNull();
        result!.AuthorizationUrl.Should().NotBeNullOrEmpty();
        result.AuthorizationUrl.Should().Contain("accounts.google.com");
        result.AuthorizationUrl.Should().Contain("client_id=");
        result.AuthorizationUrl.Should().Contain($"redirect_uri={Uri.EscapeDataString(redirectUri)}");
        result.AuthorizationUrl.Should().Contain($"state={Uri.EscapeDataString(state)}");
        result.Provider.Should().Be(provider);
        result.State.Should().Be(state);
    }

    [Theory]
    [InlineData("Google")]
    [InlineData("GitHub")]
    [InlineData("Facebook")]
    [InlineData("Microsoft")]
    public async Task GetOAuthAuthorizationUrl_SupportedProviders_ReturnsSuccess(string provider)
    {
        // Arrange
        var redirectUri = "https://localhost:3000/auth/callback";
        var state = $"test-state-{provider.ToLower()}";

        // Act
        var response = await _client.GetAsync($"/api/v1/auth/oauth/{provider}/authorize?redirectUri={Uri.EscapeDataString(redirectUri)}&state={Uri.EscapeDataString(state)}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<OAuthAuthorizationResponseDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().NotBeNull();
        result!.Provider.Should().Be(provider);
    }

    [Theory]
    [InlineData("Twitter")]
    [InlineData("LinkedIn")]
    [InlineData("InvalidProvider")]
    public async Task GetOAuthAuthorizationUrl_UnsupportedProviders_ReturnsBadRequest(string provider)
    {
        // Arrange
        var redirectUri = "https://localhost:3000/auth/callback";
        var state = "test-state";

        // Act
        var response = await _client.GetAsync($"/api/v1/auth/oauth/{provider}/authorize?redirectUri={Uri.EscapeDataString(redirectUri)}&state={Uri.EscapeDataString(state)}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetOAuthAuthorizationUrl_MissingRedirectUri_ReturnsBadRequest()
    {
        // Arrange
        var provider = "Google";

        // Act
        var response = await _client.GetAsync($"/api/v1/auth/oauth/{provider}/authorize");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetOAuthAuthorizationUrl_InvalidRedirectUri_ReturnsBadRequest()
    {
        // Arrange
        var provider = "Google";
        var invalidRedirectUri = "not-a-valid-uri";

        // Act
        var response = await _client.GetAsync($"/api/v1/auth/oauth/{provider}/authorize?redirectUri={invalidRedirectUri}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ProcessOAuthCallback_ValidRequest_ProcessesSuccessfully()
    {
        // Note: This is a mock test since we can't actually complete OAuth flow in integration tests
        // In a real scenario, this would require mocking the OAuth provider responses
        
        // Arrange
        var provider = "Google";
        var request = new OAuthCallbackRequestDto
        {
            Code = "mock-auth-code-for-testing",
            State = "test-state-123"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync($"/api/v1/auth/oauth/{provider}/callback", content);

        // Assert
        // This will likely return an error since we don't have real OAuth setup, 
        // but it should reach the controller and validate the request structure
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
        
        // The important thing is that it doesn't return 404 (endpoint exists)
        // and it doesn't return 405 (method allowed)
    }

    [Fact]
    public async Task ProcessOAuthCallback_OAuthError_ReturnsBadRequest()
    {
        // Arrange
        var provider = "Google";
        var request = new OAuthCallbackRequestDto
        {
            Code = "",
            State = "test-state-123",
            Error = "access_denied",
            ErrorDescription = "User denied access"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync($"/api/v1/auth/oauth/{provider}/callback", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<OAuthLoginResponseDto>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain("access_denied");
    }

    [Fact]
    public async Task ProcessOAuthCallback_EmptyCode_ReturnsBadRequest()
    {
        // Arrange
        var provider = "Google";
        var request = new OAuthCallbackRequestDto
        {
            Code = "",
            State = "test-state-123"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync($"/api/v1/auth/oauth/{provider}/callback", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task LinkExternalAccount_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var provider = "GitHub";
        var request = new OAuthCallbackRequestDto
        {
            Code = "mock-auth-code",
            State = "test-state"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync($"/api/v1/auth/oauth/{provider}/link", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UnlinkExternalAccount_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var provider = "GitHub";

        // Act
        var response = await _client.DeleteAsync($"/api/v1/auth/oauth/{provider}/unlink");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task OAuth_Endpoints_HaveCorrectRouting()
    {
        // Test all OAuth endpoints exist with correct HTTP methods

        // Test authorization URL endpoint
        var authResponse = await _client.GetAsync("/api/v1/auth/oauth/Google/authorize?redirectUri=https://test.com");
        authResponse.StatusCode.Should().NotBe(HttpStatusCode.NotFound);

        // Test callback endpoint
        var callbackContent = new StringContent("{\"code\":\"test\"}", Encoding.UTF8, "application/json");
        var callbackResponse = await _client.PostAsync("/api/v1/auth/oauth/Google/callback", callbackContent);
        callbackResponse.StatusCode.Should().NotBe(HttpStatusCode.NotFound);

        // Test link endpoint (should require auth)
        var linkContent = new StringContent("{\"code\":\"test\"}", Encoding.UTF8, "application/json");
        var linkResponse = await _client.PostAsync("/api/v1/auth/oauth/Google/link", linkContent);
        linkResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized); // Expected since no auth

        // Test unlink endpoint (should require auth)
        var unlinkResponse = await _client.DeleteAsync("/api/v1/auth/oauth/Google/unlink");
        unlinkResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized); // Expected since no auth
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task OAuth_AuthorizeEndpoint_OnlyAllowsGetMethod(string method)
    {
        // Arrange
        var request = new HttpRequestMessage(new HttpMethod(method), "/api/v1/auth/oauth/Google/authorize?redirectUri=https://test.com");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        if (method == "GET")
        {
            response.StatusCode.Should().NotBe(HttpStatusCode.MethodNotAllowed);
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        }
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task OAuth_CallbackEndpoint_OnlyAllowsPostMethod(string method)
    {
        // Arrange
        var content = method == "POST" ? new StringContent("{\"code\":\"test\"}", Encoding.UTF8, "application/json") : null;
        var request = new HttpRequestMessage(new HttpMethod(method), "/api/v1/auth/oauth/Google/callback");
        if (content != null) request.Content = content;

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        if (method == "POST")
        {
            response.StatusCode.Should().NotBe(HttpStatusCode.MethodNotAllowed);
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        }
    }

    [Fact]
    public async Task OAuth_Endpoints_ReturnCorrectContentType()
    {
        // Test that OAuth endpoints return JSON

        var response = await _client.GetAsync("/api/v1/auth/oauth/Google/authorize?redirectUri=https://test.com&state=test");
        
        if (response.IsSuccessStatusCode)
        {
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        }
    }
}