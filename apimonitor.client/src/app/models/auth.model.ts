export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  message?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
  rememberMe: boolean;
}

export interface RegisterRequest {
  email: string;
  password: string;
  confirmPassword: string;
  rememberMe: boolean;
}
