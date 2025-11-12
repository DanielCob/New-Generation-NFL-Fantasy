// Envoltura (tu API la usa en POST/PUT)
export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}

/* ========= League: Requests ========= */

// POST /api/League
export interface CreateLeagueRequest {
  Name: string;
  Description: string;
  TeamSlots: number;
  LeaguePassword: string;
  InitialTeamName?: string;
  PlayoffTeams: number;
  AllowDecimals: boolean;
  PositionFormatID: number;
  ScoringSchemaID: number;
}

// PUT /api/League/{id}/config
export interface EditLeagueConfigRequest {
  name: string;
  description: string;
  teamSlots: number;
  positionFormatID: number;
  scoringSchemaID: number;
  playoffTeams: number;
  allowDecimals: boolean;
  tradeDeadlineEnabled: boolean;
  tradeDeadlineDate: string;          // ISO string
  maxRosterChangesPerTeam: number;
  maxFreeAgentAddsPerTeam: number;
}


export interface UpdateLeagueStatusRequest {
  NewStatus: number;
  Reason: string;
}

export interface JoinLeagueRequest {
  LeagueID: number;
  LeaguePassword: string;
  TeamName: string;
}

export interface JoinLeagueData {
  TeamID: number;
  LeagueID: number;
  TeamName: string;
  LeagueName: string;
  AvailableSlots: number;
  Message: string; // viene duplicado con ApiResponse.message, lo dejamos por compatibilidad
}

/* ========= League: Path params ========= */
export interface LeagueIdParam { id: number; }

/* ========= League: Responses (POST/PUT con envoltura) ========= */
export interface CreateLeagueData {
  LeagueID: number;
  LeaguePublicID: number; // NUEVO
  Name: string;
  TeamSlots: number;
  AvailableSlots: number;
  Status: number;
  PlayoffTeams: number;
  AllowDecimals: boolean;
  CreatedAt: string; // ISO format
  Message: string;
}

export interface LeagueTeamSummary {
  TeamID: number;
  TeamName: string;
  OwnerUserID: number;
  OwnerName: string;
  CreatedAt: string; // ISO date
}

export interface LeagueSummary {
  LeagueID: number;
  LeaguePublicID: number; // NUEVO
  Name: string;
  Description: string;
  Status: number;
  TeamSlots: number;
  TeamsCount: number;
  AvailableSlots: number;
  PlayoffTeams: number;
  AllowDecimals: boolean;
  TradeDeadlineEnabled: boolean;
  PositionFormatID: number;
  PositionFormatName: string;
  ScoringSchemaID: number;
  ScoringSchemaName: string;
  ScoringVersion: number;
  SeasonID: number;
  SeasonLabel: string;
  Year: number;
  StartDate: string;  // ISO date
  EndDate: string;    // ISO date
  CreatedByUserID: number;
  CreatedByName: string;
  CreatedAt: string;  // ISO date
  UpdatedAt: string;  // ISO date
  Teams: LeagueTeamSummary[];
}

export interface LeagueDirectoryItem {
  LeagueID: number;
  LeaguePublicID: number; // NUEVO
  SeasonLabel: string;
  Name: string;
  Status: number;
  TeamSlots: number;
  TeamsCount: number;
  AvailableSlots: number;
  CreatedByUserID: number;
  CreatedAt: string; // ISO date
}

export interface LeagueMember {
  LeagueID: number;
  UserID: number;
  RoleCode: 'COMMISSIONER' | 'CO_COMMISSIONER' | 'MANAGER' | 'SPECTATOR'; // según roles observados
  IsPrimaryCommissioner: boolean;
  JoinedAt: string; // ISO date
  UserName: string;
  UserEmail: string;
}

export interface LeagueTeam {
  TeamID: number;
  LeagueID: number;
  TeamName: string;
  OwnerUserID: number;
  OwnerName: string;
  CreatedAt: string; // ISO date
}

export interface LeagueTeamView {
  teamID: number;
  teamName: string;
  ownerName: string;

  // 🆕 NUEVOS CAMPOS Feature 3.1:
  teamImageUrl?: string;
  thumbnailUrl?: string;
  isActive: boolean;
  rosterCount: number;
  updatedAt: Date;

  createdAt: Date;
}

// Actualizar UserTeamView:

export interface UserTeamView {
  teamID: number;
  leagueID: number;
  leagueName: string;
  teamName: string;

  // 🆕 NUEVOS CAMPOS Feature 3.1:
  teamImageUrl?: string;
  thumbnailUrl?: string;
  isActive: boolean;
  rosterCount: number;

  teamCreatedAt: Date;
  leagueStatus: number;
}


export type CreateLeagueResponse = ApiResponse<CreateLeagueData>;

export type EditLeagueConfigResponse    = ApiResponse<string>;
export type UpdateLeagueStatusResponse = ApiResponse<null>;
export type JoinLeagueResponse          = ApiResponse<JoinLeagueData>;


/* ========= League: GET (sin envoltura, por definir cuando tengamos schema) ========= */
export type LeagueSummaryResponse = ApiResponse<LeagueSummary>;  // GET /api/League/{id}/summary
export type LeagueDirectoryResponse = ApiResponse<LeagueDirectoryItem[]>;  // GET /api/League/directory
export type LeagueMembersResponse = ApiResponse<LeagueMember[]>;  // GET /api/League/{id}/members
export type LeagueTeamsResponse = ApiResponse<LeagueTeam[]>;// GET /api/League/{id}/teams

// ========= SEARCH RESPONSE =========
// LeagueSearchResult debe ser compatible con LeagueDirectoryItem
export interface LeagueSearchResult {
  LeagueID: number;
  LeaguePublicID: number;
  Name: string;
  Description: string;
  TeamSlots: number;
  TeamsCount: number;
  AvailableSlots: number;
  PlayoffTeams: number;
  AllowDecimals: boolean;
  SeasonLabel: string;
  SeasonYear: number;
  CreatedByName: string;
  CreatedAt: string;
  TotalRecords: number;
  CurrentPage: number;
  PageSize: number;
  TotalPages: number;
  // Propiedades adicionales para compatibilidad con LeagueDirectoryItem
  Status: number;
  CreatedByUserID: number;
}

export interface PagedLeagueSearchResult {
  data: LeagueSearchResult[];
  totalRecords: number;
  currentPage: number;
  pageSize: number;
  totalPages: number;
}

// ========= LEAGUE CONFIGURATION =========
export interface TeamSlotsOption {
  value: number;
  label: string;
  description: string;
  allowedPlayoffTeams: number[];
}

export interface PlayoffTeamsOption {
  value: number;
  label: string;
  description: string;
}

export type LeagueSearchResponse = ApiResponse<PagedLeagueSearchResult>;