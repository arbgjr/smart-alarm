// Re-export all types for easier imports
export * from './api';
export * from './components';

// Common utility types
export type Optional<T, K extends keyof T> = Omit<T, K> & Partial<Pick<T, K>>;
export type RequiredFields<T, K extends keyof T> = T & Required<Pick<T, K>>;
export type PartialFields<T, K extends keyof T> = Omit<T, K> & Partial<Pick<T, K>>;

// Generic ID type
export type ID = string;

// Common response patterns
export type AsyncResult<T> = {
  data?: T;
  loading: boolean;
  error?: string | null;
};

// Event callback types
export type VoidCallback = () => void;
export type AsyncVoidCallback = () => Promise<void>;
export type ValueCallback<T> = (value: T) => void;
export type AsyncValueCallback<T> = (value: T) => Promise<void>;

// Route parameter types
export type RouteParams = Record<string, string>;
export type SearchParams = Record<string, string | string[]>;

// Environment types
export interface AppConfig {
  apiUrl: string;
  environment: 'development' | 'staging' | 'production';
  version: string;
  features: {
    enablePwa: boolean;
    enableOfflineMode: boolean;
    enablePushNotifications: boolean;
    enableAnalytics: boolean;
  };
}

// Common filter and sort types
export interface FilterOption {
  key: string;
  label: string;
  value: any;
}

export interface SortOption {
  key: string;
  label: string;
  direction: 'asc' | 'desc';
}

export interface PaginationParams {
  page: number;
  pageSize: number;
}

export interface FilterParams {
  search?: string;
  filters?: Record<string, any>;
  sort?: {
    field: string;
    direction: 'asc' | 'desc';
  };
}

// Date/time utility types
export type DateString = string; // ISO 8601 format
export type TimeString = string; // HH:mm format
export type DateTimeString = string; // ISO 8601 datetime

// Status enums as const assertions for better TypeScript inference
export const LOADING_STATES = {
  IDLE: 'idle',
  LOADING: 'loading',
  SUCCESS: 'success',
  ERROR: 'error',
} as const;

export type LoadingStatus = typeof LOADING_STATES[keyof typeof LOADING_STATES];

export const THEME_MODES = {
  LIGHT: 'light',
  DARK: 'dark',
  AUTO: 'auto',
} as const;

export type ThemeMode = typeof THEME_MODES[keyof typeof THEME_MODES];
