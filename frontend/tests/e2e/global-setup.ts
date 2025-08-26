import { chromium, FullConfig } from '@playwright/test';
import { execSync } from 'child_process';
import path from 'path';

async function globalSetup(config: FullConfig) {
  console.log('🚀 Starting E2E test setup...');

  try {
    // Check if Docker is available
    try {
      execSync('docker --version', { stdio: 'pipe' });
      console.log('✅ Docker is available');
    } catch (error) {
      console.warn('⚠️  Docker not available, skipping containerized backend setup');
      return;
    }

    // Set up test database using Docker
    console.log('🐳 Setting up test database...');
    await setupTestDatabase();

    // Start backend services if needed
    console.log('🔧 Starting backend services...');
    await startBackendServices();

    // Wait for services to be ready
    console.log('⏳ Waiting for services to be ready...');
    await waitForServices();

    // Set up test users and data
    console.log('👤 Setting up test data...');
    await setupTestData();

    console.log('✅ E2E test setup completed successfully');

  } catch (error) {
    console.error('❌ E2E test setup failed:', error);
    throw error;
  }
}

async function setupTestDatabase(): Promise<void> {
  try {
    // Check if test database container is running
    try {
      execSync('docker ps --format "table {{.Names}}" | grep smart-alarm-test-db', { stdio: 'pipe' });
      console.log('📦 Test database container already running');
      return;
    } catch {
      // Container not running, need to start it
    }

    // Start PostgreSQL test database
    const dbCommand = `
      docker run -d \
        --name smart-alarm-test-db \
        --rm \
        -e POSTGRES_DB=smartalarm_test \
        -e POSTGRES_USER=testuser \
        -e POSTGRES_PASSWORD=testpass \
        -p 5433:5432 \
        postgres:15-alpine
    `;

    execSync(dbCommand, { stdio: 'inherit' });
    
    // Wait for database to be ready
    let attempts = 0;
    const maxAttempts = 30;
    
    while (attempts < maxAttempts) {
      try {
        execSync(`docker exec smart-alarm-test-db pg_isready -U testuser -d smartalarm_test`, { stdio: 'pipe' });
        console.log('✅ Test database is ready');
        break;
      } catch {
        attempts++;
        console.log(`⏳ Waiting for database... (${attempts}/${maxAttempts})`);
        await new Promise(resolve => setTimeout(resolve, 2000));
      }
    }

    if (attempts >= maxAttempts) {
      throw new Error('Database failed to start within timeout');
    }

    // Run database migrations if needed
    console.log('🔄 Running database migrations...');
    // This would run your actual migration scripts
    // execSync('npm run db:migrate:test', { stdio: 'inherit' });

  } catch (error) {
    console.error('Failed to setup test database:', error);
    throw error;
  }
}

async function startBackendServices(): Promise<void> {
  try {
    // Check if backend services are already running
    const backendUrl = process.env.E2E_BACKEND_URL || 'http://localhost:5000';
    
    try {
      const response = await fetch(`${backendUrl}/health`, { signal: AbortSignal.timeout(5000) });
      if (response.ok) {
        console.log('✅ Backend services already running');
        return;
      }
    } catch {
      // Backend not running, need to start it
    }

    // Start backend in test mode using Docker Compose
    const composeFile = path.resolve(__dirname, '../../docker-compose.test.yml');
    
    try {
      execSync(`docker-compose -f ${composeFile} up -d`, { 
        stdio: 'inherit',
        env: {
          ...process.env,
          NODE_ENV: 'test',
          DATABASE_URL: 'postgresql://testuser:testpass@localhost:5433/smartalarm_test',
        }
      });
      console.log('✅ Backend services started');
    } catch (error) {
      console.warn('⚠️  Could not start backend with Docker Compose, assuming manual setup');
    }

  } catch (error) {
    console.error('Failed to start backend services:', error);
    throw error;
  }
}

async function waitForServices(): Promise<void> {
  const services = [
    { name: 'Frontend', url: process.env.E2E_BASE_URL || 'http://localhost:5173' },
    { name: 'Backend API', url: (process.env.E2E_BACKEND_URL || 'http://localhost:5000') + '/health' },
  ];

  for (const service of services) {
    let attempts = 0;
    const maxAttempts = 30;
    
    console.log(`⏳ Waiting for ${service.name}...`);
    
    while (attempts < maxAttempts) {
      try {
        const response = await fetch(service.url, { 
          signal: AbortSignal.timeout(5000),
          headers: { 'Accept': 'application/json' }
        });
        
        if (response.ok || response.status === 404) { // 404 is ok for frontend root
          console.log(`✅ ${service.name} is ready`);
          break;
        }
      } catch (error) {
        attempts++;
        if (attempts < maxAttempts) {
          console.log(`⏳ ${service.name} not ready, retrying... (${attempts}/${maxAttempts})`);
          await new Promise(resolve => setTimeout(resolve, 2000));
        }
      }
    }

    if (attempts >= maxAttempts) {
      throw new Error(`${service.name} failed to start within timeout`);
    }
  }
}

async function setupTestData(): Promise<void> {
  try {
    // Create test user accounts
    const backendUrl = process.env.E2E_BACKEND_URL || 'http://localhost:5000';
    
    const testUsers = [
      {
        email: 'testuser@example.com',
        password: 'TestPassword123!',
        name: 'Test User',
        role: 'user'
      },
      {
        email: 'admin@example.com', 
        password: 'AdminPassword123!',
        name: 'Admin User',
        role: 'admin'
      }
    ];

    for (const user of testUsers) {
      try {
        const response = await fetch(`${backendUrl}/api/auth/register`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify(user)
        });

        if (response.ok || response.status === 409) { // 409 = user already exists
          console.log(`✅ Test user ${user.email} ready`);
        } else {
          console.warn(`⚠️  Failed to create user ${user.email}: ${response.status}`);
        }
      } catch (error) {
        console.warn(`⚠️  Could not create test user ${user.email}:`, error);
      }
    }

    // Set up test data in localStorage for offline tests
    const browser = await chromium.launch();
    const page = await browser.newPage();
    
    try {
      await page.goto(process.env.E2E_BASE_URL || 'http://localhost:5173');
      
      // Set up test authentication state
      await page.evaluate(() => {
        const testAuthState = {
          state: {
            user: {
              id: 'test-user-id',
              email: 'testuser@example.com',
              name: 'Test User',
              role: 'user'
            },
            token: 'test-jwt-token',
            isAuthenticated: true,
            refreshToken: 'test-refresh-token'
          },
          version: 0
        };
        
        localStorage.setItem('smart-alarm-auth', JSON.stringify(testAuthState));
        
        // Set up test alarms
        const testAlarms = {
          state: {
            alarms: [
              {
                id: 'test-alarm-1',
                name: 'Morning Alarm',
                time: '07:00',
                triggerTime: '07:00',
                isEnabled: true,
                isRecurring: true,
                daysOfWeek: ['monday', 'tuesday', 'wednesday', 'thursday', 'friday'],
                description: 'Daily morning alarm',
                userId: 'test-user-id',
                createdAt: new Date().toISOString(),
                updatedAt: new Date().toISOString()
              },
              {
                id: 'test-alarm-2',
                name: 'Weekend Alarm',
                time: '09:00',
                triggerTime: '09:00',
                isEnabled: false,
                isRecurring: true,
                daysOfWeek: ['saturday', 'sunday'],
                description: 'Weekend morning alarm',
                userId: 'test-user-id',
                createdAt: new Date().toISOString(),
                updatedAt: new Date().toISOString()
              }
            ],
            totalCount: 2,
            currentPage: 1,
            totalPages: 1,
            pageSize: 10,
            filters: {},
            isLoading: false,
            error: null,
            lastSync: new Date().toISOString()
          },
          version: 0
        };
        
        localStorage.setItem('smart-alarm-alarms', JSON.stringify(testAlarms));
        
        // Enable ML data collection for testing
        localStorage.setItem('ml-data-collection-consent', 'true');
        
        console.log('✅ Test data initialized in localStorage');
      });

    } finally {
      await browser.close();
    }

    console.log('✅ Test data setup completed');

  } catch (error) {
    console.error('Failed to setup test data:', error);
    throw error;
  }
}

export default globalSetup;