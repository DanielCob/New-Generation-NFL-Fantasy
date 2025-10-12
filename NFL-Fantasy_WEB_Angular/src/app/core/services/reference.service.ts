// src/app/core/services/reference.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

import {
  CurrentSeasonResponse,
  PositionFormatsResponse,
  PositionSlotsResponse,
} from '../models/reference.model';

@Injectable({ providedIn: 'root' })
export class ReferenceService {
  // Casing exacto del Swagger: /api/Reference/...
  private readonly baseUrl = `${environment.apiUrl}/Reference`;

  constructor(private http: HttpClient) {}

  /** GET /api/Reference/current-season */
  getCurrentSeason(): Observable<CurrentSeasonResponse> {
    return this.http.get<CurrentSeasonResponse>(`${this.baseUrl}/current-season`);
  }

  /** GET /api/Reference/position-formats */
  listPositionFormats(): Observable<PositionFormatsResponse> {
    return this.http.get<PositionFormatsResponse>(`${this.baseUrl}/position-formats`);
  }

  /** GET /api/Reference/position-formats/{id}/slots */
  listFormatSlots(formatId: number): Observable<PositionSlotsResponse> {
    return this.http.get<PositionSlotsResponse>(`${this.baseUrl}/position-formats/${formatId}/slots`);
  }
}
