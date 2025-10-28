/**
 * user.model.ts
 * -----------------------------------------------------------------------------
 * Ajuste: se agrega `ProfileImageUrl?` (opcional) al UserProfile para poder
 * mostrar el avatar en /profile/header cuando el backend lo envíe (ya sea
 * desde /User/header o /User/profile).
 */

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}

/* ========= ENTIDADES RELACIONADAS AL PERFIL COMPLETO ========= */

export interface CommissionedLeague {
  LeagueID: number;
  LeagueName: string;
  Status: number;
  TeamSlots: number;
  RoleCode: 'COMMISSIONER' | 'CO_COMMISSIONER';
  IsPrimaryCommissioner: boolean;
  JoinedAt: string; // ISO
}

export interface UserTeam {
  TeamID: number;
  LeagueID: number;
  LeagueName: string;
  TeamName: string;
  CreatedAt: string; // ISO
}

export interface UserProfile {
  UserID: number;
  Email: string;
  Name: string;
  Alias: string;
  LanguageCode: string; // e.g. "en"
  AccountStatus: number;
  CreatedAt: string; // ISO
  UpdatedAt: string; // ISO
  /** Some APIs return SystemRoleCode instead of Role */
  Role?: 'ADMIN' | 'MANAGER' | 'CLIENT';
  SystemRoleCode?: 'ADMIN' | 'MANAGER' | 'CLIENT' | string;
  /** URL de imagen de perfil si existe (puede venir desde sp_GetUserProfile) */
  ProfileImageUrl?: string | null;

  CommissionedLeagues: CommissionedLeague[];
  Teams: UserTeam[];
}

/* ========= REQUEST/RESPONSE: PUT /api/User/profile =========
   Campos de imagen opcionales (se envían solo si se provee URL).
================================================================ */

export interface EditUserProfileRequest {
  Name: string;
  Alias: string;
  LanguageCode: string;

  ProfileImageUrl?: string;
  ProfileImageWidth?: number;
  ProfileImageHeight?: number;
  ProfileImageBytes?: number;
}

/* ========= OTROS TIPOS RELACIONADOS ========= */

export interface UserHeader {
  UserID: number;
  Email: string;
  Name: string;
  Alias: string;
  LanguageCode: string;
  AccountStatus: number;
  CreatedAt: string; // ISO
  UpdatedAt: string; // ISO
}

export interface UserSession {
  UserID: number;
  SessionID: string;
  CreatedAt: string;       // ISO
  LastActivityAt: string;  // ISO
  ExpiresAt: string;       // ISO
  IsValid: boolean;
}

// src/app/core/models/user.model.ts
// src/app/core/models/user.model.ts
export interface UserTeam {
  TeamID: number;
  TeamName: string;
  LeagueID: number;
  LeagueName: string;
  CreatedAt: string;       // ISO
}



export type UserSessionsResponse = ApiResponse<UserSession[]>;
export type UserHeaderResponse = ApiResponse<UserHeader>;
export type EditUserProfileResponse = ApiResponse<null>;
export type UserProfileResponse = ApiResponse<UserProfile>;
