import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { AuthService } from '../services/authService';
import { tokenStorage } from '../lib/api';
import {
  LoginRequest,
  RegisterRequest,
  ForgotPasswordRequest,
  ResetPasswordRequest,
  AuthError
} from '../types/auth';

// Query keys
export const authKeys = {
  all: ['auth'] as const,
  user: () => [...authKeys.all, 'user'] as const,
};

export const useAuth = () => {
  const queryClient = useQueryClient();

  // Check if we have an access token to determine if query should run
  const hasAccessToken = !!tokenStorage.getAccessToken();

  // Get current user
  const {
    data: user,
    isLoading: queryIsLoading,
    error: userError,
  } = useQuery({
    queryKey: authKeys.user(),
    queryFn: () => AuthService.getCurrentUser(),
    retry: false,
    staleTime: 5 * 60 * 1000, // 5 minutes
    // Skip fetching current user if there's no stored access token
    enabled: hasAccessToken,
  });

  // When query is disabled, isLoading should always be false
  const isLoading = hasAccessToken ? queryIsLoading : false;

  // Login mutation
  const loginMutation = useMutation({
    mutationFn: (data: LoginRequest) => AuthService.login(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: authKeys.user() });
    },
    onError: (error) => {
      console.error('Login error:', error);
    },
  });

  // Register mutation
  const registerMutation = useMutation({
    mutationFn: (data: RegisterRequest) => AuthService.register(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: authKeys.user() });
    },
    onError: (error) => {
      console.error('Register error:', error);
    },
  });

  // Logout mutation
  const logoutMutation = useMutation({
    mutationFn: () => AuthService.logout(),
    onSuccess: () => {
      queryClient.clear();
    },
    onError: (error) => {
      console.error('Logout error:', error);
      // Clear data even if logout fails
      queryClient.clear();
    },
  });

  // Forgot password mutation
  const forgotPasswordMutation = useMutation({
    mutationFn: (data: ForgotPasswordRequest) => AuthService.forgotPassword(data),
    onError: (error) => {
      console.error('Forgot password error:', error);
    },
  });

  // Reset password mutation
  const resetPasswordMutation = useMutation({
    mutationFn: (data: ResetPasswordRequest) => AuthService.resetPassword(data),
    onError: (error) => {
      console.error('Reset password error:', error);
    },
  });

  // Simplified FIDO2 authentication
  const authenticateWithFIDO2 = useMutation({
    mutationFn: async (): Promise<void> => {
      if (!navigator.credentials) {
        throw new Error('WebAuthn não é suportado neste navegador');
      }
      // For now, just throw an error since backend integration is needed
      throw new Error('FIDO2 authentication requer integração com backend');
    },
    onError: (error) => {
      console.error('FIDO2 authentication error:', error);
    },
  });

  return {
    // User data
    user,
    isLoading,
    userError,
    isAuthenticated: !!user,

    // Login
    login: loginMutation.mutate,
    isLoggingIn: loginMutation.isPending,
    loginError: loginMutation.error as AuthError | null,
    loginSuccess: loginMutation.isSuccess,

    // Register
    register: registerMutation.mutate,
    isRegistering: registerMutation.isPending,
    registerError: registerMutation.error as AuthError | null,
    registerSuccess: registerMutation.isSuccess,

    // Logout
    logout: logoutMutation.mutate,
    isLoggingOut: logoutMutation.isPending,

    // Forgot password
    forgotPassword: forgotPasswordMutation.mutate,
    isForgettingPassword: forgotPasswordMutation.isPending,
    forgotPasswordError: forgotPasswordMutation.error as AuthError | null,
    forgotPasswordSuccess: forgotPasswordMutation.isSuccess,

    // Reset password
    resetPassword: resetPasswordMutation.mutate,
    isResettingPassword: resetPasswordMutation.isPending,
    resetPasswordError: resetPasswordMutation.error as AuthError | null,
    resetPasswordSuccess: resetPasswordMutation.isSuccess,

    // FIDO2 (simplified)
    authenticateWithFIDO2: authenticateWithFIDO2.mutate,
    isAuthenticatingFIDO2: authenticateWithFIDO2.isPending,
    fido2Error: authenticateWithFIDO2.error as AuthError | null,
  };
};
