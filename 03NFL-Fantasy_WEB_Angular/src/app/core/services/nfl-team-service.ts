import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateNFLTeamDTO,
  UpdateNFLTeamDTO,
  ListNFLTeamsRequest,
  ListNFLTeamsResponse,
  NFLTeamDetails,
  NFLTeamBasic
} from '../models/nfl-team-model';
import { ApiResponse } from '../models/common-model';

@Injectable({ providedIn: 'root' })
export class NFLTeamService {
  private http = inject(HttpClient);
  // Usar el casing del Swagger/Backend para evitar sorpresas
  private baseUrl = `${environment.apiUrl}/NFLTeam`;

  create(dto: CreateNFLTeamDTO): Observable<ApiResponse<{ nflTeamID: number; teamName: string }>> {
    return this.http.post<ApiResponse<any>>(`${this.baseUrl}`, dto);
  }

  list(request: ListNFLTeamsRequest): Observable<ApiResponse<ListNFLTeamsResponse>> {
    let params = new HttpParams()
      .set('PageNumber', String(request.PageNumber))
      .set('PageSize', String(request.PageSize));

    // Sólo incluir parámetros cuando tienen valor ("cuando algo no está, NO se incluye")
    if (request.SearchTeam && request.SearchTeam.trim().length) {
      // El backend espera SearchTerm
      params = params.set('SearchTerm', request.SearchTeam.trim());
    }
    if (request.FilterCity && request.FilterCity.trim().length) {
      params = params.set('FilterCity', request.FilterCity.trim());
    }
    if (request.FilterIsActive !== undefined) {
      // incluir explícitamente true/false cuando el usuario lo selecciona
      params = params.set('FilterIsActive', String(request.FilterIsActive));
    }

    return this.http.get<ApiResponse<ListNFLTeamsResponse>>(`${this.baseUrl}`, { params });
  }

  getDetails(nflTeamId: number): Observable<ApiResponse<NFLTeamDetails>> {
    return this.http.get<ApiResponse<NFLTeamDetails>>(`${this.baseUrl}/${nflTeamId}`);
  }

  update(nflTeamId: number, dto: UpdateNFLTeamDTO): Observable<ApiResponse<void>> {
    return this.http.put<ApiResponse<void>>(`${this.baseUrl}/${nflTeamId}`, dto);
  }

  deactivate(nflTeamId: number): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.baseUrl}/${nflTeamId}/deactivate`, {});
  }

  reactivate(nflTeamId: number): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.baseUrl}/${nflTeamId}/reactivate`, {});
  }

  getActive(): Observable<ApiResponse<NFLTeamBasic[]>> {
    return this.http.get<ApiResponse<NFLTeamBasic[]>>(`${this.baseUrl}/active`);
  }
}
