// Simple API performance test using Node.js
const https = require('https');
const http = require('http');

class PerformanceTest {
  constructor(baseUrl = 'https://example.com') {
    this.baseUrl = baseUrl;
    this.results = [];
  }

  async makeRequest(path, method = 'GET', data = null) {
    return new Promise((resolve, reject) => {
      const url = new URL(path, this.baseUrl);
      const isHttps = url.protocol === 'https:';
      const client = isHttps ? https : http;

      const options = {
        hostname: url.hostname,
        port: url.port || (isHttps ? 443 : 80),
        path: url.pathname + url.search,
        method: method,
        headers: {
          'User-Agent': 'SmartAlarm-PerformanceTest/1.0',
          'Accept': 'application/json, text/html, */*',
        }
      };

      if (data) {
        options.headers['Content-Type'] = 'application/json';
        options.headers['Content-Length'] = Buffer.byteLength(data);
      }

      const startTime = Date.now();

      const req = client.request(options, (res) => {
        let responseData = '';

        res.on('data', (chunk) => {
          responseData += chunk;
        });

        res.on('end', () => {
          const endTime = Date.now();
          const duration = endTime - startTime;

          resolve({
            status: res.statusCode,
            duration: duration,
            size: responseData.length,
            headers: res.headers,
            data: responseData
          });
        });
      });

      req.on('error', (error) => {
        const endTime = Date.now();
        const duration = endTime - startTime;

        reject({
          error: error.message,
          duration: duration
        });
      });

      req.setTimeout(30000, () => {
        req.destroy();
        reject({
          error: 'Request timeout',
          duration: 30000
        });
      });

      if (data) {
        req.write(data);
      }

      req.end();
    });
  }

  async runSingleTest(name, path, expectedStatus = 200) {
    console.log(`Testing: ${name}`);

    try {
      const result = await this.makeRequest(path);

      const success = result.status === expectedStatus ||
                     (expectedStatus === 200 && result.status < 400);

      const testResult = {
        name,
        path,
        success,
        status: result.status,
        duration: result.duration,
        size: result.size,
        timestamp: new Date().toISOString()
      };

      this.results.push(testResult);

      console.log(`  ✓ ${name}: ${result.status} (${result.duration}ms, ${result.size} bytes)`);

      return testResult;
    } catch (error) {
      const testResult = {
        name,
        path,
        success: false,
        status: 0,
        duration: error.duration || 0,
        size: 0,
        error: error.error,
        timestamp: new Date().toISOString()
      };

      this.results.push(testResult);

      console.log(`  ✗ ${name}: ${error.error} (${error.duration || 0}ms)`);

      return testResult;
    }
  }

  async runLoadTest(name, path, concurrency = 5, requests = 20) {
    console.log(`\nRunning load test: ${name} (${concurrency} concurrent, ${requests} total requests)`);

    const startTime = Date.now();
    const promises = [];
    const results = [];

    for (let i = 0; i < requests; i++) {
      const promise = this.makeRequest(path)
        .then(result => {
          results.push({
            success: true,
            duration: result.duration,
            status: result.status,
            size: result.size
          });
        })
        .catch(error => {
          results.push({
            success: false,
            duration: error.duration || 0,
            status: 0,
            size: 0,
            error: error.error
          });
        });

      promises.push(promise);

      // Control concurrency
      if (promises.length >= concurrency) {
        await Promise.all(promises.splice(0, concurrency));
      }

      // Small delay between requests
      await new Promise(resolve => setTimeout(resolve, 10));
    }

    // Wait for remaining requests
    if (promises.length > 0) {
      await Promise.all(promises);
    }

    const totalTime = Date.now() - startTime;

    // Calculate statistics
    const successfulRequests = results.filter(r => r.success);
    const failedRequests = results.filter(r => !r.success);
    const durations = successfulRequests.map(r => r.duration);

    const stats = {
      name,
      totalRequests: requests,
      successfulRequests: successfulRequests.length,
      failedRequests: failedRequests.length,
      successRate: (successfulRequests.length / requests) * 100,
      totalTime,
      requestsPerSecond: requests / (totalTime / 1000),
      averageResponseTime: durations.length > 0 ? durations.reduce((a, b) => a + b, 0) / durations.length : 0,
      minResponseTime: durations.length > 0 ? Math.min(...durations) : 0,
      maxResponseTime: durations.length > 0 ? Math.max(...durations) : 0,
      p95ResponseTime: durations.length > 0 ? this.calculatePercentile(durations, 95) : 0
    };

    console.log(`  Results:`);
    console.log(`    Total requests: ${stats.totalRequests}`);
    console.log(`    Successful: ${stats.successfulRequests} (${stats.successRate.toFixed(1)}%)`);
    console.log(`    Failed: ${stats.failedRequests}`);
    console.log(`    Total time: ${stats.totalTime}ms`);
    console.log(`    Requests/sec: ${stats.requestsPerSecond.toFixed(2)}`);
    console.log(`    Avg response time: ${stats.averageResponseTime.toFixed(2)}ms`);
    console.log(`    Min response time: ${stats.minResponseTime}ms`);
    console.log(`    Max response time: ${stats.maxResponseTime}ms`);
    console.log(`    95th percentile: ${stats.p95ResponseTime}ms`);

    return stats;
  }

  calculatePercentile(values, percentile) {
    const sorted = values.slice().sort((a, b) => a - b);
    const index = Math.ceil((percentile / 100) * sorted.length) - 1;
    return sorted[index] || 0;
  }

  async runFullTestSuite() {
    console.log(`Starting performance tests for: ${this.baseUrl}\n`);

    // Basic endpoint tests
    await this.runSingleTest('Home Page', '/');
    await this.runSingleTest('Health Check', '/health');
    await this.runSingleTest('API Health', '/api/health');
    await this.runSingleTest('Non-existent Page', '/non-existent', 404);

    // Load tests
    const loadTestResults = [];

    loadTestResults.push(await this.runLoadTest('Home Page Load Test', '/', 3, 10));
    loadTestResults.push(await this.runLoadTest('Health Check Load Test', '/health', 5, 20));

    // Summary
    console.log('\n=== PERFORMANCE TEST SUMMARY ===');

    const successfulTests = this.results.filter(r => r.success).length;
    const totalTests = this.results.length;

    console.log(`Single tests: ${successfulTests}/${totalTests} passed`);

    loadTestResults.forEach(result => {
      console.log(`${result.name}: ${result.successRate.toFixed(1)}% success rate, ${result.requestsPerSecond.toFixed(2)} req/s`);
    });

    // Performance thresholds
    console.log('\n=== PERFORMANCE THRESHOLDS ===');

    const avgResponseTime = this.results
      .filter(r => r.success && r.duration > 0)
      .reduce((sum, r, _, arr) => sum + r.duration / arr.length, 0);

    console.log(`Average response time: ${avgResponseTime.toFixed(2)}ms ${avgResponseTime < 2000 ? '✓' : '✗'} (threshold: <2000ms)`);

    const successRate = (successfulTests / totalTests) * 100;
    console.log(`Success rate: ${successRate.toFixed(1)}% ${successRate >= 95 ? '✓' : '✗'} (threshold: >=95%)`);

    return {
      singleTests: this.results,
      loadTests: loadTestResults,
      summary: {
        totalTests,
        successfulTests,
        successRate,
        avgResponseTime
      }
    };
  }
}

// Run tests if this file is executed directly
if (require.main === module) {
  const baseUrl = process.argv[2] || 'https://example.com';
  const test = new PerformanceTest(baseUrl);

  test.runFullTestSuite()
    .then(results => {
      console.log('\nPerformance tests completed!');

      // Exit with error code if tests failed
      const overallSuccess = results.summary.successRate >= 95 && results.summary.avgResponseTime < 2000;
      process.exit(overallSuccess ? 0 : 1);
    })
    .catch(error => {
      console.error('Performance tests failed:', error);
      process.exit(1);
    });
}

module.exports = PerformanceTest;
