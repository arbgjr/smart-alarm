import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');
const responseTime = new Trend('response_time');

// Spike test configuration - sudden load increases
export const options = {
  stages: [
    { duration: '2m', target: 10 },   // Normal load
    { duration: '30s', target: 500 }, // Sudden spike to 500 users
    { duration: '1m', target: 500 },  // Stay at spike level
    { duration: '30s', target: 10 },  // Drop back to normal
    { duration: '2m', target: 10 },   // Normal load
    { duration: '30s', target: 1000 }, // Even bigger spike
    { duration: '1m', target: 1000 }, // Stay at high spike
    { duration: '30s', target: 0 },   // Drop to zero
  ],
  thresholds: {
    http_req_duration: ['p(95)<10000'], // 95% of requests should be below 10s (very lenient for spike test)
    http_req_failed: ['rate<0.2'],      // Error rate should be below 20%
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

export default function () {
  // During spike test, focus on the most critical endpoints
  const criticalEndpoints = [
    { url: '/health', weight: 0.3 },
    { url: '/', weight: 0.4 },
    { url: '/api/health', weight: 0.2 },
    { url: '/api/status', weight: 0.1 },
  ];

  // Select endpoint based on weight
  let random = Math.random();
  let selectedEndpoint = '/health'; // default

  for (const endpoint of criticalEndpoints) {
    if (random < endpoint.weight) {
      selectedEndpoint = endpoint.url;
      break;
    }
    random -= endpoint.weight;
  }

  const response = http.get(`${BASE_URL}${selectedEndpoint}`);

  const success = check(response, {
    'request completed': (r) => r.status !== 0, // Any response is good during spike
    'response time under 30s': (r) => r.timings.duration < 30000, // Very lenient during spike
    'not server error': (r) => r.status < 500 || r.status === 503, // 503 is acceptable during overload
  });

  errorRate.add(!success);
  responseTime.add(response.timings.duration);

  // Very short sleep during spike test
  sleep(0.1);
}

export function setup() {
  console.log('Starting spike test...');
  console.log(`Target URL: ${BASE_URL}`);
  console.log('This test simulates sudden traffic spikes');

  // Test if the service is available before starting
  const healthResponse = http.get(`${BASE_URL}/health`);
  console.log(`Initial health check: ${healthResponse.status}`);

  return { baseUrl: BASE_URL };
}

export function teardown(data) {
  console.log('Spike test completed');

  // Final health check to see if service recovered
  const finalHealthResponse = http.get(`${data.baseUrl}/health`);
  console.log(`Final health check: ${finalHealthResponse.status}`);
}
