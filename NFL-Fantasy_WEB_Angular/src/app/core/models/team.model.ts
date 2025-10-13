// src/app/core/models/team.model.ts

// ========= DTOs / Requests =========
export interface UpdateTeamBrandingDTO {
  teamName?: string;
  teamImageUrl?: string;
  teamImageWidth?: number;
  teamImageHeight?: number;
  teamImageBytes?: number;
  thumbnailUrl?: string;
  thumbnailWidth?: number;
  thumbnailHeight?: number;
  thumbnailBytes?: number;
}

export type AcquisitionType = 'Draft' | 'Waivers' | 'FreeAgent' | 'Trade';

export interface AddPlayerToRosterDTO {
  playerID: number;
  position: string;              // p.ej. 'QB' | 'RB' | 'WR' | 'TE' | 'K' | 'DEF' | 'FLEX'
  acquisitionType?: AcquisitionType;
}

// ========= ViewModels / Responses =========
export interface MyTeamResponse {
  teamId: number;
  teamName: string;
  leagueId: number;
  leagueName: string;
  teamImageUrl?: string;
  thumbnailUrl?: string;

  // lista principal usada por la UI
  roster: RosterItem[];

  // para panel de distribución (opcional en backend; en UI usa [] por defecto)
  distribution?: RosterDistribution[];
}

export interface RosterItem {
  rosterID: number;
  playerID: number;
  fullName: string;              // "Josh Allen"
  firstName?: string;
  lastName?: string;
  position: string;              // "QB", "RB", etc.
  nflTeamName?: string;          // "BUF", "Bills", etc.
  photoUrl?: string;
  photoThumbnailUrl?: string;
  acquisitionType?: AcquisitionType;
  acquiredAt?: string;           // ISO
  isStarter?: boolean;
  isIR?: boolean;
}

export interface RosterDistribution {
  position: string;              // "QB", "RB", "WR", ...
  count: number;                 // cantidad en roster
  percent: number;               // 0..100
}

// (opcional) Detalle ampliado si lo usas en otras pantallas
export interface FantasyTeamDetails {
  teamId: number;
  teamName: string;
  leagueId: number;
  leagueName: string;
  ownerUserID: number;
  isOwner?: boolean;
  teamImageUrl?: string;
  thumbnailUrl?: string;
  createdAt?: string;            // ISO
  updatedAt?: string;            // ISO
  roster: RosterItem[];
  distribution?: RosterDistribution[];
}
// src/app/core/models/team.model.ts
export interface OwnedTeamOption {
  teamId: number;
  teamName: string;
  leagueName?: string;
  thumbnailUrl?: string;
}

