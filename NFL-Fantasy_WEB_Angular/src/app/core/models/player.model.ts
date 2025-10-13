export interface PlayerBasic {
  playerID: number;
  firstName: string;
  lastName: string;
  fullName: string;
  position: string;
  nflTeamID?: number;
  nflTeamName?: string;
  injuryStatus?: string;
  photoThumbnailUrl?: string;
  isActive: boolean;
}

export interface AvailablePlayer {
  playerID: number;
  fullName: string;
  position: string;
  nflTeamName?: string;
  nflTeamCity?: string;
  injuryStatus?: string;
  photoThumbnailUrl?: string;
}

// Para filtros
export interface PlayerFilters {
  position?: string;
  nflTeamId?: number;
  injuryStatus?: string;
  searchTerm?: string;
}

// Constantes para posiciones NFL
export const NFL_POSITIONS = [
  'QB', 'RB', 'WR', 'TE', 'K', 'DEF'
] as const;

export type NFLPosition = typeof NFL_POSITIONS[number];

// Constantes para tipos de adquisición
export const ACQUISITION_TYPES = [
  'Draft', 'Trade', 'FreeAgent', 'Waiver'
] as const;

export type AcquisitionType = typeof ACQUISITION_TYPES[number];

// Constantes para estados de lesión
export const INJURY_STATUSES = [
  'Healthy', 'Questionable', 'Doubtful', 'Out', 'IR'
] as const;

export type InjuryStatus = typeof INJURY_STATUSES[number];
