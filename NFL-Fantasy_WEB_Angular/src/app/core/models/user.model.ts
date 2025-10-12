export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}

export interface CommissionedLeague {
  LeagueID: number;
  LeagueName: string;
  Status: number;
  TeamSlots: number;
  RoleCode: 'COMMISSIONER' | 'CO_COMMISSIONER'; // ajustable según tu sistema
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
  Role: 'ADMIN' | 'MANAGER' | 'CLIENT'; // ajustable según roles existentes
  CommissionedLeagues: CommissionedLeague[];
  Teams: UserTeam[];
}

/* ========= Request: PUT /api/User/profile ========= */
export interface EditUserProfileRequest {
  Name: string;
  Alias: string;
  LanguageCode: string;
  ProfileImageUrl: string;
  ProfileImageWidth: number;
  ProfileImageHeight: number;
  ProfileImageBytes: number;
}

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

export type UserSessionsResponse = ApiResponse<UserSession[]>;
export type UserHeaderResponse = ApiResponse<UserHeader>;

/* ========= Response: PUT /api/User/profile ========= */
export type EditUserProfileResponse = ApiResponse<null>; // No incluye campo Data


export type UserProfileResponse = ApiResponse<UserProfile>;
