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
