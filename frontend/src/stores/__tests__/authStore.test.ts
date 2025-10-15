import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { renderHook } from '@testing-library/react';
import { act } from 'react';
import { useAuthStore, useIsAuthenticated, useCurrentUser, useAuthToken } from '../authStore';
import type { User } from '@/types/auth';

// Mock localStorage
const localStorageMock = {
  getItem: vi.fn(),
  setItem: vi.fn(),
  removeItem: vi.fn(),
  clear: vi.fn(),
};
Object.defineProperty(window, 'localStorage', { value: localStorageMock });

// Mock Date for consistent token expiry testing
const mockDate = new Date('2025-01-25T10:00:00.000Z');
vi.setSystemTime(mockDate);

const mockUser: User = {
  id: '1',
  email: 'test@example.com',
  name: 'Test User',
  role: 'User',
  isActive: true,
  createdAt: '2025-01-25T00:00:00.000Z',
};

const mockToken = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyLCJleHAiOjE3Mzc2NDE0MDB9.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c';

describe('AuthStore', () => {
  beforeEach(() => {
    // Clear store state before each test
    useAuthStore.getState().logout();
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('Initial State', () => {
    it('should have correct initial state', () => {
      const { result } = renderHook(() => useAuthStore());
      
      expect(result.current.user).toBeNull();
      expect(result.current.token).toBeNull();
      expect(result.current.refreshToken).toBeNull();
      expect(result.current.isAuthenticated).toBe(false);
      expect(result.current.isLoading).toBe(false);
      expect(result.current.error).toBeNull();
    });
  });

  describe('Authentication Actions', () => {
    it('should login user successfully', () => {
      const { result } = renderHook(() => useAuthStore());
      
      act(() => {
        result.current.login(mockUser, mockToken, 'refresh-token');
      });

      expect(result.current.user).toEqual(mockUser);
      expect(result.current.token).toBe(mockToken);
      expect(result.current.refreshToken).toBe('refresh-token');
      expect(result.current.isAuthenticated).toBe(true);
      expect(result.current.isLoading).toBe(false);
      expect(result.current.error).toBeNull();
    });

    it('should logout user successfully', () => {
      const { result } = renderHook(() => useAuthStore());
      
      // First login
      act(() => {
        result.current.login(mockUser, mockToken, 'refresh-token');
      });

      // Then logout
      act(() => {
        result.current.logout();
      });

      expect(result.current.user).toBeNull();
      expect(result.current.token).toBeNull();
      expect(result.current.refreshToken).toBeNull();
      expect(result.current.isAuthenticated).toBe(false);
      expect(result.current.isLoading).toBe(false);
      expect(result.current.error).toBeNull();
    });

    it('should set user only', () => {
      const { result } = renderHook(() => useAuthStore());
      
      act(() => {
        result.current.setUser(mockUser);
      });

      expect(result.current.user).toEqual(mockUser);
      expect(result.current.isAuthenticated).toBe(true);
      expect(result.current.token).toBeNull();
    });

    it('should set tokens only', () => {
      const { result } = renderHook(() => useAuthStore());
      
      act(() => {
        result.current.setTokens(mockToken, 'refresh-token');
      });

      expect(result.current.token).toBe(mockToken);
      expect(result.current.refreshToken).toBe('refresh-token');
      expect(result.current.isAuthenticated).toBe(true);
      expect(result.current.user).toBeNull();
    });
  });

  describe('Loading and Error States', () => {
    it('should set loading state', () => {
      const { result } = renderHook(() => useAuthStore());
      
      act(() => {
        result.current.setLoading(true);
      });

      expect(result.current.isLoading).toBe(true);

      act(() => {
        result.current.setLoading(false);
      });

      expect(result.current.isLoading).toBe(false);
    });

    it('should set error state and stop loading', () => {
      const { result } = renderHook(() => useAuthStore());
      
      act(() => {
        result.current.setLoading(true);
        result.current.setError('Login failed');
      });

      expect(result.current.error).toBe('Login failed');
      expect(result.current.isLoading).toBe(false);
    });

    it('should clear error state', () => {
      const { result } = renderHook(() => useAuthStore());
      
      act(() => {
        result.current.setError('Some error');
      });

      expect(result.current.error).toBe('Some error');

      act(() => {
        result.current.clearError();
      });

      expect(result.current.error).toBeNull();
    });
  });

  describe('Role-Based Permissions', () => {
    it('should check user roles correctly', () => {
      const { result } = renderHook(() => useAuthStore());
      
      act(() => {
        result.current.setUser({ ...mockUser, role: 'Admin' });
      });

      expect(result.current.hasRole('admin')).toBe(true);
      expect(result.current.hasRole('user')).toBe(false);
      expect(result.current.hasRole('Admin')).toBe(true);
    });

    it('should check permissions based on roles', () => {
      const { result } = renderHook(() => useAuthStore());
      
      // Test admin permissions
      act(() => {
        result.current.setUser({ ...mockUser, role: 'Admin' });
      });

      expect(result.current.hasPermission('read')).toBe(true);
      expect(result.current.hasPermission('write')).toBe(true);
      expect(result.current.hasPermission('delete')).toBe(true);
      expect(result.current.hasPermission('manage_users')).toBe(true);

      // Test user permissions
      act(() => {
        result.current.setUser({ ...mockUser, role: 'User' });
      });

      expect(result.current.hasPermission('read')).toBe(true);
      expect(result.current.hasPermission('write')).toBe(true);
      expect(result.current.hasPermission('delete')).toBe(false);
      expect(result.current.hasPermission('manage_users')).toBe(false);
    });

    it('should return false for permissions when no user', () => {
      const { result } = renderHook(() => useAuthStore());
      
      expect(result.current.hasRole('admin')).toBe(false);
      expect(result.current.hasPermission('read')).toBe(false);
    });
  });

  describe('Token Management', () => {
    it('should decode JWT token expiry correctly', () => {
      const { result } = renderHook(() => useAuthStore());
      
      act(() => {
        result.current.setTokens(mockToken);
      });

      const expiry = result.current.getTokenExpiry();
      expect(expiry).toBeDefined();
      expect(expiry).toBeInstanceOf(Date);
      // Token expires at timestamp 1737641400 (2025-01-23)
      expect(expiry?.getTime()).toBe(1737641400000);
    });

    it('should check if token is expired', () => {
      const { result } = renderHook(() => useAuthStore());
      
      act(() => {
        result.current.setTokens(mockToken);
      });

      // Token expires in 2025-01-23, we're in 2025-01-25 (expired)
      expect(result.current.isTokenExpired()).toBe(true);
    });

    it('should return null expiry for invalid token', () => {
      const { result } = renderHook(() => useAuthStore());
      
      act(() => {
        result.current.setTokens('invalid.token.here');
      });

      expect(result.current.getTokenExpiry()).toBeNull();
      expect(result.current.isTokenExpired()).toBe(true);
    });

    it('should return true for expired when no token', () => {
      const { result } = renderHook(() => useAuthStore());
      
      expect(result.current.isTokenExpired()).toBe(true);
    });
  });

  describe('Persistence', () => {
    it('should persist auth state to localStorage', async () => {
      const { result } = renderHook(() => useAuthStore());
      
      act(() => {
        result.current.login(mockUser, mockToken, 'refresh-token');
      });

      // Wait for persistence to complete
      await new Promise(resolve => setTimeout(resolve, 100));

      // Check if localStorage was called with correct data
      expect(localStorageMock.setItem).toHaveBeenCalled();
      const setItemCalls = vi.mocked(localStorageMock.setItem).mock.calls;
      const persistCall = setItemCalls.find(call => call[0] === 'smart-alarm-auth');
      
      expect(persistCall).toBeDefined();
      if (persistCall) {
        const persistedData = JSON.parse(persistCall[1]);
        expect(persistedData.state.user).toEqual(mockUser);
        expect(persistedData.state.token).toBe(mockToken);
        expect(persistedData.state.isAuthenticated).toBe(true);
      }
    });
  });

  describe('Selective Hooks', () => {
    it('should provide selective auth hooks', () => {
      
      const { result: authResult } = renderHook(() => useAuthStore());
      const { result: isAuthResult } = renderHook(() => useIsAuthenticated());
      const { result: userResult } = renderHook(() => useCurrentUser());
      const { result: tokenResult } = renderHook(() => useAuthToken());

      act(() => {
        authResult.current.login(mockUser, mockToken);
      });

      expect(isAuthResult.current).toBe(true);
      expect(userResult.current).toEqual(mockUser);
      expect(tokenResult.current).toBe(mockToken);
    });
  });

  describe('Error Scenarios', () => {
    it('should handle malformed JWT tokens gracefully', () => {
      const { result } = renderHook(() => useAuthStore());
      
      act(() => {
        result.current.setTokens('not.a.valid.jwt');
      });

      expect(() => result.current.getTokenExpiry()).not.toThrow();
      expect(result.current.getTokenExpiry()).toBeNull();
      expect(result.current.isTokenExpired()).toBe(true);
    });

    it('should handle missing role gracefully', () => {
      const { result } = renderHook(() => useAuthStore());
      
      const userWithoutRole = { ...mockUser, role: undefined } as any;
      
      act(() => {
        result.current.setUser(userWithoutRole);
      });

      expect(result.current.hasRole('admin')).toBe(false);
      expect(result.current.hasPermission('read')).toBe(false);
    });
  });
});