// Test configuration for Smart Alarm E2E tests

export const config = {
  frontend: {
    baseUrl: process.env.FRONTEND_BASE_URL || 'http://localhost:3000',
    timeout: 30000,
  },
  api: {
    baseUrl: process.env.API_BASE_URL || 'http://localhost:8080/api/v1',
    timeout: 10000,
  },
  oauth: {
    providers: ['google', 'github', 'facebook', 'microsoft'],
    timeout: 15000,
    redirectUri: process.env.OAUTH_REDIRECT_URI || 'http://localhost:3000/auth/callback',
  },
  test: {
    slowMo: process.env.CI ? 0 : 100, // Slow down actions in non-CI environments
    headless: process.env.CI ? true : false,
    screenshots: process.env.CI ? 'only-on-failure' : 'on',
    videos: process.env.CI ? 'retain-on-failure' : 'on',
  },
  database: {
    resetBetweenTests: process.env.NODE_ENV === 'test',
  },
  auth: {
    testUser: {
      email: 'test@smartalarm.com',
      password: 'Test123!@#',
      name: 'Test User',
    },
    adminUser: {
      email: 'admin@smartalarm.com', 
      password: 'Admin123!@#',
      name: 'Admin User',
    },
  },
} as const;

// Helper functions for test configuration
export const getTestTimeout = (operation: keyof typeof config.frontend | keyof typeof config.api | keyof typeof config.oauth) => {
  return config.frontend[operation as keyof typeof config.frontend] || 
         config.api[operation as keyof typeof config.api] || 
         config.oauth[operation as keyof typeof config.oauth] || 
         30000;
};

export const isCI = () => Boolean(process.env.CI);
export const isDevelopment = () => process.env.NODE_ENV === 'development';
export const isTest = () => process.env.NODE_ENV === 'test';