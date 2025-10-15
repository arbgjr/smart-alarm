# Performance Tests

This directory contains performance and load testing tools for the Smart Alarm application.

## Available Tests

### 1. Node.js API Performance Tests (`api-performance.js`)

- Basic API endpoint testing
- Response time measurement
- Load testing with configurable concurrency
- Built-in performance thresholds
- No external dependencies (uses Node.js built-in modules)

**Usage:**

```bash
node api-performance.js [BASE_URL]
```

**Example:**

```bash
node api-performance.js http://localhost:5000
```

### 2. k6 Load Tests (`load-test.js`)

- Comprehensive load testing with realistic user scenarios
- Authentication testing
- CRUD operations testing
- Configurable load stages
- Custom metrics and thresholds

**Usage:**

```bash
k6 run load-test.js
# or with custom URL
BASE_URL=http://localhost:5000 k6 run load-test.js
```

### 3. k6 Stress Tests (`stress-test.js`)

- Gradually increases load to find breaking points
- Tests system behavior under extreme load
- Identifies performance degradation points

**Usage:**

```bash
k6 run stress-test.js
```

### 4. k6 Spike Tests (`spike-test.js`)

- Tests system behavior during sudden traffic spikes
- Simulates viral traffic or DDoS-like conditions
- Validates system recovery after spikes

**Usage:**

```bash
k6 run spike-test.js
```

## Test Runners

### PowerShell Runner (`simple-perf-test.ps1`)

Simple PowerShell script to run Node.js performance tests.

**Usage:**

```powershell
powershell -ExecutionPolicy Bypass -File simple-perf-test.ps1 -BaseUrl http://localhost:5000
```

### Advanced PowerShell Runner (`run-performance-tests.ps1`)

Comprehensive test runner with multiple test types and tool detection.

**Usage:**

```powershell
# Run all available tests
powershell -ExecutionPolicy Bypass -File run-performance-tests.ps1 -TestType all -BaseUrl http://localhost:5000

# Run specific test type
powershell -ExecutionPolicy Bypass -File run-performance-tests.ps1 -TestType load -BaseUrl http://localhost:5000
```

## Prerequisites

### For Node.js Tests

- Node.js (v14 or higher)
- No additional dependencies required

### For k6 Tests

- k6 installation required
- Install from: https://k6.io/docs/getting-started/installation/

**Windows:**

```powershell
winget install k6
```

**macOS:**

```bash
brew install k6
```

**Linux:**

```bash
sudo gpg -k
sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
sudo apt-get update
sudo apt-get install k6
```

## Performance Thresholds

### Default Thresholds

- **Response Time**: 95th percentile < 2000ms
- **Error Rate**: < 5%
- **Success Rate**: > 95%

### Load Test Thresholds

- **Concurrent Users**: 10-20 users
- **Test Duration**: 5-15 minutes
- **Requests per Second**: Target varies by endpoint

### Stress Test Thresholds

- **Peak Load**: Up to 300 concurrent users
- **Error Rate**: < 10% (more lenient for stress testing)
- **Response Time**: 95th percentile < 5000ms

## Interpreting Results

### Good Performance Indicators

- Response times consistently under 1000ms
- Error rate below 1%
- Linear scaling with increased load
- Quick recovery after load spikes

### Warning Signs

- Response times increasing exponentially with load
- Error rates above 5%
- Memory leaks (increasing response times over time)
- System not recovering after load spikes

### Critical Issues

- Response times above 10 seconds
- Error rates above 20%
- System crashes or becomes unresponsive
- Data corruption or inconsistency

## Continuous Integration

### GitHub Actions Example

```yaml
name: Performance Tests
on:
  schedule:
    - cron: "0 2 * * *" # Run daily at 2 AM
  workflow_dispatch:

jobs:
  performance:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-node@v3
        with:
          node-version: "18"

      - name: Install k6
        run: |
          sudo gpg -k
          sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
          echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
          sudo apt-get update
          sudo apt-get install k6

      - name: Run Performance Tests
        run: |
          cd tests/performance
          node api-performance.js ${{ secrets.STAGING_URL }}
          BASE_URL=${{ secrets.STAGING_URL }} k6 run load-test.js
```

## Monitoring Integration

### Grafana Dashboard

The k6 tests can be integrated with Grafana for real-time monitoring:

```bash
# Run k6 with InfluxDB output
k6 run --out influxdb=http://localhost:8086/k6 load-test.js
```

### Prometheus Metrics

Custom metrics can be exported to Prometheus for alerting:

```javascript
// In k6 test files
import { Counter, Rate, Trend } from "k6/metrics";

const errorRate = new Rate("errors");
const responseTime = new Trend("response_time");
```

## Troubleshooting

### Common Issues

1. **Connection Refused**

   - Ensure the application is running
   - Check firewall settings
   - Verify the correct URL and port

2. **High Error Rates**

   - Check application logs
   - Verify database connections
   - Monitor system resources

3. **Slow Response Times**

   - Check database query performance
   - Monitor CPU and memory usage
   - Review application profiling data

4. **k6 Installation Issues**
   - Verify system requirements
   - Check PATH environment variable
   - Try alternative installation methods

### Debug Mode

Run tests with verbose output:

```bash
# Node.js tests with debug info
DEBUG=* node api-performance.js

# k6 tests with verbose output
k6 run --verbose load-test.js
```

## Best Practices

1. **Test Environment**

   - Use dedicated performance testing environment
   - Mirror production configuration
   - Isolate from other testing activities

2. **Test Data**

   - Use realistic test data volumes
   - Clean up test data after runs
   - Avoid testing with production data

3. **Baseline Establishment**

   - Run tests regularly to establish baselines
   - Track performance trends over time
   - Set up alerts for performance regressions

4. **Load Patterns**

   - Test with realistic user behavior patterns
   - Include peak load scenarios
   - Test gradual load increases

5. **Monitoring**
   - Monitor system resources during tests
   - Track application-specific metrics
   - Correlate performance with system changes
