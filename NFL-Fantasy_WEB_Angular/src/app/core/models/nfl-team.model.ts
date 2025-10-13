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
  SearchTerm?: string;
  FilterCity?: string;
  FilterIsActive?: boolean;
}

// ===================================
// ViewModels (Response) - Feature 10.1
// ===================================

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
  CreatedAt: Date;
  UpdatedAt: Date;
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
  CreatedAt: Date;
  CreatedByName?: string;
  UpdatedAt: Date;
  UpdatedByName?: string;
  ChangeHistory: NFLTeamChange[];
  ActivePlayers: PlayerBasic[];
}

export interface NFLTeamChange {
  ChangeID: number;
  FieldName: string;
  OldValue?: string;
  NewValue?: string;
  ChangedAt: Date;
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
