// Respuesta genérica de la API (todas devuelven este envoltorio)
export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}

/* -------------------- AUTH: REQUESTS -------------------- */

// /api/Auth/register  (cuerpo según Swagger)
export interface RegisterRequest {
  Name: string;
  Email: string;
  Password: string;
  PasswordConfirm: string;

  Alias?: string;
  LanguageCode?: string;
  ProfileImageUrl?: string;
  ProfileImageWidth?: number;
  ProfileImageHeight?: number;
  ProfileImageBytes?: number;
}



// /api/Auth/login
export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  Success: boolean;
  Message: string;
  Data: {
    SessionID: string;
    Message: string;
    UserID: number;
    Email: string;
    Name: string;
  };
}

// /api/Auth/request-reset
export interface RequestReset {
  email: string;
}

// /api/Auth/reset-with-token
export interface ResetWithTokenRequest {
  token: string;
  newPassword: string;
  confirmPassword: string;
}



/* -------------------- AUTH: RESPONSES -------------------- */

// register / request-reset / reset-with-token / logout
// también devuelven ApiResponse<string> (mensaje o dato simple)
export type SimpleOkResponse = ApiResponse<string>;


// HECHO POR IA
