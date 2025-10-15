using Microsoft.AspNetCore.Mvc.Testing;
using System.Diagnostics;
using System.Net.Http.Json;
using Xunit;
using Xunit.Abstractions;

namespace SmartAlarm.IntegrationService.Tests.Performance
{
    /// <summary>
    /// Testes de performance para validar comportamento sob carga
    /// </summary>
    public class ServicePerformanceTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly ITestOutputHelper _output;

        public ServicePerformanceTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
        {
            _factory = factory;
            _output = output;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task IntegrationService_Should_HandleHighThroughput_Requests()
        {
            // Arrange
            const int requestCount = 100;
            const int maxAcceptableResponseTimeMs = 5000; // 5 segundos para 100 requisições

            _output.WriteLine($"Testing high throughput with {requestCount} concurrent requests");

            var stopwatch = Stopwatch.StartNew();
            var tasks = new List<Task<(bool Success, long ResponseTimeMs)>>();

            // Act
            for (int i = 0; i < requestCount; i++)
            {
                var taskIndex = i;
                var task = Task.Run(async () =>
                {
                    var requestStopwatch = Stopwatch.StartNew();
                    try
                    {
                        var response = await _client.GetAsync("/api/v1/integrations/providers");
                        requestStopwatch.Stop();

                        return (Success: response.IsSuccessStatusCode, ResponseTimeMs: requestStopwatch.ElapsedMilliseconds);
                    }
                    catch (Exception ex)
                    {
                        requestStopwatch.Stop();
                        _output.WriteLine($"Request {taskIndex} failed: {ex.Message}");
                        return (Success: false, ResponseTimeMs: requestStopwatch.ElapsedMilliseconds);
                    }
                });
                tasks.Add(task);
            }

            var results = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            var successfulRequests = results.Count(r => r.Success);
            var averageResponseTime = results.Average(r => r.ResponseTimeMs);
            var maxResponseTime = results.Max(r => r.ResponseTimeMs);
            var minResponseTime = results.Min(r => r.ResponseTimeMs);
            var totalTime = stopwatch.ElapsedMilliseconds;

            _output.WriteLine($"Performance Results:");
            _output.WriteLine($"  Total Requests: {requestCount}");
            _output.WriteLine($"  Successful: {successfulRequests} ({(double)successfulRequests / requestCount * 100:F1}%)");
            _output.WriteLine($"  Total Time: {totalTime}ms");
            _output.WriteLine($"  Average Response Time: {averageResponseTime:F1}ms");
            _output.WriteLine($"  Min Response Time: {minResponseTime}ms");
            _output.WriteLine($"  Max Response Time: {maxResponseTime}ms");
            _output.WriteLine($"  Throughput: {(double)requestCount / totalTime * 1000:F1} requests/second");

            // Performance assertions
            Assert.True(totalTime < maxAcceptableResponseTimeMs,
                $"Total time {totalTime}ms exceeded maximum acceptable time {maxAcceptableResponseTimeMs}ms");

            Assert.True(successfulRequests > requestCount * 0.8,
                $"Success rate {(double)successfulRequests / requestCount * 100:F1}% is below 80%");

            Assert.True(averageResponseTime < 1000,
                $"Average response time {averageResponseTime:F1}ms is above 1000ms");
        }

        [Fact]
        public async Task IntegrationService_Should_HandleMemoryEfficiently_UnderLoad()
        {
            // Arrange
            const int iterationCount = 50;
            _output.WriteLine($"Testing memory efficiency with {iterationCount} iterations");

            var initialMemory = GC.GetTotalMemory(true);
            var memoryMeasurements = new List<long>();

            // Act
            for (int i = 0; i < iterationCount; i++)
            {
                // Fazer requisições que podem alocar memória
                var userId = Guid.NewGuid();
                var response = await _client.GetAsync($"/api/v1/integrations/user/{userId}?includeStatistics=true");

                // Medir memória a cada 10 iterações
                if (i % 10 == 0)
                {
                    var currentMemory = GC.GetTotalMemory(false);
                    memoryMeasurements.Add(currentMemory);
                    _output.WriteLine($"Iteration {i}: Memory usage: {currentMemory / 1024 / 1024:F1} MB");
                }

                // Pequena pausa para simular carga real
                await Task.Delay(10);
            }

            // Forçar garbage collection e medir memória final
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var finalMemory = GC.GetTotalMemory(true);

            // Assert
            var memoryIncrease = finalMemory - initialMemory;
            var maxMemoryDuringTest = memoryMeasurements.Max();

            _output.WriteLine($"Memory Analysis:");
            _output.WriteLine($"  Initial Memory: {initialMemory / 1024 / 1024:F1} MB");
            _output.WriteLine($"  Final Memory: {finalMemory / 1024 / 1024:F1} MB");
            _output.WriteLine($"  Memory Increase: {memoryIncrease / 1024 / 1024:F1} MB");
            _output.WriteLine($"  Max Memory During Test: {maxMemoryDuringTest / 1024 / 1024:F1} MB");

            // Memory should not increase excessively (less than 100MB increase)
            Assert.True(memoryIncrease < 100 * 1024 * 1024,
                $"Memory increase {memoryIncrease / 1024 / 1024:F1} MB is excessive");
        }

        [Fact]
        public async Task IntegrationService_Should_MaintainResponseTimes_UnderSustainedLoad()
        {
            // Arrange
            const int durationSeconds = 30;
            const int requestsPerSecond = 5;

            _output.WriteLine($"Testing sustained load for {durationSeconds} seconds at {requestsPerSecond} requests/second");

            var endTime = DateTime.UtcNow.AddSeconds(durationSeconds);
            var responseTimes = new List<long>();
            var errors = new List<string>();
            var requestCount = 0;

            // Act
            while (DateTime.UtcNow < endTime)
            {
                var tasks = new List<Task>();

                for (int i = 0; i < requestsPerSecond; i++)
                {
                    var task = Task.Run(async () =>
                    {
                        var stopwatch = Stopwatch.StartNew();
                        try
                        {
                            var response = await _client.GetAsync("/api/v1/integrations/providers");
                            stopwatch.Stop();

                            lock (responseTimes)
                            {
                                responseTimes.Add(stopwatch.ElapsedMilliseconds);
                                requestCount++;
                            }

                            if (!response.IsSuccessStatusCode)
                            {
                                lock (errors)
                                {
                                    errors.Add($"HTTP {response.StatusCode}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            stopwatch.Stop();
                            lock (errors)
                            {
                                errors.Add(ex.Message);
                            }
                        }
                    });
                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);
                await Task.Delay(1000); // Esperar 1 segundo antes do próximo lote
            }

            // Assert
            var averageResponseTime = responseTimes.Any() ? responseTimes.Average() : 0;
            var p95ResponseTime = responseTimes.Any() ? responseTimes.OrderBy(x => x).Skip((int)(responseTimes.Count * 0.95)).First() : 0;
            var errorRate = (double)errors.Count / requestCount * 100;

            _output.WriteLine($"Sustained Load Results:");
            _output.WriteLine($"  Total Requests: {requestCount}");
            _output.WriteLine($"  Average Response Time: {averageResponseTime:F1}ms");
            _output.WriteLine($"  95th Percentile Response Time: {p95ResponseTime}ms");
            _output.WriteLine($"  Error Rate: {errorRate:F1}%");
            _output.WriteLine($"  Total Errors: {errors.Count}");

            if (errors.Any())
            {
                _output.WriteLine($"  Error Types: {string.Join(", ", errors.GroupBy(e => e).Select(g => $"{g.Key} ({g.Count()})"))}");
            }

            // Performance assertions for sustained load
            Assert.True(averageResponseTime < 2000,
                $"Average response time {averageResponseTime:F1}ms is above 2000ms under sustained load");

            Assert.True(p95ResponseTime < 5000,
                $"95th percentile response time {p95ResponseTime}ms is above 5000ms");

            Assert.True(errorRate < 10,
                $"Error rate {errorRate:F1}% is above 10%");
        }

        [Fact]
        public async Task IntegrationService_Should_RecoverFromSpikes_Gracefully()
        {
            // Arrange
            _output.WriteLine("Testing recovery from traffic spikes");

            // Baseline - normal load
            var baselineStopwatch = Stopwatch.StartNew();
            var baselineResponse = await _client.GetAsync("/api/v1/integrations/providers");
            baselineStopwatch.Stop();
            var baselineTime = baselineStopwatch.ElapsedMilliseconds;

            _output.WriteLine($"Baseline response time: {baselineTime}ms");

            // Spike - high concurrent load
            const int spikeRequests = 50;
            var spikeTasks = new List<Task<long>>();

            for (int i = 0; i < spikeRequests; i++)
            {
                var task = Task.Run(async () =>
                {
                    var stopwatch = Stopwatch.StartNew();
                    try
                    {
                        await _client.GetAsync("/api/v1/integrations/providers");
                        stopwatch.Stop();
                        return stopwatch.ElapsedMilliseconds;
                    }
                    catch
                    {
                        stopwatch.Stop();
                        return stopwatch.ElapsedMilliseconds;
                    }
                });
                spikeTasks.Add(task);
            }

            var spikeResults = await Task.WhenAll(spikeTasks);
            var averageSpikeTime = spikeResults.Average();

            _output.WriteLine($"Average response time during spike: {averageSpikeTime:F1}ms");

            // Recovery - wait and test normal load again
            await Task.Delay(2000); // Wait 2 seconds for recovery

            var recoveryStopwatch = Stopwatch.StartNew();
            var recoveryResponse = await _client.GetAsync("/api/v1/integrations/providers");
            recoveryStopwatch.Stop();
            var recoveryTime = recoveryStopwatch.ElapsedMilliseconds;

            _output.WriteLine($"Recovery response time: {recoveryTime}ms");

            // Assert
            _output.WriteLine($"Performance Recovery Analysis:");
            _output.WriteLine($"  Baseline: {baselineTime}ms");
            _output.WriteLine($"  Spike Average: {averageSpikeTime:F1}ms");
            _output.WriteLine($"  Recovery: {recoveryTime}ms");
            _output.WriteLine($"  Recovery vs Baseline: {(double)recoveryTime / baselineTime:F2}x");

            // Service should recover to reasonable performance levels
            Assert.True(recoveryTime < baselineTime * 3,
                $"Recovery time {recoveryTime}ms is more than 3x baseline {baselineTime}ms");

            // Spike shouldn't cause complete failure
            Assert.True(averageSpikeTime < 10000,
                $"Average spike response time {averageSpikeTime:F1}ms is above 10 seconds");
        }

        [Fact]
        public async Task IntegrationService_Should_HandleLargeResponses_Efficiently()
        {
            // Arrange
            _output.WriteLine("Testing large response handling efficiency");

            var userId = Guid.NewGuid();
            var stopwatch = Stopwatch.StartNew();

            // Act - Request that might return large data
            var response = await _client.GetAsync($"/api/v1/integrations/user/{userId}?includeStatistics=true&includeInactive=true");
            stopwatch.Stop();

            // Assert
            var responseTime = stopwatch.ElapsedMilliseconds;
            var contentLength = 0L;

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                contentLength = content.Length;
            }

            _output.WriteLine($"Large Response Test Results:");
            _output.WriteLine($"  Response Time: {responseTime}ms");
            _output.WriteLine($"  Content Length: {contentLength} characters");
            _output.WriteLine($"  Status Code: {response.StatusCode}");

            // Performance should be reasonable even for large responses
            if (response.IsSuccessStatusCode)
            {
                Assert.True(responseTime < 3000,
                    $"Response time {responseTime}ms for large response is above 3000ms");
            }
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
