// Respuesta genérica de la API (todas devuelven este envoltorio)
export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}

export interface CurrentSeason {
  SeasonID: number;
  Label: string;
  Year: number;
  StartDate: string;   // ISO format: "2025-09-01T00:00:00"
  EndDate: string;     // ISO format: "2026-02-28T00:00:00"
  IsCurrent: boolean;
  CreatedAt: string;   // ISO format
}
export interface PositionFormat {
  PositionFormatID: number;
  Name: string;
  Description: string;
  CreatedAt: string; // ISO format
}

export interface PositionSlot {
  PositionFormatID: number;
  FormatName: string;
  PositionCode: string;  // e.g., "QB", "RB/WR", "IR"
  SlotCount: number;
}

export type PositionSlotsResponse = ApiResponse<PositionSlot[]>;

export type PositionFormatsResponse = ApiResponse<PositionFormat[]>;

export type CurrentSeasonResponse = ApiResponse<CurrentSeason>;
