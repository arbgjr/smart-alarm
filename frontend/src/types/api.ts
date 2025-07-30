// Core API types matching backend DTOs
export interface User {
  id: string;
  email: string;
  name: string;
  createdAt: string;
  updatedAt: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  refreshToken: string;
  user: User;
  expiresAt: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  name: string;
}

// Alarm types
export interface Alarm {
  id: string;
  userId: string;
  name: string;
  description?: string;
  triggerTime: string; // ISO datetime
  isActive: boolean;
  isRecurring: boolean;
  recurrencePattern?: RecurrencePattern;
  soundFile?: string;
  volume: number;
  snoozeMinutes: number;
  categoryId?: string;
  tags: string[];
  createdAt: string;
  updatedAt: string;
}

export interface CreateAlarmRequest {
  name: string;
  description?: string;
  triggerTime: string;
  isRecurring: boolean;
  recurrencePattern?: RecurrencePattern;
  soundFile?: string;
  volume: number;
  snoozeMinutes: number;
  categoryId?: string;
  tags: string[];
}

export interface UpdateAlarmRequest extends Partial<CreateAlarmRequest> {
  isActive?: boolean;
}

export interface RecurrencePattern {
  type: 'daily' | 'weekly' | 'monthly' | 'yearly' | 'custom';
  interval: number; // Every N days/weeks/months
  daysOfWeek?: number[]; // 0-6, Sunday = 0
  dayOfMonth?: number; // 1-31
  endDate?: string; // ISO date
  maxOccurrences?: number;
}

// Routine types
export interface Routine {
  id: string;
  userId: string;
  name: string;
  description?: string;
  isActive: boolean;
  steps: RoutineStep[];
  schedule: RoutineSchedule;
  estimatedDuration: number; // minutes
  categoryId?: string;
  tags: string[];
  createdAt: string;
  updatedAt: string;
}

export interface RoutineStep {
  id: string;
  name: string;
  description?: string;
  estimatedMinutes: number;
  isOptional: boolean;
  order: number;
}

export interface RoutineSchedule {
  type: 'time-based' | 'event-based';
  triggerTime?: string; // For time-based
  triggerEvent?: string; // For event-based
  daysOfWeek: number[];
  isRecurring: boolean;
}

export interface CreateRoutineRequest {
  name: string;
  description?: string;
  steps: Omit<RoutineStep, 'id'>[];
  schedule: RoutineSchedule;
  estimatedDuration: number;
  categoryId?: string;
  tags: string[];
}

export interface UpdateRoutineRequest extends Partial<CreateRoutineRequest> {
  isActive?: boolean;
}

// Category types
export interface Category {
  id: string;
  userId: string;
  name: string;
  color: string;
  icon?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateCategoryRequest {
  name: string;
  color: string;
  icon?: string;
}

// API Response wrapper
export interface ApiResponse<T> {
  data: T;
  success: boolean;
  message?: string;
  errors?: string[];
}

export interface PaginatedResponse<T> {
  data: T[];
  pagination: {
    page: number;
    pageSize: number;
    totalItems: number;
    totalPages: number;
    hasNext: boolean;
    hasPrevious: boolean;
  };
}

// Error types
export interface ApiError {
  message: string;
  status: number;
  code?: string;
  details?: Record<string, string[]>;
}

// Theme and preferences
export interface UserPreferences {
  theme: 'light' | 'dark' | 'auto';
  language: string;
  timezone: string;
  accessibilityOptions: AccessibilityOptions;
  notificationSettings: NotificationSettings;
}

export interface AccessibilityOptions {
  highContrast: boolean;
  reducedMotion: boolean;
  fontSize: 'small' | 'medium' | 'large' | 'extra-large';
  dyslexiaFriendlyFont: boolean;
  screenReaderOptimized: boolean;
}

export interface NotificationSettings {
  pushNotifications: boolean;
  emailNotifications: boolean;
  soundEnabled: boolean;
  vibrationEnabled: boolean;
  quietHours: {
    enabled: boolean;
    startTime: string; // HH:mm
    endTime: string; // HH:mm
  };
}
