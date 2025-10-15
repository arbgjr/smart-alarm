import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');
const responseTime = new Trend('response_time');

// Test configuration
export const options = {
  stages: [
    { duration: '2m', target: 10 }, // Ramp up to 10 users over 2 minutes
    { duration: '5m', target: 10 }, // Stay at 10 users for 5 minutes
    { duration: '2m', target: 20 }, // Ramp up to 20 users over 2 minutes
    { duration: '5m', target: 20 }, // Stay at 20 users for 5 minutes
    { duration: '2m', target: 0 },  // Ramp down to 0 users over 2 minutes
  ],
  thresholds: {
    http_req_duration: ['p(95)<2000'], // 95% of requests should be below 2s
    http_req_failed: ['rate<0.05'],    // Error rate should be below 5%
    errors: ['rate<0.05'],             // Custom error rate should be below 5%
  },
};

// Base URL - can be overridden with environment variable
const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

// Test data
const testUsers = [
  { email: 'test1@example.com', password: 'TestPassword123!' },
  { email: 'test2@example.com', password: 'TestPassword123!' },
  { email: 'test3@example.com', password: 'TestPassword123!' },
];

// Helper function to get random test user
function getRandomUser() {
  return testUsers[Math.floor(Math.random() * testUsers.length)];
}

// Helper function to authenticate and get token
function authenticate() {
  const user = getRandomUser();

  const loginResponse = http.post(`${BASE_URL}/api/auth/login`, JSON.stringify({
    email: user.email,
    password: user.password
  }), {
    headers: { 'Content-Type': 'application/json' },
  });

  const success = check(loginResponse, {
    'login successful': (r) => r.status === 200,
    'login response has token': (r) => {
      try {
        const body = JSON.parse(r.body);
        return body.token !== undefined;
      } catch {
        return false;
      }
    },
  });

  if (success && loginResponse.status === 200) {
    try {
      const body = JSON.parse(loginResponse.body);
      return body.token;
    } catch {
      return null;
    }
  }

  return null;
}

// Main test function
export default function () {
  // Test 1: Health Check
  testHealthCheck();

  // Test 2: Static Assets
  testStaticAssets();

  // Test 3: API Authentication
  const token = testAuthentication();

  if (token) {
    // Test 4: Authenticated API calls
    testAlarmOperations(token);
    testUserOperations(token);
  }

  // Small delay between iterations
  sleep(1);
}

function testHealthCheck() {
  const response = http.get(`${BASE_URL}/health`);

  const success = check(response, {
    'health check status is 200': (r) => r.status === 200,
    'health check response time < 500ms': (r) => r.timings.duration < 500,
  });

  errorRate.add(!success);
  responseTime.add(response.timings.duration);
}

function testStaticAssets() {
  // Test loading of common static assets
  const assets = [
    '/',
    '/favicon.ico',
    '/manifest.json',
  ];

  assets.forEach(asset => {
    const response = http.get(`${BASE_URL}${asset}`);

    const success = check(response, {
      [`${asset} loads successfully`]: (r) => r.status === 200 || r.status === 404, // 404 is acceptable for optional assets
      [`${asset} response time < 1s`]: (r) => r.timings.duration < 1000,
    });

    errorRate.add(!success);
    responseTime.add(response.timings.duration);
  });
}

function testAuthentication() {
  const user = getRandomUser();

  const response = http.post(`${BASE_URL}/api/auth/login`, JSON.stringify({
    email: user.email,
    password: user.password
  }), {
    headers: { 'Content-Type': 'application/json' },
  });

  const success = check(response, {
    'auth request status is 200 or 401': (r) => r.status === 200 || r.status === 401,
    'auth response time < 2s': (r) => r.timings.duration < 2000,
  });

  errorRate.add(!success);
  responseTime.add(response.timings.duration);

  // Return token if authentication was successful
  if (response.status === 200) {
    try {
      const body = JSON.parse(response.body);
      return body.token;
    } catch {
      return null;
    }
  }

  return null;
}

function testAlarmOperations(token) {
  const headers = {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`,
  };

  // Test: Get alarms
  const getAlarmsResponse = http.get(`${BASE_URL}/api/alarms`, { headers });

  check(getAlarmsResponse, {
    'get alarms status is 200 or 401': (r) => r.status === 200 || r.status === 401,
    'get alarms response time < 1s': (r) => r.timings.duration < 1000,
  });

  responseTime.add(getAlarmsResponse.timings.duration);

  // Test: Create alarm (if get was successful)
  if (getAlarmsResponse.status === 200) {
    const newAlarm = {
      name: `Load Test Alarm ${Date.now()}`,
      time: '07:30',
      enabled: true,
      daysOfWeek: ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday']
    };

    const createResponse = http.post(`${BASE_URL}/api/alarms`, JSON.stringify(newAlarm), { headers });

    const createSuccess = check(createResponse, {
      'create alarm status is 201 or 400': (r) => r.status === 201 || r.status === 400 || r.status === 401,
      'create alarm response time < 2s': (r) => r.timings.duration < 2000,
    });

    errorRate.add(!createSuccess);
    responseTime.add(createResponse.timings.duration);

    // Test: Update alarm (if create was successful)
    if (createResponse.status === 201) {
      try {
        const createdAlarm = JSON.parse(createResponse.body);
        const alarmId = createdAlarm.id;

        const updateData = {
          ...newAlarm,
          name: `Updated ${newAlarm.name}`,
          enabled: false
        };

        const updateResponse = http.put(`${BASE_URL}/api/alarms/${alarmId}`, JSON.stringify(updateData), { headers });

        check(updateResponse, {
          'update alarm status is 200 or 404': (r) => r.status === 200 || r.status === 404 || r.status === 401,
          'update alarm response time < 2s': (r) => r.timings.duration < 2000,
        });

        responseTime.add(updateResponse.timings.duration);

        // Test: Delete alarm
        const deleteResponse = http.del(`${BASE_URL}/api/alarms/${alarmId}`, null, { headers });

        check(deleteResponse, {
          'delete alarm status is 204 or 404': (r) => r.status === 204 || r.status === 404 || r.status === 401,
          'delete alarm response time < 1s': (r) => r.timings.duration < 1000,
        });

        responseTime.add(deleteResponse.timings.duration);
      } catch (e) {
        // Ignore parsing errors in load test
      }
    }
  }
}

function testUserOperations(token) {
  const headers = {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`,
  };

  // Test: Get user profile
  const profileResponse = http.get(`${BASE_URL}/api/user/profile`, { headers });

  const profileSuccess = check(profileResponse, {
    'get profile status is 200 or 401': (r) => r.status === 200 || r.status === 401,
    'get profile response time < 1s': (r) => r.timings.duration < 1000,
  });

  errorRate.add(!profileSuccess);
  responseTime.add(profileResponse.timings.duration);

  // Test: Get user preferences
  const preferencesResponse = http.get(`${BASE_URL}/api/user/preferences`, { headers });

  check(preferencesResponse, {
    'get preferences status is 200 or 401 or 404': (r) => r.status === 200 || r.status === 401 || r.status === 404,
    'get preferences response time < 1s': (r) => r.timings.duration < 1000,
  });

  responseTime.add(preferencesResponse.timings.duration);
}

// Setup function (runs once per VU at the beginning)
export function setup() {
  console.log('Starting load test...');
  console.log(`Base URL: ${BASE_URL}`);
  console.log(`Test users: ${testUsers.length}`);

  // Verify that the application is running
  const healthResponse = http.get(`${BASE_URL}/health`);
  if (healthResponse.status !== 200) {
    console.warn(`Warning: Health check failed with status ${healthResponse.status}`);
  }

  return { baseUrl: BASE_URL };
}

// Teardown function (runs once at the end)
export function teardown(data) {
  console.log('Load test completed');
  console.log(`Base URL was: ${data.baseUrl}`);
}
