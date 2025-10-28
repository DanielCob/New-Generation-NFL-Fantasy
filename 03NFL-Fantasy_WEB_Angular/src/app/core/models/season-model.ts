import { ApiResponse } from './common-model';

// Core Season entity returned by API
export interface Season {
  SeasonID: number;
  Label: string;
  Year: number;
  StartDate: string; // ISO
  EndDate: string;   // ISO
  IsCurrent: boolean;
  CreatedAt: string; // ISO
}

// POST /api/Seasons
export interface CreateSeasonRequest {
  Name: string;
  WeekCount: number;
  StartDate: string; // ISO
  EndDate: string;   // ISO
  MarkAsCurrent?: boolean;
}

// PUT /api/Seasons/{id}
export interface UpdateSeasonRequest {
  Name: string;
  WeekCount: number;
  StartDate: string; // ISO
  EndDate: string;   // ISO
  SetAsCurrent?: boolean;
  ConfirmMakeCurrent?: boolean;
}

// Responses (wrapping can vary per endpoint; we use generic)
export type SeasonResponse = ApiResponse<Season>;
export type CreateSeasonResponse = ApiResponse<Season | string | null>;
export type UpdateSeasonResponse = ApiResponse<Season | string | null>;

// Season Weeks
export interface SeasonWeek {
  WeekNumber: number; // or Index
  StartDate: string;  // ISO
  EndDate: string;    // ISO
}
export type SeasonWeeksResponse = ApiResponse<SeasonWeek[]>;
