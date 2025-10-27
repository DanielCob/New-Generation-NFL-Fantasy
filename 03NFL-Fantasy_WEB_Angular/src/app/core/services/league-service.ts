// src/app/core/services/league.service.ts
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

// ====== MODELOS (tuyos) ======
import {
  // requests
  CreateLeagueRequest,
  EditLeagueConfigRequest,
  UpdateLeagueStatusRequest,
  JoinLeagueRequest,

  // responses
  CreateLeagueResponse,
  EditLeagueConfigResponse,
  UpdateLeagueStatusResponse,
  LeagueSummaryResponse,
  LeagueDirectoryResponse,
  LeagueMembersResponse,
  LeagueTeamsResponse,
  JoinLeagueResponse
} from '../models/league-model';

@Injectable({ providedIn: 'root' })
export class LeagueService {
  // /api/League (casing exacto de Swagger)
  private readonly baseUrl = `${environment.apiUrl}/League`;

  constructor(private http: HttpClient) {}

  /** POST /api/League */
  create(body: CreateLeagueRequest): Observable<CreateLeagueResponse> {
    return this.http.post<CreateLeagueResponse>(`${this.baseUrl}`, body);
  }

  /** PUT /api/League/{id}/config */
  editConfig(id: number, body: EditLeagueConfigRequest): Observable<EditLeagueConfigResponse> {
    return this.http.put<EditLeagueConfigResponse>(`${this.baseUrl}/${id}/config`, body);
  }

  /** PUT /api/League/{id}/status */
  setStatus(id: number, body: UpdateLeagueStatusRequest): Observable<UpdateLeagueStatusResponse> {
    return this.http.put<UpdateLeagueStatusResponse>(`${this.baseUrl}/${id}/status`, body);
  }

  /** GET /api/League/{id}/summary */
  getSummary(id: number): Observable<LeagueSummaryResponse> {
    return this.http.get<LeagueSummaryResponse>(`${this.baseUrl}/${id}/summary`);
  }

  /** GET /api/League/{id}/members */
  getMembers(id: number): Observable<LeagueMembersResponse> {
    return this.http.get<LeagueMembersResponse>(`${this.baseUrl}/${id}/members`);
  }

  /** GET /api/League/{id}/teams */
  getTeams(id: number): Observable<LeagueTeamsResponse> {
    return this.http.get<LeagueTeamsResponse>(`${this.baseUrl}/${id}/teams`);
  }
  joinLeague(payload: JoinLeagueRequest) {
    return this.http.post<JoinLeagueResponse>(`${this.baseUrl}/join`, payload);
  }



  /**
   * GET /api/League/directory
   * Filtros opcionales: seasonId y status (según tus láminas).
   * Si no mandas nada, trae todo el directorio.
   */
  getDirectory(options?: { seasonId?: number; status?: number }): Observable<LeagueDirectoryResponse> {
    let params = new HttpParams();
    if (options?.seasonId != null) params = params.set('SeasonId', String(options.seasonId));
    if (options?.status != null)   params = params.set('Status', String(options.status));
    return this.http.get<LeagueDirectoryResponse>(`${this.baseUrl}/directory`, { params });
  }
}
