/**
 * nfl-player-model.ts
 * -----------------------------------------------------------------------------
 * Modelos para /api/nflplayer (7 endpoints).
 * Se respeta el casing del backend (Swagger) para evitar sorpresas.
 */

export interface CreateNFLPlayerDTO {
  FirstName: string;
  LastName: string;
  Position: string;
  NFLTeamID: number;

  InjuryStatus?: string;
  InjuryDescription?: string;

  PhotoUrl?: string;
  PhotoWidth?: number;
  PhotoHeight?: number;
  PhotoBytes?: number;

  PhotoThumbnailUrl?: string;
  ThumbnailUrl?: string;
  ThumbnailWidth?: number;
  ThumbnailHeight?: number;
  ThumbnailBytes?: number;
}

export interface UpdateNFLPlayerDTO {
  FirstName?: string;
  LastName?: string;
  Position?: string;
  NFLTeamID?: number;

  InjuryStatus?: string;
  InjuryDescription?: string;

  PhotoUrl?: number | string;   // algunos backends aceptan url o bytes/ids
  PhotoWidth?: number;
  PhotoHeight?: number;
  PhotoBytes?: number;

  PhotoThumbnailUrl?: string;
  ThumbnailUrl?: string;
  ThumbnailWidth?: number;
  ThumbnailHeight?: number;
  ThumbnailBytes?: number;

  IsActive?: boolean;
}

export interface ListNFLPlayersRequest {
  PageNumber: number;
  PageSize: number;

  SearchTerm?: string;
  FilterPosition?: string;
  FilterNFLTeamID?: number;
  FilterIsActive?: boolean;
}

export interface NFLPlayerListItem {
  NFLPlayerID: number;
  FirstName: string;
  LastName: string;
  FullName: string;
  Position: string;
  NFLTeamID: number;

  InjuryStatus?: string;
  IsActive: boolean;

  PhotoUrl?: string;
  ThumbnailUrl?: string;

  CreatedAt?: string; // ISO
  UpdatedAt?: string; // ISO
}

export interface ListNFLPlayersResponse {
  Players: NFLPlayerListItem[];
  TotalRecords: number;
  CurrentPage: number;
  PageSize: number;
  TotalPages: number;
}

export interface NFLPlayerDetails {
  NFLPlayerID: number;
  FirstName: string;
  LastName: string;
  FullName: string;
  Position: string;
  NFLTeamID: number;

  InjuryStatus?: string;
  InjuryDescription?: string;

  PhotoUrl?: string;
  PhotoWidth?: number;
  PhotoHeight?: number;
  PhotoBytes?: number;

  PhotoThumbnailUrl?: string;
  ThumbnailUrl?: string;
  ThumbnailWidth?: number;
  ThumbnailHeight?: number;
  ThumbnailBytes?: number;

  IsActive?: boolean;
  CreatedAt?: string;    // ISO
  CreatedByName?: string;
  UpdatedAt?: string;    // ISO
  UpdatedByName?: string;
}

/** Para /api/nflplayer/active */
export interface NFLPlayerBasic {
  NFLPlayerID: number;
  FullName: string;
  Position: string;
  NFLTeamID: number;
  ThumbnailUrl?: string;
}

export interface BatchCreatePlayersResponse {
  Success: boolean;
  Message: string;
  Data: {
    CreatedPlayers: Array<{
      NFLPlayerID: number;
      PlayerName: string;
      Position: string;
      NFLTeamID: number;
      Success: boolean;
    }>;
    Errors: string[];
    TotalProcessed: number;
    SuccessCount: number;
    ErrorCount: number;
  };
}

export interface CreateBatchReportRequest {
  ReportUrl: string;
  TotalProcessed: number;
  SuccessCount: number;
  ErrorCount: number;
  Message: string;
}

export interface CreateBatchReportResponse {
  Success: boolean;
  Message: string;
  Data: {
    BatchReportID: number;
    ReportUrl: string;
    TotalProcessed: number;
    SuccessCount: number;
    ErrorCount: number;
    Message: string;
  };
}

export interface BatchReportListItem {
  BatchReportID: number;
  ReportUrl: string;
  TotalProcessed: number;
  SuccessCount: number;
  ErrorCount: number;
  ActorUserID: number;
  ActorName: string;
  ActorEmail: string;
  SourceIp: string;
  UserAgent: string;
  CreatedAt: string;
}

export interface ListBatchReportsRequest {
  PageNumber: number;
  PageSize?: number;
}

export interface ListBatchReportsResponse {
  Reports: BatchReportListItem[];
  TotalRecords: number;
  CurrentPage: number;
  PageSize: number;
  TotalPages: number;
}

export interface BatchReportDetails {
  BatchReportID: number;
  ReportUrl: string;
  TotalProcessed: number;
  SuccessCount: number;
  ErrorCount: number;
  ActorUserID: number;
  ActorName: string;
  ActorEmail: string;
  ActorRole: string;
  SourceIp: string;
  UserAgent: string;
  CreatedAt: string;
}

// 1. ACTUALIZAR nfl-player-model.ts - AGREGAR AL FINAL

// ===== PLAYER NEWS =====
export interface CreatePlayerNewsDTO {
  NFLPlayerID: number;
  NewsText: string;
  IsInjury: boolean;
  InjurySummary?: string;
  Designation?: 'O' | 'D' | 'Q' | 'P' | 'FP' | 'IR' | 'PUP' | 'SUS';
}

export interface PlayerNewsItem {
  NewsID: number;
  NFLPlayerID: number;
  NewsText: string;
  IsInjury: boolean;
  InjurySummary?: string;
  Designation?: string;
  CreatedByUserID: number;
  CreatedByName: string;
  CreatedAt: string;
}

export interface ListPlayerNewsRequest {
  PageNumber: number;
  PageSize?: number;
}

export interface ListPlayerNewsResponse {
  News: PlayerNewsItem[];
  TotalRecords: number;
  CurrentPage: number;
  PageSize: number;
  TotalPages: number;
}

export interface PlayerNewsDetails {
  NewsID: number;
  NFLPlayerID: number;
  PlayerFirstName: string;
  PlayerLastName: string;
  PlayerFullName: string;
  NewsText: string;
  IsInjury: boolean;
  InjurySummary?: string;
  Designation?: string;
  CreatedByUserID: number;
  CreatedByName: string;
  CreatedAt: string;
  IsDeleted: boolean;
}

export interface CreatePlayerNewsResponse {
  Success: boolean;
  Message: string;
  Data: {
    NewsID: number;
    Message: string;
  };
}

/** Data que devuelve el DELETE /NFLPlayer/news/{newsId} */
export interface DeletePlayerNewsData {
  Message: string;
  // Puede venir con una designación, ser null o no venir
  RevertedDesignation?: 'O' | 'D' | 'Q' | 'P' | 'FP' | 'IR' | 'PUP' | 'SUS' | null;
}

export interface DeletePlayerNewsResponse {
  Success: boolean;
  Message: string;
  Data: DeletePlayerNewsData;
}

export const INJURY_DESIGNATIONS = [
  { value: 'O', label: 'Out (O)', description: 'No jugarán' },
  { value: 'D', label: 'Doubtful (D)', description: 'Muy poco probable (~25%)' },
  { value: 'Q', label: 'Questionable (Q)', description: 'Probabilidad ~50%' },
  { value: 'P', label: 'Probable (P)', description: 'Casi seguro que juega' },
  { value: 'FP', label: 'Full Practice (FP)', description: 'Participación plena' },
  { value: 'IR', label: 'Injured Reserve (IR)', description: 'Fuera por periodo extendido' },
  { value: 'PUP', label: 'PUP', description: 'Físicamente incapaz de jugar' },
  { value: 'SUS', label: 'Suspended (SUS)', description: 'No elegible por sanción' }
] as const;