import api, { tokenStorage } from '../lib/api';
import {
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  ForgotPasswordRequest,
  ResetPasswordRequest,
  User,
  FIDO2Challenge,
  FIDO2RegistrationRequest,
  FIDO2AuthenticationRequest
} from '../types/auth';

export class AuthService {
  // Traditional email/password authentication
  static async login(credentials: LoginRequest): Promise<LoginResponse> {
    const response = await api.post<LoginResponse>('/auth/login', credentials);
    const { accessToken, refreshToken } = response.data;

    tokenStorage.setAccessToken(accessToken);
    tokenStorage.setRefreshToken(refreshToken);

    return response.data;
  }

  static async register(userData: RegisterRequest): Promise<LoginResponse> {
    const response = await api.post<LoginResponse>('/auth/register', userData);
    const { accessToken, refreshToken } = response.data;

    tokenStorage.setAccessToken(accessToken);
    tokenStorage.setRefreshToken(refreshToken);

    return response.data;
  }

  static async logout(): Promise<void> {
    try {
      const refreshToken = tokenStorage.getRefreshToken();
      if (refreshToken) {
        await api.post('/auth/logout', { refreshToken });
      }
    } catch (error) {
      console.error('Logout error:', error);
    } finally {
      tokenStorage.clearTokens();
    }
  }

  static async refreshToken(): Promise<string> {
    const refreshToken = tokenStorage.getRefreshToken();
    if (!refreshToken) {
      throw new Error('No refresh token available');
    }

    const response = await api.post<{ accessToken: string }>('/auth/refresh', {
      refreshToken,
    });

    const { accessToken } = response.data;
    tokenStorage.setAccessToken(accessToken);

    return accessToken;
  }

  static async getCurrentUser(): Promise<User> {
    const response = await api.get<User>('/auth/me');
    return response.data;
  }

  static async forgotPassword(request: ForgotPasswordRequest): Promise<void> {
    await api.post('/auth/forgot-password', request);
  }

  static async resetPassword(request: ResetPasswordRequest): Promise<void> {
    await api.post('/auth/reset-password', request);
  }

  // FIDO2 Authentication methods
  static async getFIDO2RegistrationChallenge(request: FIDO2RegistrationRequest): Promise<FIDO2Challenge> {
    const response = await api.post<FIDO2Challenge>('/auth/fido2/register/begin', request);
    return response.data;
  }

  static async completeFIDO2Registration(
    credentialId: string,
    attestationResponse: any
  ): Promise<void> {
    await api.post('/auth/fido2/register/complete', {
      credentialId,
      attestationResponse,
    });
  }

  static async getFIDO2AuthenticationChallenge(
    request: FIDO2AuthenticationRequest
  ): Promise<FIDO2Challenge> {
    const response = await api.post<FIDO2Challenge>('/auth/fido2/authenticate/begin', request);
    return response.data;
  }

  static async completeFIDO2Authentication(
    credentialId: string,
    assertionResponse: any
  ): Promise<LoginResponse> {
    const response = await api.post<LoginResponse>('/auth/fido2/authenticate/complete', {
      credentialId,
      assertionResponse,
    });

    const { accessToken, refreshToken } = response.data;
    tokenStorage.setAccessToken(accessToken);
    tokenStorage.setRefreshToken(refreshToken);

    return response.data;
  }

  // Utility methods
  static isAuthenticated(): boolean {
    return !!tokenStorage.getAccessToken();
  }

  static getStoredToken(): string | null {
    return tokenStorage.getAccessToken();
  }
}
