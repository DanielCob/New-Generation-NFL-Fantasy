import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

import { Season, CreateSeasonRequest, UpdateSeasonRequest, SeasonResponse, CreateSeasonResponse, UpdateSeasonResponse, SeasonWeek } from '../models/season-model';
import { ApiResponse } from '../models/common-model';

@Injectable({ providedIn: 'root' })
export class SeasonService {
  private readonly baseUrl = `${environment.apiUrl}/Seasons`; // /api/Seasons

  constructor(private http: HttpClient) {}

  /** GET /api/Seasons/current -> Season */
  getCurrent(): Observable<Season> {
    return this.http
      .get<ApiResponse<Season> | any>(`${this.baseUrl}/current`)
      .pipe(map(res => (res.data ?? res.Data) as Season));
  }

  /** POST /api/Seasons */
  createSeason(body: CreateSeasonRequest): Observable<CreateSeasonResponse> {
    return this.http.post<CreateSeasonResponse>(`${this.baseUrl}`, body);
  }

  /** PUT /api/Seasons/{id} */
  updateSeason(id: number, body: UpdateSeasonRequest): Observable<UpdateSeasonResponse> {
    return this.http.put<UpdateSeasonResponse>(`${this.baseUrl}/${id}`, body);
  }

  /** POST /api/Seasons/{id}/deactivate */
  deactivateSeason(id: number): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${this.baseUrl}/${id}/deactivate`, { Confirm: true });
  }

  /** DELETE /api/Seasons/{id} with body { Confirm:true } */
  deleteSeason(id: number): Observable<ApiResponse<string>> {
    return this.http.delete<ApiResponse<string>>(`${this.baseUrl}/${id}`, { body: { Confirm: true } as any });
  }

  /** GET /api/Seasons -> list all (assumed). UI will filter historicals */
  listAll(): Observable<ApiResponse<Season[]>> {
    return this.http.get<ApiResponse<Season[]>>(`${this.baseUrl}`);
  }

  /** GET /api/Seasons/{id} */
  getSeason(id: number): Observable<ApiResponse<Season>> {
    return this.http.get<ApiResponse<Season>>(`${this.baseUrl}/${id}`);
  }

  /** GET /api/Seasons/{id}/weeks */
  getWeeks(id: number): Observable<ApiResponse<SeasonWeek[]>> {
    return this.http.get<ApiResponse<SeasonWeek[]>>(`${this.baseUrl}/${id}/weeks`);
  }
}
