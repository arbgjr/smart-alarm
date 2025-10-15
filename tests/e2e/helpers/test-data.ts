import { faker } from '@faker-js/faker';

export interface TestAlarm {
  name: string;
  time: string;
  enabled: boolean;
  daysOfWeek: string[];
  description?: string;
  sound?: string;
  volume?: number;
  snoozeMinutes?: number;
}

export interface TestUser {
  email: string;
  password: string;
  name: string;
  preferences?: {
    theme: 'light' | 'dark' | 'system';
    language: string;
    timezone: string;
    notifications: boolean;
  };
}

export interface TestSchedule {
  alarmId: string;
  startDate: string;
  endDate?: string;
  isActive: boolean;
  repeatType: 'daily' | 'weekly' | 'monthly' | 'custom';
}

// Alarm data generators
export function generateTestAlarm(overrides: Partial<TestAlarm> = {}): TestAlarm {
  const weekdays = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'];
  const weekends = ['Saturday', 'Sunday'];
  const allDays = [...weekdays, ...weekends];
  
  const baseAlarm: TestAlarm = {
    name: faker.lorem.words(2),
    time: generateRandomTime(),
    enabled: faker.datatype.boolean(0.8), // 80% chance of being enabled
    daysOfWeek: faker.helpers.arrayElements(allDays, { min: 1, max: 7 }),
    description: faker.lorem.sentence(),
    sound: faker.helpers.arrayElement(['default', 'gentle', 'nature', 'classic']),
    volume: faker.number.int({ min: 10, max: 100 }),
    snoozeMinutes: faker.helpers.arrayElement([5, 10, 15, 30])
  };

  return { ...baseAlarm, ...overrides };
}

export function generateWorkdayAlarm(overrides: Partial<TestAlarm> = {}): TestAlarm {
  return generateTestAlarm({
    name: `Work ${faker.lorem.word()}`,
    time: generateTimeInRange('06:00', '09:00'),
    daysOfWeek: ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'],
    enabled: true,
    ...overrides
  });
}

export function generateWeekendAlarm(overrides: Partial<TestAlarm> = {}): TestAlarm {
  return generateTestAlarm({
    name: `Weekend ${faker.lorem.word()}`,
    time: generateTimeInRange('08:00', '11:00'),
    daysOfWeek: ['Saturday', 'Sunday'],
    enabled: true,
    ...overrides
  });
}

export function generateMedicationAlarm(overrides: Partial<TestAlarm> = {}): TestAlarm {
  const times = ['08:00', '14:00', '20:00']; // Common medication times
  
  return generateTestAlarm({
    name: `Medication - ${faker.lorem.word()}`,
    time: faker.helpers.arrayElement(times),
    daysOfWeek: ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'],
    enabled: true,
    sound: 'gentle',
    description: 'Take medication reminder',
    ...overrides
  });
}

// User data generators
export function generateTestUser(overrides: Partial<TestUser> = {}): TestUser {
  const baseUser: TestUser = {
    email: faker.internet.email(),
    password: 'TestPassword123!',
    name: faker.person.fullName(),
    preferences: {
      theme: faker.helpers.arrayElement(['light', 'dark', 'system']),
      language: faker.helpers.arrayElement(['en', 'pt', 'es', 'fr']),
      timezone: faker.location.timeZone(),
      notifications: faker.datatype.boolean(0.9)
    }
  };

  return { ...baseUser, ...overrides };
}

export function generateAdminUser(): TestUser {
  return generateTestUser({
    email: 'admin@smartalarm.test',
    password: 'AdminPassword123!',
    name: 'Admin User'
  });
}

export function generateGuestUser(): TestUser {
  return generateTestUser({
    email: 'guest@smartalarm.test',
    password: 'GuestPassword123!',
    name: 'Guest User'
  });
}

// Schedule data generators
export function generateTestSchedule(overrides: Partial<TestSchedule> = {}): TestSchedule {
  const startDate = faker.date.future();
  const endDate = faker.date.future({ refDate: startDate });
  
  const baseSchedule: TestSchedule = {
    alarmId: faker.string.uuid(),
    startDate: startDate.toISOString().split('T')[0],
    endDate: endDate.toISOString().split('T')[0],
    isActive: faker.datatype.boolean(0.8),
    repeatType: faker.helpers.arrayElement(['daily', 'weekly', 'monthly', 'custom'])
  };

  return { ...baseSchedule, ...overrides };
}

// Time utilities
export function generateRandomTime(): string {
  const hour = faker.number.int({ min: 0, max: 23 });
  const minute = faker.number.int({ min: 0, max: 59 });
  return `${hour.toString().padStart(2, '0')}:${minute.toString().padStart(2, '0')}`;
}

export function generateTimeInRange(startTime: string, endTime: string): string {
  const [startHour, startMinute] = startTime.split(':').map(Number);
  const [endHour, endMinute] = endTime.split(':').map(Number);
  
  const startTotalMinutes = startHour * 60 + startMinute;
  const endTotalMinutes = endHour * 60 + endMinute;
  
  const randomTotalMinutes = faker.number.int({
    min: startTotalMinutes,
    max: endTotalMinutes
  });
  
  const hour = Math.floor(randomTotalMinutes / 60);
  const minute = randomTotalMinutes % 60;
  
  return `${hour.toString().padStart(2, '0')}:${minute.toString().padStart(2, '0')}`;
}

export function addMinutesToTime(time: string, minutes: number): string {
  const [hour, minute] = time.split(':').map(Number);
  const totalMinutes = hour * 60 + minute + minutes;
  
  const newHour = Math.floor(totalMinutes / 60) % 24;
  const newMinute = totalMinutes % 60;
  
  return `${newHour.toString().padStart(2, '0')}:${newMinute.toString().padStart(2, '0')}`;
}

// Test data collections
export function generateAlarmCollection(count: number = 5): TestAlarm[] {
  return Array.from({ length: count }, () => generateTestAlarm());
}

export function generateMixedAlarmCollection(): TestAlarm[] {
  return [
    generateWorkdayAlarm({ name: 'Morning Workout' }),
    generateWorkdayAlarm({ name: 'Work Start', time: '08:30' }),
    generateWeekendAlarm({ name: 'Weekend Sleep-in' }),
    generateMedicationAlarm({ name: 'Vitamin D' }),
    generateTestAlarm({ 
      name: 'Lunch Break', 
      time: '12:00', 
      daysOfWeek: ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'] 
    }),
    generateTestAlarm({ 
      name: 'Evening Walk', 
      time: '18:00', 
      enabled: false 
    })
  ];
}

// Validation helpers
export function validateAlarmData(alarm: TestAlarm): boolean {
  if (!alarm.name || alarm.name.trim().length === 0) return false;
  if (!alarm.time || !/^\d{2}:\d{2}$/.test(alarm.time)) return false;
  if (!alarm.daysOfWeek || alarm.daysOfWeek.length === 0) return false;
  if (alarm.volume !== undefined && (alarm.volume < 0 || alarm.volume > 100)) return false;
  if (alarm.snoozeMinutes !== undefined && alarm.snoozeMinutes < 1) return false;
  
  return true;
}

export function validateUserData(user: TestUser): boolean {
  if (!user.email || !/\S+@\S+\.\S+/.test(user.email)) return false;
  if (!user.password || user.password.length < 8) return false;
  if (!user.name || user.name.trim().length === 0) return false;
  
  return true;
}

// OAuth test data
export interface OAuthTestData {
  provider: 'google' | 'github' | 'facebook' | 'microsoft';
  clientId: string;
  redirectUri: string;
  scopes: string[];
  mockAccessToken: string;
  mockUserInfo: {
    id: string;
    name: string;
    email: string;
    avatar?: string;
  };
}

export function generateOAuthTestData(
  provider: 'google' | 'github' | 'facebook' | 'microsoft'
): OAuthTestData {
  const baseUrl = 'http://localhost:3001';
  
  const providerConfigs = {
    google: {
      clientId: 'test-google-client-id',
      scopes: ['openid', 'profile', 'email'],
      redirectUri: `${baseUrl}/auth/callback/google`
    },
    github: {
      clientId: 'test-github-client-id',
      scopes: ['user:email', 'read:user'],
      redirectUri: `${baseUrl}/auth/callback/github`
    },
    facebook: {
      clientId: 'test-facebook-client-id',
      scopes: ['email', 'public_profile'],
      redirectUri: `${baseUrl}/auth/callback/facebook`
    },
    microsoft: {
      clientId: 'test-microsoft-client-id',
      scopes: ['openid', 'profile', 'email', 'User.Read'],
      redirectUri: `${baseUrl}/auth/callback/microsoft`
    }
  };

  return {
    provider,
    ...providerConfigs[provider],
    mockAccessToken: `mock-${provider}-access-token-${faker.string.alphanumeric(32)}`,
    mockUserInfo: {
      id: faker.string.uuid(),
      name: faker.person.fullName(),
      email: faker.internet.email(),
      avatar: faker.image.avatar()
    }
  };
}

// Calendar integration test data
export interface CalendarEventTestData {
  id: string;
  title: string;
  startTime: string;
  endTime: string;
  location?: string;
  description?: string;
  attendees?: string[];
  isRecurring: boolean;
}

export function generateCalendarEvent(overrides: Partial<CalendarEventTestData> = {}): CalendarEventTestData {
  const startDate = faker.date.future();
  const endDate = new Date(startDate.getTime() + (60 * 60 * 1000)); // 1 hour later

  return {
    id: faker.string.uuid(),
    title: faker.lorem.words(3),
    startTime: startDate.toISOString(),
    endTime: endDate.toISOString(),
    location: faker.location.streetAddress(),
    description: faker.lorem.sentence(),
    attendees: Array.from({ length: faker.number.int({ min: 0, max: 5 }) }, () => 
      faker.internet.email()
    ),
    isRecurring: faker.datatype.boolean(0.3),
    ...overrides
  };
}

// Performance test data
export function generateLargeAlarmDataset(size: number): TestAlarm[] {
  return Array.from({ length: size }, (_, index) => 
    generateTestAlarm({ 
      name: `Performance Test Alarm ${index + 1}`,
      enabled: index % 2 === 0 // Alternate enabled/disabled for testing filters
    })
  );
}