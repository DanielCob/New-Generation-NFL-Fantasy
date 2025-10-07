// src/app/core/models/auth.model.ts
export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  success: boolean;
  message: string;
  userID?: number;
  userType?: string;
  sessionToken?: string;
}

export interface ApiResponse {
  success: boolean;
  message: string;
  data?: any;
}