// Authentication types and interfaces
export interface User {
  id: string;
  email: string;
  name: string;
  role: 'Admin' | 'User';
  isActive: boolean;
  createdAt: string;
  lastLoginAt?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
  rememberMe?: boolean;
}

export interface LoginResponse {
  user: User;
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  confirmPassword: string;
  name: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
  confirmPassword: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface AuthError {
  type: 'validation' | 'authentication' | 'authorization' | 'network' | 'server';
  message: string;
  details?: Record<string, string[]>;
}

export interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: AuthError | null;
  accessToken: string | null;
  refreshToken: string | null;
}

// FIDO2 Authentication Types
export interface FIDO2Challenge {
  challenge: ArrayBuffer;
  rp: {
    name: string;
    id?: string;
  };
  user: {
    id: ArrayBuffer;
    name: string;
    displayName: string;
  };
  pubKeyCredParams: PublicKeyCredentialParameters[];
  authenticatorSelection?: AuthenticatorSelectionCriteria;
  timeout?: number;
  attestation?: AttestationConveyancePreference;
  userVerification?: UserVerificationRequirement;
}

export interface FIDO2RegistrationRequest {
  userId: string;
  credentialName: string;
}

export interface FIDO2AuthenticationRequest {
  email: string;
}
