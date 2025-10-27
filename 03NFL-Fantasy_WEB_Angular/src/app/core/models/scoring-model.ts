export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}

export interface ScoringSchema {
  ScoringSchemaID: number;
  Name: string;
  Version: number;
  IsTemplate: boolean;
  Description: string;
  CreatedAt: string; // ISO 8601 date string
}
export interface ScoringRule {
  ScoringSchemaID: number;
  Name: string;
  Version: number;
  MetricCode: string;
  FlatPoints?: number;         // Opcional: no todos los objetos lo traen
  PointsPerUnit?: number;      // Opcional: sólo aparece en métricas por unidad
  Unit?: 'YARD' | 'EVENT';     // Inferido del JSON, ajustable si hay más
  UnitValue?: number;
}

export type ScoringSchemaRulesResponse = ApiResponse<ScoringRule[]>;


export type ScoringSchemasResponse = ApiResponse<ScoringSchema[]>;
