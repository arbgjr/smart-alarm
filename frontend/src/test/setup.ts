import '@testing-library/jest-dom';
import { beforeAll, vi } from 'vitest';

// Mock IntersectionObserver
global.IntersectionObserver = vi.fn(() => ({
  disconnect: vi.fn(),
  observe: vi.fn(),
  unobserve: vi.fn(),
}));

// Mock ResizeObserver
global.ResizeObserver = vi.fn(() => ({
  disconnect: vi.fn(),
  observe: vi.fn(),
  unobserve: vi.fn(),
}));

// Mock matchMedia
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: vi.fn().mockImplementation(query => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: vi.fn(), // deprecated
    removeListener: vi.fn(), // deprecated
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  })),
});

// Mock localStorage
Object.defineProperty(window, 'localStorage', {
  value: {
    getItem: vi.fn(),
    setItem: vi.fn(),
    removeItem: vi.fn(),
    clear: vi.fn(),
  },
  writable: true,
});

// Mock sessionStorage
Object.defineProperty(window, 'sessionStorage', {
  value: {
    getItem: vi.fn(),
    setItem: vi.fn(),
    removeItem: vi.fn(),
    clear: vi.fn(),
  },
  writable: true,
});

// Mock navigator
Object.defineProperty(navigator, 'onLine', {
  writable: true,
  value: true,
});

// Mock ServiceWorker
Object.defineProperty(navigator, 'serviceWorker', {
  value: {
    register: vi.fn(() => Promise.resolve()),
    ready: Promise.resolve({
      sync: { register: vi.fn() }
    }),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
  },
  writable: true,
});

// Mock window.crypto for token generation
Object.defineProperty(window, 'crypto', {
  value: {
    getRandomValues: vi.fn().mockReturnValue(new Uint32Array(10)),
  },
});

// Mock fetch globally
global.fetch = vi.fn();

beforeAll(() => {
  // Reset all mocks before each test suite
  vi.clearAllMocks();
});