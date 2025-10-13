/**
 * nfl-team.model.ts
 * -----------------------------------------------------------------------------
 * Cambios:
 * - ListNFLTeamsRequest usa SearchTeam (no SearchTerm) para alinear con la API.
 * - Fechas tipadas como string (ISO) porque así llegan en el JSON de ejemplo.
 */

export interface CreateNFLTeamDTO {
  TeamName: string;
  City: string;
  TeamImageUrl?: string;
  TeamImageWidth?: number;
  TeamImageHeight?: number;
  TeamImageBytes?: number;
  ThumbnailUrl?: string;
  ThumbnailWidth?: number;
  ThumbnailHeight?: number;
  ThumbnailBytes?: number;
}

export interface UpdateNFLTeamDTO {
  TeamName?: string;
  City?: string;
  TeamImageUrl?: string;
  TeamImageWidth?: number;
  TeamImageHeight?: number;
  TeamImageBytes?: number;
  ThumbnailUrl?: string;
  ThumbnailWidth?: number;
  ThumbnailHeight?: number;
  ThumbnailBytes?: number;
}

export interface ListNFLTeamsRequest {
  PageNumber: number;
  PageSize: number;
  SearchTeam?: string;      // ✅ este es el nombre del request interno de la app
  FilterCity?: string;
  FilterIsActive?: boolean;
}

/** Respuesta del listado paginado */
export interface ListNFLTeamsResponse {
  Teams: NFLTeamListItem[];
  TotalRecords: number;
  CurrentPage: number;
  PageSize: number;
  TotalPages: number;
}

export interface NFLTeamListItem {
  NFLTeamID: number;
  TeamName: string;
  City: string;
  TeamImageUrl?: string;
  ThumbnailUrl?: string;
  IsActive: boolean;
  CreatedAt: string;          // ISO
  UpdatedAt: string;          // ISO
}

export interface NFLTeamDetails {
  NFLTeamID: number;
  TeamName: string;
  City: string;
  TeamImageUrl?: string;
  TeamImageWidth?: number;
  TeamImageHeight?: number;
  TeamImageBytes?: number;
  ThumbnailUrl?: string;
  ThumbnailWidth?: number;
  ThumbnailHeight?: number;
  ThumbnailBytes?: number;
  IsActive: boolean;
  CreatedAt: string;          // ISO
  CreatedByName?: string;
  UpdatedAt: string;          // ISO
  UpdatedByName?: string;
  ChangeHistory: NFLTeamChange[];
  ActivePlayers: PlayerBasic[];
}

export interface NFLTeamChange {
  ChangeID: number;
  FieldName: string;
  OldValue?: string;
  NewValue?: string;
  ChangedAt: string;          // ISO
  ChangedByName: string;
}

export interface NFLTeamBasic {
  NFLTeamID: number;
  TeamName: string;
  City: string;
  TeamImageUrl?: string;
  ThumbnailUrl?: string;
}

export interface PlayerBasic {
  PlayerID: number;
  FirstName: string;
  LastName: string;
  FullName: string;
  Position: string;
  InjuryStatus: string;
  IsActive: boolean;
}
