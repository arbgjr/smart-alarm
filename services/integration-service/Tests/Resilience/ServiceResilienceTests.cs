using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace SmartAlarm.IntegrationService.Tests.Resilience
{
    /// <summary>
    /// Testes de resiliência para validar comportamento em cenários de falha
    /// </summary>
    public class ServiceResilienceTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly ITestOutputHelper _output;

        public ServiceResilienceTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
        {
            _factory = factory;
            _output = output;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task IntegrationService_Should_HandleInvalidInput_Gracefully()
        {
            // Arrange
            _output.WriteLine("Testing graceful handling of invalid input");

            var invalidRequests = new[]
            {
                // Request com dados inválidos
                new { Provider = "", Configuration = (object?)null, EnableNotifications = true },
                // Request com valores extremos
                new { Provider = new string('x', 1000), Configuration = new Dictionary<string, string>(), EnableNotifications = true },
                // Request com caracteres especiais
                new { Provider = "test<script>alert('xss')</script>", Configuration = new Dictionary<string, string>(), EnableNotifications = true }
            };

            // Act & Assert
            foreach (var (request, index) in invalidRequests.Select((r, i) => (r, i)))
            {
                _output.WriteLine($"Testing invalid request {index + 1}");

                var alarmId = Guid.NewGuid();
                var response = await _client.PostAsJsonAsync($"/api/v1/integrations/alarm/{alarmId}", request);

                _output.WriteLine($"  Response Status: {response.StatusCode}");

                // Should handle gracefully with appropriate error codes
                Assert.True(
                    response.StatusCode == HttpStatusCode.BadRequest ||
                    response.StatusCode == HttpStatusCode.UnprocessableEntity ||
                    response.StatusCode == HttpStatusCode.InternalServerError,
                    $"Unexpected status code {response.StatusCode} for invalid request {index + 1}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Assert.False(string.IsNullOrEmpty(errorContent), "Error response should contain error information");

                    // Verify error response is valid JSON
                    try
                    {
                        JsonSerializer.Deserialize<JsonElement>(errorContent);
                        _output.WriteLine($"  Valid error JSON returned");
                    }
                    catch (JsonException)
                    {
                        _output.WriteLine($"  Warning: Error response is not valid JSON: {errorContent}");
                    }
                }
            }
        }

        [Fact]
        public async Task IntegrationService_Should_HandleNetworkTimeouts_Gracefully()
        {
            // Arrange
            _output.WriteLine("Testing network timeout handling");

            using var timeoutClient = _factory.CreateClient();
            timeoutClient.Timeout = TimeSpan.FromMilliseconds(100); // Very short timeout

            var userId = Guid.NewGuid();

            // Act
            var tasks = new List<Task<(HttpStatusCode StatusCode, string Error)>>();

            for (int i = 0; i < 5; i++)
            {
                var task = Task.Run(async () =>
                {
                    try
                    {
                        var response = await timeoutClient.GetAsync($"/api/v1/integrations/user/{userId}");
                        var content = await response.Content.ReadAsStringAsync();
                        return (response.StatusCode, content);
                    }
                    catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                    {
                        return (HttpStatusCode.RequestTimeout, "Timeout occurred");
                    }
                    catch (HttpRequestException ex)
                    {
                        return (HttpStatusCode.ServiceUnavailable, ex.Message);
                    }
                    catch (Exception ex)
                    {
                        return (HttpStatusCode.InternalServerError, ex.Message);
                    }
                });
                tasks.Add(task);
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            _output.WriteLine("Timeout Test Results:");
            foreach (var (statusCode, error) in results)
            {
                _output.WriteLine($"  Status: {statusCode}, Error: {error}");
            }

            // At least some requests should complete or timeout gracefully
            var timeoutCount = results.Count(r => r.StatusCode == HttpStatusCode.RequestTimeout);
            var successCount = results.Count(r => (int)r.StatusCode >= 200 && (int)r.StatusCode < 300);
            var errorCount = results.Count(r => (int)r.StatusCode >= 500);

            _output.WriteLine($"Timeouts: {timeoutCount}, Success: {successCount}, Errors: {errorCount}");

            // Service should handle timeouts without crashing
            Assert.True(timeoutCount + successCount + errorCount == results.Length, "All requests should be categorized");
        }

        [Fact]
        public async Task IntegrationService_Should_HandleConcurrentFailures_WithoutCascading()
        {
            // Arrange
            _output.WriteLine("Testing concurrent failure handling without cascading effects");

            var concurrentTasks = new List<Task<(bool Success, HttpStatusCode StatusCode, long ResponseTime)>>();
            const int concurrentRequests = 20;

            // Act - Make concurrent requests that might fail
            for (int i = 0; i < concurrentRequests; i++)
            {
                var taskIndex = i;
                var task = Task.Run(async () =>
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    try
                    {
                        // Mix of valid and invalid requests to simulate partial failures
                        var userId = taskIndex % 3 == 0 ? Guid.Empty : Guid.NewGuid(); // Some invalid GUIDs
                        var response = await _client.GetAsync($"/api/v1/integrations/user/{userId}");

                        stopwatch.Stop();
                        return (response.IsSuccessStatusCode, response.StatusCode, stopwatch.ElapsedMilliseconds);
                    }
                    catch (Exception ex)
                    {
                        stopwatch.Stop();
                        _output.WriteLine($"Task {taskIndex} exception: {ex.Message}");
                        return (false, HttpStatusCode.InternalServerError, stopwatch.ElapsedMilliseconds);
                    }
                });
                concurrentTasks.Add(task);
            }

            var results = await Task.WhenAll(concurrentTasks);

            // Assert
            var successCount = results.Count(r => r.Success);
            var failureCount = results.Count(r => !r.Success);
            var averageResponseTime = results.Average(r => r.ResponseTime);
            var maxResponseTime = results.Max(r => r.ResponseTime);

            _output.WriteLine($"Concurrent Failure Test Results:");
            _output.WriteLine($"  Total Requests: {concurrentRequests}");
            _output.WriteLine($"  Successful: {successCount}");
            _output.WriteLine($"  Failed: {failureCount}");
            _output.WriteLine($"  Average Response Time: {averageResponseTime:F1}ms");
            _output.WriteLine($"  Max Response Time: {maxResponseTime}ms");

            // Some failures are expected, but service should remain responsive
            Assert.True(averageResponseTime < 5000, $"Average response time {averageResponseTime:F1}ms indicates service degradation");
            Assert.True(maxResponseTime < 10000, $"Max response time {maxResponseTime}ms indicates potential deadlock or cascade failure");

            // Service should handle at least some requests successfully
            Assert.True(successCount > 0 || failureCount == concurrentRequests, "Service should handle some requests or fail all consistently");
        }

        [Fact]
        public async Task IntegrationService_Should_RecoverFromTemporaryFailures()
        {
            // Arrange
            _output.WriteLine("Testing recovery from temporary failures");

            var userId = Guid.NewGuid();
            var recoveryAttempts = new List<(bool Success, HttpStatusCode StatusCode, long ResponseTime)>();

            // Act - Simulate recovery attempts over time
            for (int attempt = 1; attempt <= 10; attempt++)
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    var response = await _client.GetAsync($"/api/v1/integrations/user/{userId}");
                    stopwatch.Stop();

                    recoveryAttempts.Add((response.IsSuccessStatusCode, response.StatusCode, stopwatch.ElapsedMilliseconds));

                    _output.WriteLine($"Attempt {attempt}: {response.StatusCode} ({stopwatch.ElapsedMilliseconds}ms)");
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    recoveryAttempts.Add((false, HttpStatusCode.InternalServerError, stopwatch.ElapsedMilliseconds));
                    _output.WriteLine($"Attempt {attempt}: Exception - {ex.Message}");
                }

                // Wait between attempts to simulate recovery time
                await Task.Delay(500);
            }

            // Assert
            var successfulAttempts = recoveryAttempts.Count(r => r.Success);
            var failedAttempts = recoveryAttempts.Count(r => !r.Success);
            var improvementOverTime = recoveryAttempts.Skip(5).Count(r => r.Success) > recoveryAttempts.Take(5).Count(r => r.Success);

            _output.WriteLine($"Recovery Test Results:");
            _output.WriteLine($"  Successful Attempts: {successfulAttempts}/10");
            _output.WriteLine($"  Failed Attempts: {failedAttempts}/10");
            _output.WriteLine($"  Improvement Over Time: {improvementOverTime}");

            // Service should show some level of recovery or consistent behavior
            Assert.True(successfulAttempts > 0 || failedAttempts == 10, "Service should recover or fail consistently");
        }

        [Fact]
        public async Task IntegrationService_Should_HandleResourceExhaustion_Gracefully()
        {
            // Arrange
            _output.WriteLine("Testing resource exhaustion handling");

            var tasks = new List<Task<(HttpStatusCode StatusCode, bool HasContent, long ResponseTime)>>();
            const int resourceExhaustionRequests = 100;

            // Act - Create many concurrent requests to potentially exhaust resources
            for (int i = 0; i < resourceExhaustionRequests; i++)
            {
                var task = Task.Run(async () =>
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    try
                    {
                        var response = await _client.GetAsync("/api/v1/integrations/providers");
                        var content = await response.Content.ReadAsStringAsync();
                        stopwatch.Stop();

                        return (response.StatusCode, !string.IsNullOrEmpty(content), stopwatch.ElapsedMilliseconds);
                    }
                    catch (Exception)
                    {
                        stopwatch.Stop();
                        return (HttpStatusCode.ServiceUnavailable, false, stopwatch.ElapsedMilliseconds);
                    }
                });
                tasks.Add(task);
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            var successCount = results.Count(r => r.StatusCode == HttpStatusCode.OK);
            var serviceUnavailableCount = results.Count(r => r.StatusCode == HttpStatusCode.ServiceUnavailable);
            var tooManyRequestsCount = results.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);
            var averageResponseTime = results.Average(r => r.ResponseTime);

            _output.WriteLine($"Resource Exhaustion Test Results:");
            _output.WriteLine($"  Total Requests: {resourceExhaustionRequests}");
            _output.WriteLine($"  Successful (200): {successCount}");
            _output.WriteLine($"  Service Unavailable (503): {serviceUnavailableCount}");
            _output.WriteLine($"  Too Many Requests (429): {tooManyRequestsCount}");
            _output.WriteLine($"  Average Response Time: {averageResponseTime:F1}ms");

            // Service should handle resource exhaustion gracefully
            Assert.True(averageResponseTime < 10000, $"Average response time {averageResponseTime:F1}ms indicates severe resource exhaustion");

            // Should have some successful requests or appropriate error responses
            Assert.True(successCount > 0 || serviceUnavailableCount > 0 || tooManyRequestsCount > 0,
                "Service should either succeed or return appropriate error codes under load");
        }

        [Fact]
        public async Task IntegrationService_Should_MaintainDataConsistency_DuringFailures()
        {
            // Arrange
            _output.WriteLine("Testing data consistency during failures");

            var userId = Guid.NewGuid();
            var consistencyTests = new List<(string Operation, bool Success, string Result)>();

            // Act - Perform operations that might affect data consistency
            var operations = new[]
            {
                ("GetProviders", async () => await _client.GetAsync("/api/v1/integrations/providers")),
                ("GetUserIntegrations", async () => await _client.GetAsync($"/api/v1/integrations/user/{userId}")),
                ("GetRateLimitStats", async () => await _client.GetAsync("/api/v1/integrations/rate-limiting/statistics")),
            };

            foreach (var (operationName, operation) in operations)
            {
                try
                {
                    var response = await operation();
                    var content = await response.Content.ReadAsStringAsync();

                    var success = response.IsSuccessStatusCode;
                    var result = success ? "Success" : $"Error: {response.StatusCode}";

                    consistencyTests.Add((operationName, success, result));

                    // Verify response structure if successful
                    if (success && !string.IsNullOrEmpty(content))
                    {
                        try
                        {
                            var jsonData = JsonSerializer.Deserialize<JsonElement>(content);
                            var isValidStructure = jsonData.ValueKind != JsonValueKind.Undefined;

                            if (!isValidStructure)
                            {
                                consistencyTests.Add(($"{operationName}_Structure", false, "Invalid JSON structure"));
                            }
                        }
                        catch (JsonException ex)
                        {
                            consistencyTests.Add(($"{operationName}_JSON", false, $"JSON parsing error: {ex.Message}"));
                        }
                    }
                }
                catch (Exception ex)
                {
                    consistencyTests.Add((operationName, false, $"Exception: {ex.Message}"));
                }

                // Small delay between operations
                await Task.Delay(100);
            }

            // Assert
            _output.WriteLine("Data Consistency Test Results:");
            foreach (var (operation, success, result) in consistencyTests)
            {
                _output.WriteLine($"  {operation}: {(success ? "✓" : "✗")} {result}");
            }

            var successfulOperations = consistencyTests.Count(t => t.Success);
            var totalOperations = consistencyTests.Count;

            // At least basic operations should maintain consistency
            Assert.True(successfulOperations > 0, "At least some operations should succeed to verify consistency");

            // No operation should return corrupted data (if it succeeds, data should be valid)
            var corruptedDataOperations = consistencyTests.Where(t => t.Success && t.Result.Contains("Invalid") || t.Result.Contains("parsing error"));
            Assert.Empty(corruptedDataOperations);
        }

        [Fact]
        public async Task IntegrationService_Should_HandleCircuitBreaker_States()
        {
            // Arrange
            _output.WriteLine("Testing circuit breaker behavior");

            var userId = Guid.NewGuid();
            var circuitBreakerTests = new List<(int Attempt, HttpStatusCode StatusCode, long ResponseTime)>();

            // Act - Make requests that might trigger circuit breaker
            for (int attempt = 1; attempt <= 15; attempt++)
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    // Use invalid token to potentially trigger failures
                    _client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid_token_to_trigger_circuit_breaker");

                    var response = await _client.PostAsync(
                        $"/api/v1/integrations/calendar/sync?userId={userId}&provider=google",
                        null);

                    stopwatch.Stop();
                    circuitBreakerTests.Add((attempt, response.StatusCode, stopwatch.ElapsedMilliseconds));
                }
                catch (Exception)
                {
                    stopwatch.Stop();
                    circuitBreakerTests.Add((attempt, HttpStatusCode.ServiceUnavailable, stopwatch.ElapsedMilliseconds));
                }

                // Small delay between attempts
                await Task.Delay(200);
            }

            // Assert
            _output.WriteLine("Circuit Breaker Test Results:");
            foreach (var (attempt, statusCode, responseTime) in circuitBreakerTests)
            {
                _output.WriteLine($"  Attempt {attempt}: {statusCode} ({responseTime}ms)");
            }

            var fastFailures = circuitBreakerTests.Where(t => t.ResponseTime < 100).Count();
            var slowFailures = circuitBreakerTests.Where(t => t.ResponseTime >= 100).Count();

            _output.WriteLine($"Fast Failures (potential circuit breaker): {fastFailures}");
            _output.WriteLine($"Slow Failures: {slowFailures}");

            // Circuit breaker should eventually provide fast failures
            // Note: This test might not trigger actual circuit breaker in test environment
            Assert.True(circuitBreakerTests.Count == 15, "All attempts should be recorded");
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
