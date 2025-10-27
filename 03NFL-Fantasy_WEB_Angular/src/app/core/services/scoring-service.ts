// src/app/core/services/scoring.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

import {
  ScoringSchemasResponse,
  ScoringSchemaRulesResponse,
} from '../models/scoring-model';

@Injectable({ providedIn: 'root' })
export class ScoringService {
  // Casing exacto del Swagger: /api/Scoring/...
  private readonly baseUrl = `${environment.apiUrl}/Scoring`;

  constructor(private http: HttpClient) {}

  /** GET /api/Scoring/schemas */
  listSchemas(): Observable<ScoringSchemasResponse> {
    return this.http.get<ScoringSchemasResponse>(`${this.baseUrl}/schemas`);
  }

  /** GET /api/Scoring/schemas/{id}/rules */
  listRules(schemaId: number): Observable<ScoringSchemaRulesResponse> {
    return this.http.get<ScoringSchemaRulesResponse>(
      `${this.baseUrl}/schemas/${schemaId}/rules`
    );
  }
}