import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';
import type { User } from '@/types/auth';

interface AuthState {
  // State
  user: User | null;
  token: string | null;
  refreshToken: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;

  // Actions
  setUser: (user: User) => void;
  setTokens: (token: string, refreshToken?: string) => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
  login: (user: User, token: string, refreshToken?: string) => void;
  logout: () => void;
  clearError: () => void;
  
  // Utils
  hasRole: (role: string) => boolean;
  hasPermission: (permission: string) => boolean;
  getTokenExpiry: () => Date | null;
  isTokenExpired: () => boolean;
}

// Helper function to decode JWT token
const decodeJwtToken = (token: string): any => {
  try {
    const base64Url = token.split('.')[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );
    return JSON.parse(jsonPayload);
  } catch (error) {
    console.error('Error decoding JWT token:', error);
    return null;
  }
};

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      // Initial state
      user: null,
      token: null,
      refreshToken: null,
      isAuthenticated: false,
      isLoading: false,
      error: null,

      // Actions
      setUser: (user) =>
        set((state) => ({
          ...state,
          user,
          isAuthenticated: !!user,
        })),

      setTokens: (token, refreshToken) =>
        set((state) => ({
          ...state,
          token,
          refreshToken: refreshToken || state.refreshToken,
          isAuthenticated: !!token,
        })),

      setLoading: (loading) =>
        set((state) => ({
          ...state,
          isLoading: loading,
        })),

      setError: (error) =>
        set((state) => ({
          ...state,
          error,
          isLoading: false,
        })),

      login: (user, token, refreshToken) =>
        set(() => ({
          user,
          token,
          refreshToken: refreshToken || null,
          isAuthenticated: true,
          isLoading: false,
          error: null,
        })),

      logout: () =>
        set(() => ({
          user: null,
          token: null,
          refreshToken: null,
          isAuthenticated: false,
          isLoading: false,
          error: null,
        })),

      clearError: () =>
        set((state) => ({
          ...state,
          error: null,
        })),

      // Utility functions
      hasRole: (role: string) => {
        const { user } = get();
        return user?.role?.toLowerCase() === role.toLowerCase() || false;
      },

      hasPermission: (permission: string) => {
        const { user } = get();
        if (!user?.role) return false;
        
        // Define role permissions mapping
        const rolePermissions: Record<string, string[]> = {
          admin: ['read', 'write', 'delete', 'manage_users', 'manage_system'],
          user: ['read', 'write'],
        };

        const userRole = user.role.toLowerCase();
        return rolePermissions[userRole]?.includes(permission) || false;
      },

      getTokenExpiry: () => {
        const { token } = get();
        if (!token) return null;

        const decoded = decodeJwtToken(token);
        if (!decoded?.exp) return null;

        return new Date(decoded.exp * 1000);
      },

      isTokenExpired: () => {
        const expiry = get().getTokenExpiry();
        if (!expiry) return true;
        
        return expiry <= new Date();
      },
    }),
    {
      name: 'smart-alarm-auth',
      storage: createJSONStorage(() => localStorage),
      partialize: (state) => ({
        user: state.user,
        token: state.token,
        refreshToken: state.refreshToken,
        isAuthenticated: state.isAuthenticated,
      }),
      // Custom deserializer to handle token expiry on load
      onRehydrateStorage: () => (state) => {
        if (state?.isTokenExpired()) {
          state.logout();
        }
      },
    }
  )
);

// Hook for getting auth actions only (prevents unnecessary re-renders)
export const useAuthActions = () =>
  useAuthStore((state) => ({
    setUser: state.setUser,
    setTokens: state.setTokens,
    setLoading: state.setLoading,
    setError: state.setError,
    login: state.login,
    logout: state.logout,
    clearError: state.clearError,
  }));

// Hook for getting auth utilities only
export const useAuthUtils = () =>
  useAuthStore((state) => ({
    hasRole: state.hasRole,
    hasPermission: state.hasPermission,
    getTokenExpiry: state.getTokenExpiry,
    isTokenExpired: state.isTokenExpired,
  }));

// Selectors for specific auth state
export const useIsAuthenticated = () => useAuthStore((state) => state.isAuthenticated);
export const useCurrentUser = () => useAuthStore((state) => state.user);
export const useAuthToken = () => useAuthStore((state) => state.token);
export const useAuthLoading = () => useAuthStore((state) => state.isLoading);
export const useAuthError = () => useAuthStore((state) => state.error);