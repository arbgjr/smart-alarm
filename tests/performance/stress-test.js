import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend, Counter } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');
const responseTime = new Trend('response_time');
const requestCount = new Counter('requests');

// Stress test configuration - gradually increase load to find breaking point
export const options = {
  stages: [
    { duration: '1m', target: 10 },   // Warm up
    { duration: '2m', target: 50 },   // Ramp up to 50 users
    { duration: '2m', target: 100 },  // Ramp up to 100 users
    { duration: '2m', target: 200 },  // Ramp up to 200 users
    { duration: '5m', target: 200 },  // Stay at 200 users for 5 minutes
    { duration: '2m', target: 300 },  // Push to 300 users
    { duration: '5m', target: 300 },  // Stay at 300 users
    { duration: '1m', target: 0 },    // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<5000'], // 95% of requests should be below 5s (more lenient for stress test)
    http_req_failed: ['rate<0.1'],     // Error rate should be below 10%
    errors: ['rate<0.1'],              // Custom error rate should be below 10%
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

export default function () {
  requestCount.add(1);

  // Stress test focuses on high-volume simple operations
  const testType = Math.random();

  if (testType < 0.4) {
    // 40% - Health checks and static content
    testHealthAndStatic();
  } else if (testType < 0.7) {
    // 30% - Authentication attempts
    testAuthentication();
  } else {
    // 30% - API operations
    testApiOperations();
  }

  // Shorter sleep for stress test
  sleep(Math.random() * 0.5);
}

function testHealthAndStatic() {
  const endpoints = [
    '/health',
    '/',
    '/favicon.ico',
  ];

  const endpoint = endpoints[Math.floor(Math.random() * endpoints.length)];
  const response = http.get(`${BASE_URL}${endpoint}`);

  const success = check(response, {
    'status is acceptable': (r) => r.status < 500, // Accept any non-server-error status
    'response time reasonable': (r) => r.timings.duration < 10000, // 10s max for stress test
  });

  errorRate.add(!success);
  responseTime.add(response.timings.duration);
}

function testAuthentication() {
  const credentials = {
    email: `stress-test-${Math.floor(Math.random() * 1000)}@example.com`,
    password: 'TestPassword123!'
  };

  const response = http.post(`${BASE_URL}/api/auth/login`, JSON.stringify(credentials), {
    headers: { 'Content-Type': 'application/json' },
  });

  const success = check(response, {
    'auth response received': (r) => r.status !== 0, // Any response is better than timeout
    'auth response time reasonable': (r) => r.timings.duration < 15000, // 15s max
  });

  errorRate.add(!success);
  responseTime.add(response.timings.duration);
}

function testApiOperations() {
  // Test various API endpoints without authentication
  const endpoints = [
    '/api/health',
    '/api/version',
    '/api/status',
  ];

  const endpoint = endpoints[Math.floor(Math.random() * endpoints.length)];
  const response = http.get(`${BASE_URL}${endpoint}`);

  const success = check(response, {
    'api response received': (r) => r.status !== 0,
    'api response time reasonable': (r) => r.timings.duration < 15000,
  });

  errorRate.add(!success);
  responseTime.add(response.timings.duration);
}

export function setup() {
  console.log('Starting stress test...');
  console.log(`Target URL: ${BASE_URL}`);
  console.log('This test will gradually increase load to find the breaking point');

  return { startTime: Date.now() };
}

export function teardown(data) {
  const duration = (Date.now() - data.startTime) / 1000;
  console.log(`Stress test completed in ${duration} seconds`);
}
