import { useEffect } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useAuthStore, useAuthActions } from '@/stores/authStore';
import { backgroundSync } from '@/utils/backgroundSync';

// Integration hook that combines Zustand auth with React Query
export function useAuthIntegration() {
  const queryClient = useQueryClient();
  const {
    user,
    token,
    isAuthenticated,
    isLoading,
    error,
    isTokenExpired,
  } = useAuthStore();
  
  const {
    setLoading,
    setError,
    login,
    logout,
  } = useAuthActions();

  // Auto-logout on token expiry
  useEffect(() => {
    const checkTokenExpiry = () => {
      if (isAuthenticated && isTokenExpired()) {
        logout();
        queryClient.clear(); // Clear all cached data
      }
    };

    // Check on mount and set up interval
    checkTokenExpiry();
    const interval = setInterval(checkTokenExpiry, 60000); // Check every minute

    return () => clearInterval(interval);
  }, [isAuthenticated, isTokenExpired, logout, queryClient]);

  return {
    user,
    token,
    isAuthenticated,
    isLoading,
    error,
    isTokenExpired,
    setLoading,
    setError,
    login,
    logout,
  };
}

// Enhanced login mutation that syncs with both stores
export function useLoginMutation() {
  const queryClient = useQueryClient();
  const { login, setLoading, setError } = useAuthActions();

  return useMutation({
    mutationFn: async ({ email, password }: { email: string; password: string }) => {
      setLoading(true);
      setError(null);
      
      // Import auth service dynamically to avoid circular dependencies
      const { AuthService } = await import('@/services/authService');
      const response = await AuthService.login({ email, password });
      
      return response;
    },
    onSuccess: (response) => {
      // Update auth store
      login(response.user, response.accessToken, response.refreshToken);
      
      // Clear and refetch all queries for the new user
      queryClient.clear();
      queryClient.refetchQueries();
      
      setLoading(false);
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || 'Login failed';
      setError(message);
      setLoading(false);
    },
  });
}

// Enhanced logout mutation
export function useLogoutMutation() {
  const queryClient = useQueryClient();
  const { logout, setLoading } = useAuthActions();

  return useMutation({
    mutationFn: async () => {
      setLoading(true);
      
      try {
        // Import auth service dynamically
        const { AuthService } = await import('@/services/authService');
        await AuthService.logout();
      } catch (error) {
        // Continue with local logout even if server call fails
        console.warn('Server logout failed, proceeding with local logout:', error);
      }
    },
    onSettled: () => {
      // Always perform local cleanup
      logout();
      
      // Clear all cached data
      queryClient.clear();
      
      // Clear background sync queue
      backgroundSync.clearQueue();
      
      setLoading(false);
    },
  });
}

// Token refresh mutation
export function useRefreshTokenMutation() {
  const queryClient = useQueryClient();
  const { setTokens, logout, setLoading } = useAuthActions();

  return useMutation({
    mutationFn: async () => {
      setLoading(true);
      
      const { AuthService } = await import('@/services/authService');
      const accessToken = await AuthService.refreshToken();
      
      return accessToken;
    },
    onSuccess: (accessToken) => {
      // Update tokens in store
      setTokens(accessToken);
      
      // Refetch queries with new token
      queryClient.refetchQueries();
      
      setLoading(false);
    },
    onError: () => {
      // If refresh fails, logout user
      logout();
      queryClient.clear();
      setLoading(false);
    },
  });
}

// Registration mutation
export function useRegisterMutation() {
  const queryClient = useQueryClient();
  const { login, setLoading, setError } = useAuthActions();

  return useMutation({
    mutationFn: async (userData: {
      email: string;
      password: string;
      confirmPassword: string;
      name: string;
    }) => {
      setLoading(true);
      setError(null);
      
      const { AuthService } = await import('@/services/authService');
      const response = await AuthService.register(userData);
      
      return response;
    },
    onSuccess: (response) => {
      // Auto-login after successful registration
      login(response.user, response.accessToken, response.refreshToken);
      
      // Clear and refetch queries for new user
      queryClient.clear();
      queryClient.refetchQueries();
      
      setLoading(false);
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || 'Registration failed';
      setError(message);
      setLoading(false);
    },
  });
}

// Hook to automatically refresh token before expiry
export function useTokenRefresh() {
  const { token, refreshToken } = useAuthStore();
  const refreshMutation = useRefreshTokenMutation();

  useEffect(() => {
    if (!token || !refreshToken) return;

    const checkAndRefreshToken = () => {
      // Refresh token 5 minutes before it expires
      const expiry = useAuthStore.getState().getTokenExpiry();
      if (expiry) {
        const timeToExpiry = expiry.getTime() - Date.now();
        const fiveMinutes = 5 * 60 * 1000;
        
        if (timeToExpiry < fiveMinutes && timeToExpiry > 0) {
          refreshMutation.mutate();
        }
      }
    };

    // Check immediately and set up interval
    checkAndRefreshToken();
    const interval = setInterval(checkAndRefreshToken, 60000); // Check every minute

    return () => clearInterval(interval);
  }, [token, refreshToken, refreshMutation]);

  return {
    isRefreshing: refreshMutation.isLoading,
    refreshError: refreshMutation.error,
  };
}

// Hook for handling authentication errors globally
export function useAuthErrorHandler() {
  const queryClient = useQueryClient();
  const { logout } = useAuthActions();

  const handleAuthError = (error: any) => {
    // Handle 401 Unauthorized errors
    if (error?.response?.status === 401) {
      logout();
      queryClient.clear();
      
      // Redirect to login page would be handled by the router/app component
      return true; // Indicates error was handled
    }
    
    return false; // Indicates error was not handled
  };

  return { handleAuthError };
}